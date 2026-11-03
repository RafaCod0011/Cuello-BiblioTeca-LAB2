using Cuello_BiblioTeca_LAB2.Helpers;
using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuello_BiblioTeca_LAB2.Controllers.Api
{
    [ApiController]
    [Route("api/prestamo")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PrestamoApiController : ControllerBase
    {
        private readonly IRepositorioPrestamo repositorio;

        public PrestamoApiController(IRepositorioPrestamo repositorio)
        {
            this.repositorio = repositorio;
        }

        // GET: api/prestamo/mis-prestamos
        [HttpGet("mis-prestamos")]
        public IActionResult GetMisPrestamos()
        {
            try
            {
                int idSocio = User.UsuarioId();
                if (idSocio == 0) 
                    return Unauthorized(new { error = "Token inválido o no proporcionado" });

                var prestamos = repositorio.ObtenerPorSocio(idSocio, 1, int.MaxValue);

                var resultado = prestamos.Select(p => new
                {
                    p.IdPrestamo,
                    Libro = p.Ejemplar?.LibroTitulo ?? "Desconocido",
                    Ejemplar = p.CodigoEjemplar,
                    p.FechaPrestamo,
                    p.FechaVencimiento,
                    p.FechaDevolucion,
                    p.Estado,
                    p.EstaVencido,
                    p.DiasAtraso
                });

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}