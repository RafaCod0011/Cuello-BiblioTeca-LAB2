using Cuello_BiblioTeca_LAB2.Helpers;
using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Cuello_BiblioTeca_LAB2.Controllers
{
    [Authorize]
    public class ReservaController : Controller
    {
        private readonly IRepositorioReserva repositorio;
        private readonly IRepositorioUsuario repositorioUsuario;
        private readonly IRepositorioLibro repositorioLibro;
        private readonly IRepositorioEjemplar repositorioEjemplar;
        private readonly IRepositorioPrestamo repositorioPrestamo;
        private readonly ILogger<ReservaController> logger;

        public ReservaController(
            IRepositorioReserva repositorio,
            IRepositorioUsuario repositorioUsuario,
            IRepositorioLibro repositorioLibro,
            IRepositorioEjemplar repositorioEjemplar,
            IRepositorioPrestamo repositorioPrestamo,
            ILogger<ReservaController> logger)
        {
            this.repositorio = repositorio;
            this.repositorioUsuario = repositorioUsuario;
            this.repositorioLibro = repositorioLibro;
            this.repositorioEjemplar = repositorioEjemplar;
            this.repositorioPrestamo = repositorioPrestamo;
            this.logger = logger;
        }

        // GET: Reserva/Index
        public IActionResult Index(int pagina = 1, string? busqueda = null, string? estado = null)
        {
            try
            {
                var tamaño = 8;
                pagina = Math.Max(pagina, 1);

                ViewBag.Busqueda = busqueda;
                ViewBag.Estado = estado;

                IList<Reserva> lista;
                int total;

                if (User.IsInRole("Administrador") || User.IsInRole("Bibliotecario"))
                {
                    // Ven todas las reservas
                    lista = repositorio.BuscarConFiltros(busqueda, estado, null, pagina, tamaño);
                    total = repositorio.ContarConFiltros(busqueda, estado, null);
                }
                else
                {
                    // Socios ven solo las suyas
                    int idSocio = User.UsuarioId();
                    lista = repositorio.ObtenerPorSocio(idSocio, pagina, tamaño);
                    total = repositorio.ContarPorSocio(idSocio);
                }

                ViewBag.Pagina = pagina;
                ViewBag.TotalPaginas = total % tamaño == 0 ? total / tamaño : total / tamaño + 1;
                ViewBag.TotalRegistros = total;
                ViewBag.EsAdmin = User.IsInRole("Administrador") || User.IsInRole("Bibliotecario");

                if (TempData.ContainsKey("Mensaje"))
                    ViewBag.Mensaje = TempData["Mensaje"];

                return View(lista);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Index de Reserva");
                throw;
            }
        }
        // GET: Reserva/Create
        public IActionResult Create(string? isbn)
        {
            try
            {
                var model = new Reserva
                {
                    FechaReserva = DateTime.Now,
                    FechaVencimiento = DateTime.Now.AddHours(48)
                };

                if (!string.IsNullOrEmpty(isbn))
                {
                    var libro = repositorioLibro.ObtenerPorIsbn(isbn);
                    if (libro != null)
                    {
                        model.ISBN = isbn;
                        ViewBag.LibroId = isbn;
                        ViewBag.LibroSeleccionado = libro.Titulo;
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create GET");
                throw;
            }
        }

        // POST: Reserva/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Reserva reserva)
        {
            try
            {
                // Validación libro existe
                var libro = repositorioLibro.ObtenerPorIsbn(reserva.ISBN);
                if (libro == null)
                    ModelState.AddModelError("ISBN", "El libro no existe.");

                // Validación no hay ejemplares disponibles
                if (libro != null)
                {
                    int disponibles = repositorioLibro.ObtenerCantidadEjemplaresDisponibles(reserva.ISBN);
                    if (disponibles > 0)
                        ModelState.AddModelError("ISBN", "Este libro tiene ejemplares disponibles. Puede pedirlo directamente en préstamo.");
                }

                // Validación  asignar ID de socio
                if (!User.IsInRole("Administrador") && !User.IsInRole("Bibliotecario"))
                {
                    reserva.IdUsuario = User.UsuarioId();
                }

                // Validación no tener reserva pendiente del mismo libro
                if (repositorio.TieneReservaPendiente(reserva.IdUsuario, reserva.ISBN))
                    ModelState.AddModelError("", "Ya tenés una reserva pendiente para este libro.");

                
                if (reserva.IdUsuario > 0)
                {
                    // Validación Límite máximo de 3 reservas pendientes
                    int reservasPendientes = repositorio.ContarReservasPendientesPorSocio(reserva.IdUsuario);
                    if (reservasPendientes >= 3)
                        ModelState.AddModelError("", "El socio ya tiene el máximo de reservas pendientes permitidas (3).");

                    // Validación Multas pendientes
                    if (repositorioPrestamo.ContarMultasPendientesPorSocio(reserva.IdUsuario) > 0)
                        ModelState.AddModelError("", "El socio tiene multas pendientes. Debe regularizarlas antes de hacer una reserva.");

                    // Validación Préstamos vencidos
                    if (repositorioPrestamo.ContarPrestamosVencidosPorSocio(reserva.IdUsuario) > 0)
                        ModelState.AddModelError("", "El socio tiene préstamos vencidos. Debe devolverlos antes de hacer una reserva.");
                }

                if (ModelState.IsValid)
                {
                    reserva.Estado = "Pendiente";
                    reserva.FechaReserva = DateTime.Now;
                    reserva.FechaVencimiento = DateTime.Now.AddHours(48);

                    repositorio.Alta(reserva);
                    TempData["Mensaje"] = "Reserva creada correctamente.";
                    return RedirectToAction(nameof(Index));
                }

                // Recargar libro si hubo error
                if (!string.IsNullOrEmpty(reserva.ISBN) && libro != null)
                {
                    ViewBag.LibroId = reserva.ISBN;
                    ViewBag.LibroSeleccionado = libro.Titulo;
                }

                return View(reserva);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create POST");
                ModelState.AddModelError("", ex.Message);
                return View(reserva);
            }
        }

        // POST: Reserva/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar(int id)
        {
            try
            {
                var reserva = repositorio.ObtenerPorId(id);
                if (reserva == null)
                    return NotFound();

                // Validar solo el dueño o admin/bibliotecario
                if (!User.IsInRole("Administrador") && !User.IsInRole("Bibliotecario"))
                {
                    if (reserva.IdUsuario != User.UsuarioId())
                        return Forbid();
                }

                if (reserva.Estado != "Pendiente")
                {
                    TempData["Error"] = "Solo se pueden cancelar reservas pendientes.";
                    return RedirectToAction(nameof(Index));
                }

                repositorio.Cancelar(id);
                TempData["Mensaje"] = "Reserva cancelada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Cancelar");
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Reserva/Ver/5
        public IActionResult Ver(int id)
        {
            try
            {
                var entidad = repositorio.ObtenerPorId(id);
                if (entidad == null)
                    return NotFound();

                // Validar permisos
                if (!User.IsInRole("Administrador") && !User.IsInRole("Bibliotecario"))
                {
                    if (entidad.IdUsuario != User.UsuarioId())
                        return Forbid();
                }

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