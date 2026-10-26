using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuello_BiblioTeca_LAB2.Controllers.Api
{
    [ApiController]
    [Route("api/libro")] 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class LibroApiController : ControllerBase
    {
        private readonly IRepositorioLibro repositorio;

        public LibroApiController(IRepositorioLibro repositorio)
        {
            this.repositorio = repositorio;
        }

        // GET: api/libro?pagina=1&tamano=10
        [HttpGet]
        public IActionResult Get([FromQuery] int pagina = 1, [FromQuery] int tamano = 10)
        {
            try
            {
                if (tamano > 50) tamano = 50;
                if (pagina < 1) pagina = 1;

                var libros = repositorio.ObtenerLista(pagina, tamano);
                var total = repositorio.ObtenerCantidad();

                var resultado = libros.Select(l => new
                {
                    l.ISBN,
                    l.Titulo,
                    l.NroPaginas,
                    l.Editorial,
                    l.AnioPublicacion,
                    l.Portada,
                    Categoria = l.CategoriaNombre ?? "Sin categoría",
                    Autores = l.AutoresTexto ?? "Sin autores",
                    Disponibles = l.EjemplaresDisponibles
                });

                return Ok(new
                {
                    datos = resultado,
                    paginaActual = pagina,
                    totalPaginas = (int)Math.Ceiling((double)total / tamano),
                    totalRegistros = total
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}