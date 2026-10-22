using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cuello_BiblioTeca_LAB2.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuello_BiblioTeca_LAB2.Controllers
{
    [Authorize(Policy = "Bibliotecario")]
    public class PrestamoController : Controller
    {
        private readonly IRepositorioPrestamo repositorio;
        private readonly IRepositorioUsuario repositorioUsuario;
        private readonly IRepositorioEjemplar repositorioEjemplar;
        private readonly IRepositorioMulta repositorioMulta;
        private readonly IConfiguration config;
        private readonly ILogger<PrestamoController> logger;

        public PrestamoController(
            IRepositorioPrestamo repositorio,
            IRepositorioUsuario repositorioUsuario,
            IRepositorioEjemplar repositorioEjemplar,
            IRepositorioMulta repositorioMulta,
            IConfiguration config,
            ILogger<PrestamoController> logger)
        {
            this.repositorio = repositorio;
            this.repositorioUsuario = repositorioUsuario;
            this.repositorioEjemplar = repositorioEjemplar;
            this.repositorioMulta = repositorioMulta;
            this.config = config;
            this.logger = logger;
        }

        // GET: Prestamo/Index
        public IActionResult Index(int pagina = 1, string? busqueda = null, string? estado = null, bool soloVencidos = false)
        {
            try
            {
                var tamaño = 5;
                pagina = Math.Max(pagina, 1);

                ViewBag.Busqueda = busqueda;
                ViewBag.Estado = estado;
                ViewBag.SoloVencidos = soloVencidos;

                var lista = repositorio.BuscarConFiltros(busqueda, estado, soloVencidos, pagina, tamaño);
                var total = repositorio.ContarConFiltros(busqueda, estado, soloVencidos);

                ViewBag.Pagina = pagina;
                ViewBag.TotalPaginas = total % tamaño == 0 ? total / tamaño : total / tamaño + 1;
                ViewBag.TotalRegistros = total;

                if (TempData.ContainsKey("Mensaje"))
                    ViewBag.Mensaje = TempData["Mensaje"];

                return View(lista);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Index de Prestamo");
                throw;
            }
        }

        // GET: Prestamo/Create
        public IActionResult Create()
        {
            try
            {
                var diasDuracion = config.GetValue<int>("Prestamo:DiasDuracion", 15);

                var model = new Prestamo
                {
                    FechaPrestamo = DateTime.Today,
                    FechaVencimiento = DateTime.Today.AddDays(diasDuracion)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create GET");
                throw;
            }
        }

        // POST: Prestamo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Prestamo prestamo)
        {
            try
            {
                // Validación de fechas
                if (prestamo.FechaPrestamo < DateTime.Today)
                {
                    ModelState.AddModelError("FechaPrestamo", "La fecha de préstamo no puede ser anterior a hoy.");
                }

                if (prestamo.FechaVencimiento <= prestamo.FechaPrestamo)
                {
                    ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento debe ser posterior a la fecha de préstamo.");
                }

                int diasDuracion = config.GetValue<int>("Prestamo:DiasDuracion", 15);
                var fechaVencimientoEsperada = prestamo.FechaPrestamo.AddDays(diasDuracion);

                if (prestamo.FechaVencimiento.Date != fechaVencimientoEsperada.Date)
                {
                    ModelState.AddModelError("FechaVencimiento", $"La fecha de vencimiento debe ser exactamente {diasDuracion} días posterior a la fecha de préstamo.");
                }

                // Validación socio existe
                var socio = repositorioUsuario.ObtenerPorId(prestamo.IdUsuario);
                if (socio == null)
                    ModelState.AddModelError("IdUsuario", "El socio no existe.");

                // Validación ejemplar existe
                var ejemplar = repositorioEjemplar.ObtenerPorId(prestamo.CodigoEjemplar);
                if (ejemplar == null)
                    ModelState.AddModelError("CodigoEjemplar", "El ejemplar no existe.");

                // Validación ejemplar disponible
                if (ejemplar != null && ejemplar.Estado != "Disponible")
                    ModelState.AddModelError("CodigoEjemplar", "El ejemplar no está disponible. ¿Querés reservarlo?");

                if (socio != null)
                {
                    // Validación multas pendientes
                    if (repositorio.ContarMultasPendientesPorSocio(socio.Id) > 0)
                        ModelState.AddModelError("", "El socio tiene multas pendientes. Debe regularizarlas antes de pedir otro libro.");

                    // Validación préstamos vencidos
                    if (repositorio.ContarPrestamosVencidosPorSocio(socio.Id) > 0)
                        ModelState.AddModelError("", "El socio tiene préstamos vencidos. Debe devolverlos antes de pedir otro libro.");

                    // Validación límite de préstamos activos
                    int maximo = config.GetValue<int>("Prestamo:MaximoPorSocio", 3);
                    int activos = repositorio.ContarPrestamosActivosPorSocio(socio.Id);
                    if (activos >= maximo)
                        ModelState.AddModelError("", $"El socio ya tiene el máximo de préstamos permitidos ({maximo}).");

                    // Validación mismo libro ya prestado
                    if (ejemplar != null && repositorio.TienePrestamoActivoDelLibro(socio.Id, ejemplar.ISBN))
                        ModelState.AddModelError("", "El socio ya tiene este libro en préstamo.");
                }

                if (ModelState.IsValid)
                {
                    prestamo.Estado = "Activo";
                    prestamo.IdUsuarioBibliotecario = User.UsuarioId();

                    repositorio.Alta(prestamo);
                    TempData["Mensaje"] = $"Préstamo registrado. Vence el {prestamo.FechaVencimiento:dd/MM/yyyy}.";
                    return RedirectToAction(nameof(Index));
                }

                return View(prestamo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create POST");
                ModelState.AddModelError("", ex.Message);
                return View(prestamo);
            }
        }

        // GET: Prestamo/Devolucion/5
        public IActionResult Devolucion(int id)
        {
            try
            {
                var entidad = repositorio.ObtenerPorId(id);
                if (entidad == null)
                    return NotFound();

                if (entidad.Estado != "Activo")
                {
                    TempData["Error"] = "El préstamo ya fue devuelto.";
                    return RedirectToAction(nameof(Index));
                }

                return View(entidad);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Devolucion GET");
                throw;
            }
        }

        // POST: Prestamo/Devolucion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Devolucion(int id, DateTime fechaDevolucion)
        {
            Prestamo? prestamo = null;

            try
            {
                prestamo = repositorio.ObtenerPorId(id);
                if (prestamo == null)
                    return NotFound();

                if (prestamo.Estado != "Activo")
                {
                    TempData["Error"] = "El préstamo ya fue devuelto.";
                    return RedirectToAction(nameof(Index));
                }

                // Validación fechas
                if (fechaDevolucion < prestamo.FechaPrestamo)
                {
                    ModelState.AddModelError("FechaDevolucion", "La fecha de devolución no puede ser anterior a la fecha de préstamo.");
                    return View(prestamo);
                }

                if (fechaDevolucion > DateTime.Today)
                {
                    ModelState.AddModelError("FechaDevolucion", "La fecha de devolución no puede ser futura.");
                    return View(prestamo);
                }

                // Calcular multa si hay atraso
                decimal tarifaMulta = config.GetValue<decimal>("Prestamo:TarifaMultaPorDia", 500);
                int diasAtraso = (fechaDevolucion - prestamo.FechaVencimiento).Days;
                decimal montoMulta = diasAtraso > 0 ? diasAtraso * tarifaMulta : 0;

                
                repositorio.RegistrarDevolucion(id, fechaDevolucion);

                // Crear multa
                if (montoMulta > 0)
                {
                    var multa = new Multa
                    {
                        IdPrestamo = id,
                        Importe = montoMulta,
                        Motivo = $"Devolución tardía ({diasAtraso} día(s) de atraso)",
                        Pagada = false,
                        FechaDeGeneracion = DateTime.Now,
                        IdUsuarioBibliotecario = User.UsuarioId()
                    };

                    repositorioMulta.Alta(multa);

                    TempData["Mensaje"] = $"Devolución registrada. Multa generada: {montoMulta.ToString("C", new System.Globalization.CultureInfo("es-AR"))} por {diasAtraso} día(s) de atraso.";
                }
                else
                {
                    TempData["Mensaje"] = "Devolución registrada correctamente.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Devolucion POST");
                ModelState.AddModelError("", ex.Message);

                if (prestamo != null)
                    return View(prestamo);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Prestamo/Ver/5
        public IActionResult Ver(int id)
        {
            try
            {
                var entidad = repositorio.ObtenerPorId(id);
                if (entidad == null)
                    return NotFound();
                return View(entidad);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Ver");
                throw;
            }
        }
    }
}