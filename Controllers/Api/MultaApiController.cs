using Cuello_BiblioTeca_LAB2.Helpers;
using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuello_BiblioTeca_LAB2.Controllers.Api
{
    [ApiController]
    [Route("api/multa")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MultaApiController : ControllerBase
    {
        private readonly IRepositorioMulta repositorio;

        public MultaApiController(IRepositorioMulta repositorio)
        {
            this.repositorio = repositorio;
        }

        // GET: api/multa/mis-multas
        [HttpGet("mis-multas")]
        public IActionResult GetMisMultas()
        {
            try
            {
                int idSocio = User.UsuarioId();
                if (idSocio == 0) 
                    return Unauthorized(new { error = "Token inválido o no proporcionado" });

                var multas = repositorio.ObtenerPorSocio(idSocio);

                var resultado = multas.Select(m => new
                {
                    m.IdMulta,
                    m.Importe,
                    m.Motivo,
                    m.Pagada,
                    m.FechaDeGeneracion,
                    m.FechaPagada,
                    Libro = m.LibroTitulo ?? "Desconocido"
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