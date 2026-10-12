using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authorization;

namespace Cuello_BiblioTeca_LAB2.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
       // private readonly IRepositorioLibro libros;
        private readonly IRepositorioUsuario usuarios;
       // private readonly IRepositorioPrestamo prestamos;
        private readonly IConfiguration config;

        public HomeController(
           // IRepositorioLibro libros,
            IRepositorioUsuario usuarios,
           // IRepositorioPrestamo prestamos,
            IConfiguration config,
            ILogger<HomeController> logger)
        {
           // this.libros = libros;
            this.usuarios = usuarios;
           // this.prestamos = prestamos;
            this.config = config;
            logger.LogInformation("Estoy en el constructor de HomeController");
        }

        public IActionResult Index()
        {
            ViewBag.Titulo = "Página de Inicio";

            // Estadísticas básicas para el dashboard
           // ViewBag.TotalLibros = libros.ObtenerCantidad();
            ViewBag.TotalUsuarios = usuarios.ObtenerCantidad();
           // ViewBag.TotalPrestamos = prestamos.ObtenerCantidad();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public IActionResult Restringido()
        {
            return View();
        }
    }
}