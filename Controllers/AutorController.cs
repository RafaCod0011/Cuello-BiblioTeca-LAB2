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
    public class AutorController : Controller
    {
        private readonly IRepositorioAutor repositorio;
        private readonly ILogger<AutorController> logger;

        public AutorController(IRepositorioAutor repo, ILogger<AutorController> logger)
        {
            this.repositorio = repo;
            this.logger = logger;
        }

        // GET: Autor/Index
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
                logger.LogError(ex, "Error en Index de Autor");
                throw;
            }
        }

        // GET: Autor/Create
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

        // POST: Autor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Bibliotecario")]
        public ActionResult Create(Autor autor)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    repositorio.Alta(autor);
                    TempData["Mensaje"] = "Autor creado correctamente";
                    return RedirectToAction(nameof(Index));
                }
                return View(autor);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create POST");
                ModelState.AddModelError("", ex.Message);
                return View(autor);
            }
        }

        // GET: Autor/Edit/5
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

        // POST: Autor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Bibliotecario")]
        public ActionResult Edit(int id, Autor entidad)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(entidad);

                var a = repositorio.ObtenerPorId(id);
                if (a == null)
                    return NotFound();

                a.Nombre = entidad.Nombre;
                a.Apellido = entidad.Apellido;
                a.Nacionalidad = entidad.Nacionalidad;

                repositorio.Modificacion(a);
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

        // GET: Autor/Eliminar/5
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

        // POST: Autor/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public ActionResult Eliminar(int id, Autor entidad)
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
                    TempData["Error"] = "No se puede eliminar el autor porque tiene libros asociados.";
                }
                else
                {
                    TempData["Error"] = ex.Message;
                }
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Autor/Ver/5
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

        // GET: Autor/Buscar?term=...  (Select2 AJAX)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Buscar(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                    return Json(new List<object>());

                var resultados = repositorio.BuscarPorNombre(term)
                    .Select(a => new
                    {
                        id = a.IdAutor,
                        text = a.ToString()
                    })
                    .ToList();

                return Json(resultados);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al buscar autores");
                return Json(new List<object>());
            }
        }
    }
}