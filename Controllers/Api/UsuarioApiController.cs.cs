using Cuello_BiblioTeca_LAB2.Helpers;
using Cuello_BiblioTeca_LAB2.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Cuello_BiblioTeca_LAB2.Controllers.Api
{
    [ApiController]
    [Route("api/usuario")] 
    public class UsuarioApiController : ControllerBase
    {
        private readonly IRepositorioUsuario repositorio;
        private readonly IConfiguration config;
        private readonly ILogger<UsuarioApiController> logger;

        public UsuarioApiController(IRepositorioUsuario repositorio, IConfiguration config, ILogger<UsuarioApiController> logger)
        {
            this.repositorio = repositorio;
            this.config = config;
            this.logger = logger;
        }

        // POST: api/usuario/login
        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginView login)
        {
            try
            {
                if (login == null || string.IsNullOrEmpty(login.Usuario) || string.IsNullOrEmpty(login.Clave))
                    return BadRequest(new { error = "Email y clave son obligatorios" });

                string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: login.Clave,
                    salt: Encoding.ASCII.GetBytes(config["Salt"] ?? ""),
                    prf: KeyDerivationPrf.HMACSHA1,
                    iterationCount: 1000,
                    numBytesRequested: 256 / 8));

                var usuario = repositorio.ObtenerPorEmail(login.Usuario);
                if (usuario == null || usuario.Clave != hashed)
                    return Unauthorized(new { error = "Email o clave incorrectos" });

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Email),
                    new Claim("FullName", usuario.Nombre + " " + usuario.Apellido),
                    new Claim(ClaimTypes.Role, usuario.RolNombre)
                };

                var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config["TokenAuthentication:SecretKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                int horas = config.GetValue<int>("TokenAuthentication:TokenExpirationHours", 8);

                var token = new JwtSecurityToken(
                    issuer: config["TokenAuthentication:Issuer"],
                    audience: config["TokenAuthentication:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddHours(horas),
                    signingCredentials: creds
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expira = DateTime.Now.AddHours(horas),
                    usuario = new
                    {
                        usuario.Id,
                        usuario.Nombre,
                        usuario.Apellido,
                        usuario.Email,
                        Rol = usuario.RolNombre
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en login API");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // GET: api/usuario/perfil
        [HttpGet("perfil")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Perfil()
        {
            try
            {
                int idUsuario = User.UsuarioId();
                if (idUsuario == 0) return Unauthorized(new { error = "Token inválido" });

                var usuario = repositorio.ObtenerPorId(idUsuario);
                if (usuario == null) return NotFound(new { error = "Usuario no encontrado" });

                return Ok(new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Apellido,
                    usuario.Dni,
                    usuario.Email,
                    usuario.Telefono,
                    usuario.Direccion,
                    Rol = usuario.RolNombre,
                    usuario.Avatar
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en perfil API");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }
    }
}