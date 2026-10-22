using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuello_BiblioTeca_LAB2.Controllers
{
    [Authorize(Policy = "Bibliotecario")]
    public class EjemplarController : Controller
    {
        private readonly IRepositorioEjemplar repositorio;
        private readonly IRepositorioLibro repositorioLibro;
        private readonly ILogger<EjemplarController> logger;

        public EjemplarController(
            IRepositorioEjemplar repositorio,
            IRepositorioLibro repositorioLibro,
            ILogger<EjemplarController> logger)
        {
            this.repositorio = repositorio;
            this.repositorioLibro = repositorioLibro;
            this.logger = logger;
        }

        // GET: Ejemplar/Index
        public IActionResult Index(int pagina = 1, string? busqueda = null, string? estado = null)
        {
            try
            {
                var tamaño = 5;
                pagina = Math.Max(pagina, 1);

                ViewBag.Busqueda = busqueda;
                ViewBag.Estado = estado;

                bool sinFiltros = string.IsNullOrWhiteSpace(busqueda) && string.IsNullOrWhiteSpace(estado);

                IList<Ejemplar> lista;
                int total;

                if (sinFiltros)
                {
                    lista = repositorio.ObtenerLista(pagina, tamaño);
                    total = repositorio.ObtenerCantidad();
                }
                else
                {
                    lista = repositorio.BuscarConFiltros(busqueda, estado, pagina, tamaño);
                    total = repositorio.ContarConFiltros(busqueda, estado);
                }

                ViewBag.Pagina = pagina;
                ViewBag.TotalPaginas = total % tamaño == 0 ? total / tamaño : total / tamaño + 1;
                ViewBag.TotalRegistros = total;
                int totalPaginas = (int)ViewBag.TotalPaginas;

                int desde = Math.Max(1, Math.Min(pagina - 2, totalPaginas - 4));
                int hasta = Math.Min(totalPaginas, desde + 4);
                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;

                if (TempData.ContainsKey("Mensaje"))
                    ViewBag.Mensaje = TempData["Mensaje"];

                return View(lista);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Index de Ejemplar");
                throw;
            }
        }

        // GET: Ejemplar/Create
        public IActionResult Create(string? isbn)
        {
            try
            {
                var model = new Ejemplar();

                // Si viene pre-seleccionado (desde Ver Libro)
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

        // POST: Ejemplar/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Ejemplar ejemplar)
        {
            try
            {
                // Validar que el libro exista
                if (string.IsNullOrEmpty(ejemplar.ISBN) || repositorioLibro.ObtenerPorIsbn(ejemplar.ISBN) == null)
                    ModelState.AddModelError("ISBN", "El libro seleccionado no existe.");

                // Validar estado
                var estadosValidos = new[] { "Disponible", "Dañado", "Baja" };
                if (!estadosValidos.Contains(ejemplar.Estado))
                    ModelState.AddModelError("Estado", "Estado inválido.");

                if (ModelState.IsValid)
                {
                    repositorio.Alta(ejemplar);
                    TempData["Mensaje"] = "Ejemplar creado correctamente";
                    return RedirectToAction(nameof(Index));
                }

                // Recargar libro seleccionado si hubo error
                if (!string.IsNullOrEmpty(ejemplar.ISBN))
                {
                    var libro = repositorioLibro.ObtenerPorIsbn(ejemplar.ISBN);
                    ViewBag.LibroId = ejemplar.ISBN;
                    ViewBag.LibroSeleccionado = libro?.Titulo;
                }

                return View(ejemplar);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create POST");
                ModelState.AddModelError("", ex.Message);
                return View(ejemplar);
            }
        }

        // GET: Ejemplar/Edit/5
        public IActionResult Edit(int id)
        {
            try
            {
                var entidad = repositorio.ObtenerPorId(id);
                if (entidad == null)
                    return NotFound();

                ViewBag.LibroId = entidad.ISBN;
                ViewBag.LibroSeleccionado = entidad.LibroTitulo;

                return View(entidad);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Edit GET");
                throw;
            }
        }

        // POST: Ejemplar/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Ejemplar entidad)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.LibroId = entidad.ISBN;
                    ViewBag.LibroSeleccionado = entidad.LibroTitulo;
                    return View(entidad);
                }

                var e = repositorio.ObtenerPorId(id);
                if (e == null)
                    return NotFound();

                // Validar estado
                var estadosValidos = new[] { "Disponible", "Dañado", "Baja" };
                if (!estadosValidos.Contains(entidad.Estado))
                    ModelState.AddModelError("Estado", "Estado inválido.");

                e.ISBN = entidad.ISBN;
                e.Localizacion = entidad.Localizacion;
                e.Estado = entidad.Estado;

                repositorio.Modificacion(e);
                TempData["Mensaje"] = "Datos guardados correctamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Edit POST");
                ModelState.AddModelError("", ex.Message);
                return View(entidad);
            }
        }

        // GET: Ejemplar/Eliminar/5
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

        // POST: Ejemplar/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id, Ejemplar entidad)
        {
            try
            {
                // Validar que no tenga préstamos activos
                if (repositorio.TienePrestamosActivos(id))
                {
                    TempData["Error"] = "No se puede eliminar el ejemplar porque tiene un préstamo activo.";
                    return RedirectToAction(nameof(Index));
                }

                repositorio.Baja(id);
                TempData["Mensaje"] = "Eliminación realizada correctamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Eliminar POST");
                if (ex.Message.Contains("foreign key constraint fails") ||
                    ex.Message.Contains("Cannot delete or update a parent row"))
                {
                    TempData["Error"] = "No se puede eliminar el ejemplar porque está asociado a préstamos.";
                }
                else
                {
                    TempData["Error"] = ex.Message;
                }
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Ejemplar/Ver/5
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

        // GET: Ejemplar/BuscarDisponibles?term=...
        [HttpGet]
        public IActionResult BuscarDisponibles(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                    return Json(new List<object>());

                var resultados = repositorio.BuscarConFiltros(term, "Disponible", 1, 20)
                    .Select(e => new
                    {
                        id = e.CodigoEjemplar,
                        text = $"{e.LibroTitulo} (Ejemplar #{e.CodigoEjemplar} - {e.Localizacion ?? "Sin ubicación"})"
                    })
                    .ToList();

                return Json(resultados);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al buscar ejemplares disponibles");
                return Json(new List<object>());
            }
        }
    }
}