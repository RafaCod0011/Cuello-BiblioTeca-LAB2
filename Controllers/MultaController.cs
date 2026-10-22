using Cuello_BiblioTeca_LAB2.Helpers;
using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Cuello_BiblioTeca_LAB2.Controllers
{
    [Authorize(Policy = "Bibliotecario")]
    public class MultaController : Controller
    {
        private readonly IRepositorioMulta repositorio;
        private readonly ILogger<MultaController> logger;

        public MultaController(IRepositorioMulta repositorio, ILogger<MultaController> logger)
        {
            this.repositorio = repositorio;
            this.logger = logger;
        }

        // GET: Multa/Index
        public IActionResult Index(int pagina = 1, string? busqueda = null, string? estado = null)
        {
            try
            {
                var tamaño = 5;
                pagina = Math.Max(pagina, 1);

                ViewBag.Busqueda = busqueda;
                ViewBag.Estado = estado;

                bool? pagada = null;
                if (estado == "Pagada") pagada = true;
                else if (estado == "Pendiente") pagada = false;

                var lista = repositorio.BuscarConFiltros(busqueda, pagada, pagina, tamaño);
                var total = repositorio.ContarConFiltros(busqueda, pagada);

                ViewBag.Pagina = pagina;
                ViewBag.TotalPaginas = total % tamaño == 0 ? total / tamaño : total / tamaño + 1;
                ViewBag.TotalRegistros = total;

                if (TempData.ContainsKey("Mensaje"))
                    ViewBag.Mensaje = TempData["Mensaje"];

                return View(lista);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Index de Multa");
                throw;
            }
        }

        // GET: Multa/Ver/5
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
                logger.LogError(ex, "Error en Ver de Multa");
                throw;
            }
        }

        // GET: Multa/Pagar/5
        public IActionResult Pagar(int id)
        {
            try
            {
                var entidad = repositorio.ObtenerPorId(id);
                if (entidad == null)
                    return NotFound();

                if (entidad.Pagada)
                {
                    TempData["Error"] = "Esta multa ya fue pagada.";
                    return RedirectToAction(nameof(Index));
                }

                return View(entidad);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Pagar GET");
                throw;
            }
        }

        // POST: Multa/Pagar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pagar(int id, Multa entidad)
        {
            try
            {
                var multa = repositorio.ObtenerPorId(id);
                if (multa == null)
                    return NotFound();

                if (multa.Pagada)
                {
                    TempData["Error"] = "Esta multa ya fue pagada.";
                    return RedirectToAction(nameof(Index));
                }

                repositorio.RegistrarPago(id, User.UsuarioId());
                TempData["Mensaje"] = "Pago registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Pagar POST");
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Multa/Eliminar/5
        [Authorize(Policy = "Administrador")]
        public IActionResult Eliminar(int id)
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
                logger.LogError(ex, "Error en Eliminar GET");
                throw;
            }
        }

        // POST: Multa/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public IActionResult Eliminar(int id, Multa entidad)
        {
            try
            {
                repositorio.Baja(id);
                TempData["Mensaje"] = "Multa eliminada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Eliminar POST");
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}