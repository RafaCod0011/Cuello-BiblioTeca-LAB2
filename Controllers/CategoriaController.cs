using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuello_BiblioTeca_LAB2.Controllers
{
    [Authorize]
    public class CategoriaController : Controller
    {
        private readonly IRepositorioCategoria repositorio;
        private readonly ILogger<CategoriaController> logger;

        public CategoriaController(IRepositorioCategoria repo, ILogger<CategoriaController> logger)
        {
            this.repositorio = repo;
            this.logger = logger;
        }

        // GET: Categoria/Index
        public ActionResult Index(int pagina = 1)
        {
            try
            {
                var tamaño = 5;
                var lista = repositorio.ObtenerLista(Math.Max(pagina, 1), tamaño);
                ViewBag.Pagina = pagina;
                var total = repositorio.ObtenerCantidad();
                ViewBag.TotalPaginas = total % tamaño == 0 ? total / tamaño : total / tamaño + 1;
                ViewBag.TotalRegistros = total;

                if (TempData.ContainsKey("Mensaje"))
                    ViewBag.Mensaje = TempData["Mensaje"];

                return View(lista);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Index de Categoria");
                throw;
            }
        }

        // GET: Categoria/Create
        [Authorize(Policy = "Bibliotecario")]
        public ActionResult Create()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create GET");
                throw;
            }
        }

        // POST: Categoria/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Bibliotecario")]
        public ActionResult Create(Categoria categoria)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    repositorio.Alta(categoria);
                    TempData["Mensaje"] = "Categoría creada correctamente";
                    return RedirectToAction(nameof(Index));
                }
                return View(categoria);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create POST");
                ModelState.AddModelError("", ex.Message);
                return View(categoria);
            }
        }

        // GET: Categoria/Edit/5
        [Authorize(Policy = "Bibliotecario")]
        public ActionResult Edit(int id)
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
                logger.LogError(ex, "Error en Edit GET");
                throw;
            }
        }

        // POST: Categoria/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Bibliotecario")]
        public ActionResult Edit(int id, Categoria entidad)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(entidad);

                var c = repositorio.ObtenerPorId(id);
                if (c == null)
                    return NotFound();

                c.Nombre = entidad.Nombre;
                c.Descripcion = entidad.Descripcion;

                repositorio.Modificacion(c);
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

        // GET: Categoria/Eliminar/5
        [Authorize(Policy = "Administrador")]
        public ActionResult Eliminar(int id)
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

        // POST: Categoria/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public ActionResult Eliminar(int id, Categoria entidad)
        {
            try
            {
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
                    TempData["Error"] = "No se puede eliminar la categoría porque tiene libros asociados.";
                }
                else
                {
                    TempData["Error"] = ex.Message;
                }
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Categoria/Ver/5
        [Authorize(Policy = "Bibliotecario")]
        public ActionResult Ver(int id)
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

        // GET: Categoria/Buscar?term=...  (Select2 AJAX)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Buscar(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
                    return Json(new List<object>());

                var resultados = repositorio.BuscarPorNombre(term)
                    .Select(c => new
                    {
                        id = c.IdCategoria,
                        text = c.Nombre
                    })
                    .ToList();

                return Json(resultados);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al buscar categorías");
                return Json(new List<object>());
            }
        }
    }
}