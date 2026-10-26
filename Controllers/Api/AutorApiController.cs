using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuello_BiblioTeca_LAB2.Controllers.Api
{
    [ApiController]
    [Route("api/autor")] 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AutorApiController : ControllerBase
    {
        private readonly IRepositorioAutor repositorio;

        public AutorApiController(IRepositorioAutor repositorio)
        {
            this.repositorio = repositorio;
        }

        // GET: api/autor?pagina=1&tamano=10
        [HttpGet]
        public IActionResult Get([FromQuery] int pagina = 1, [FromQuery] int tamano = 10)
        {
            try
            {
                if (tamano > 50) tamano = 50;
                if (pagina < 1) pagina = 1;

                var autores = repositorio.ObtenerLista(pagina, tamano);
                var total = repositorio.ObtenerCantidad();

                var resultado = autores.Select(a => new
                {
                    a.IdAutor,
                    a.Nombre,
                    a.Apellido,
                    a.Nacionalidad
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