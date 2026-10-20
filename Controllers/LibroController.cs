using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cuello_BiblioTeca_LAB2.Controllers
{
    [Authorize]
    public class LibroController : Controller
    {
        private readonly IRepositorioLibro repositorio;
        private readonly IRepositorioCategoria repositorioCategoria;
        private readonly IRepositorioAutor repositorioAutor;
        private readonly IWebHostEnvironment env;
        private readonly ILogger<LibroController> logger;

        public LibroController(
            IRepositorioLibro repositorio,
            IRepositorioCategoria repositorioCategoria,
            IRepositorioAutor repositorioAutor,
            IWebHostEnvironment env,
            ILogger<LibroController> logger)
        {
            this.repositorio = repositorio;
            this.repositorioCategoria = repositorioCategoria;
            this.repositorioAutor = repositorioAutor;
            this.env = env;
            this.logger = logger;
        }

        // GET: Libro/Index
        public IActionResult Index(int pagina = 1)
        {
            try
            {
                var tamaño = 6;
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
                logger.LogError(ex, "Error en Index de Libro");
                throw;
            }
        }

        // GET: Libro/Create
        public IActionResult Create()
        {
            try
            {
                CargarListasDesplegables();
                return View();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create GET");
                throw;
            }
        }

        // POST: Libro/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Bibliotecario")]
        public IActionResult Create(Libro libro, string? autoresIds)
        {
            try
            {
                if (libro.AnioPublicacion.HasValue && libro.AnioPublicacion.Value > DateTime.Today.Year)
                    ModelState.AddModelError("AnioPublicacion", "El año de publicación no puede ser futuro.");

                if (ModelState.IsValid)
                {
                    libro.IdAutores = ParseAutoresIds(autoresIds);
                    var errorValidacion = ValidarCategoriaYAutores(libro.IdCategoria, libro.IdAutores);
                    if (errorValidacion != null)
                        return errorValidacion;

                    // Portada
                    if (libro.PortadaFile != null && libro.PortadaFile.Length > 0)
                        libro.Portada = GuardarPortada(libro.PortadaFile, libro.ISBN);

                    repositorio.Alta(libro);
                    TempData["Mensaje"] = "Libro creado correctamente";
                    return RedirectToAction(nameof(Index));
                }
                CargarListasDesplegables(libro.IdCategoria);
                return View(libro);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Create POST");
                ModelState.AddModelError("", ex.Message);
                CargarListasDesplegables(libro.IdCategoria);
                return View(libro);
            }
        }

        // GET: Libro/Edit/978...
        public IActionResult Edit(string id)
        {
            try
            {
                var entidad = repositorio.ObtenerPorIsbn(id);
                if (entidad == null)
                    return NotFound();

                if (entidad.IdCategoria.HasValue)
                {
                    var cat = repositorioCategoria.ObtenerPorId(entidad.IdCategoria.Value);
                    ViewBag.CategoriaId = entidad.IdCategoria.Value;
                    ViewBag.CategoriaNombre = cat?.Nombre ?? "";
                }

                return View(entidad);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Edit GET");
                throw;
            }
        }

        // POST: Libro/Edit/978...
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Bibliotecario")]
        public IActionResult Edit(string id, Libro entidad, string? autoresIds)
        {
            try
            {
                if (entidad.AnioPublicacion.HasValue && entidad.AnioPublicacion.Value > DateTime.Today.Year)
                    ModelState.AddModelError("AnioPublicacion", "El año de publicación no puede ser futuro.");

                if (!ModelState.IsValid)
                {
                    CargarListasDesplegables(entidad.IdCategoria);
                    return View(entidad);
                }

                var l = repositorio.ObtenerPorIsbn(id);
                if (l == null)
                    return NotFound();

                var idsAutores = ParseAutoresIds(autoresIds);
                var errorValidacion = ValidarCategoriaYAutores(entidad.IdCategoria, idsAutores);
                if (errorValidacion != null)
                    return errorValidacion;

                l.Titulo = entidad.Titulo;
                l.NroPaginas = entidad.NroPaginas;
                l.Editorial = entidad.Editorial;
                l.AnioPublicacion = entidad.AnioPublicacion;
                l.Sinopsis = entidad.Sinopsis;
                l.IdCategoria = entidad.IdCategoria;

                // Portada
                if (entidad.PortadaFile != null && entidad.PortadaFile.Length > 0)
                {
                    EliminarPortadaFisica(l.Portada);
                    l.Portada = GuardarPortada(entidad.PortadaFile, l.ISBN);
                }

                l.IdAutores = idsAutores;

                repositorio.Modificacion(l);
                TempData["Mensaje"] = "Datos guardados correctamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Edit POST");
                ModelState.AddModelError("", ex.Message);
                CargarListasDesplegables(entidad.IdCategoria);
                return View(entidad);
            }
        }

        // GET: Libro/Eliminar/978...
        [Authorize(Policy = "Administrador")]
        public IActionResult Eliminar(string id)
        {
            try
            {
                var entidad = repositorio.ObtenerPorIsbn(id);
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

        // POST: Libro/Eliminar/978...
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrador")]
        public IActionResult Eliminar(string id, Libro entidad)
        {
            try
            {
                var libro = repositorio.ObtenerPorIsbn(id);
                if (libro != null)
                    EliminarPortadaFisica(libro.Portada);

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
                    TempData["Error"] = "No se puede eliminar el libro porque tiene ejemplares o préstamos asociados.";
                }
                else
                {
                    TempData["Error"] = ex.Message;
                }
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Libro/Ver/978...
        public IActionResult Ver(string id)
        {
            try
            {
                var entidad = repositorio.ObtenerPorIsbn(id);
                if (entidad == null)
                    return NotFound();

                ViewBag.TotalEjemplares = repositorio.ObtenerCantidadEjemplares(id);
                ViewBag.EjemplaresDisponibles = repositorio.ObtenerCantidadEjemplaresDisponibles(id);

                return View(entidad);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en Ver");
                throw;
            }
        }
        // GET: Libro/BuscarCategorias?term=...
        [HttpGet]
        [AllowAnonymous]
        public IActionResult BuscarCategorias(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
                    return Json(new List<object>());

                var resultados = repositorioCategoria.BuscarPorNombre(term)
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

        // Metodos privados para cargar listas desplegables y manejar portadas
        private void CargarListasDesplegables(int? categoriaSeleccionada = null)
        {
            ViewBag.Categorias = repositorioCategoria.ObtenerLista(1, int.MaxValue);
            if (categoriaSeleccionada.HasValue)
                ViewBag.CategoriaId = categoriaSeleccionada.Value;
        }

        private string GuardarPortada(IFormFile portadaFile, string isbn)
        {
            string wwwPath = env.WebRootPath;
            string path = Path.Combine(wwwPath, "Uploads", "Portadas");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string extension = Path.GetExtension(portadaFile.FileName);
            string fileName = $"portada_{isbn}{extension}";
            string rutaCompleta = Path.Combine(path, fileName);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                portadaFile.CopyTo(stream);
            }

            return $"/Uploads/Portadas/{fileName}";
        }

        private void EliminarPortadaFisica(string? rutaPortada)
        {
            if (string.IsNullOrEmpty(rutaPortada)) return;

            string rutaFisica = Path.Combine(env.WebRootPath, rutaPortada.TrimStart('/'));
            if (System.IO.File.Exists(rutaFisica))
                System.IO.File.Delete(rutaFisica);
        }

        private List<int> ParseAutoresIds(string? autoresIds)
        {
            var res = new List<int>();
            if (string.IsNullOrWhiteSpace(autoresIds)) return res;

            foreach (var s in autoresIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(s.Trim(), out int id))
                    res.Add(id);
            }
            return res;
        }

        private IActionResult? ValidarCategoriaYAutores(int? idCategoria, IEnumerable<int> idsAutores)
        {
            if (!idCategoria.HasValue)
                return BadRequest(new { error = "La categoría es obligatoria" });

            var categoria = repositorioCategoria.ObtenerPorId(idCategoria.Value);
            if (categoria == null)
                return BadRequest(new { error = "La categoría seleccionada no existe" });

            foreach (var id in idsAutores)
            {
                if (repositorioAutor.ObtenerPorId(id) == null)
                    return BadRequest(new { error = $"El autor con ID {id} no existe" });
            }

            return null;
        }
    }
}