using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cuello_BiblioTeca_LAB2.Models;
using Cuello_BiblioTeca_LAB2.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text;

namespace Cuello_BiblioTeca_LAB2.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IRepositorioUsuario repositorio;
        private readonly ILogger<UsuarioController> logger;
        private readonly IConfiguration config;
        private readonly IWebHostEnvironment env;

        public UsuarioController(IRepositorioUsuario repositorio, ILogger<UsuarioController> logger, IConfiguration config, IWebHostEnvironment env)
        {
            this.repositorio = repositorio;
            this.logger = logger;
            this.config = config;
            this.env = env;
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginView login, string returnUrl = null)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string hashed = HashearClave(login.Clave);

                    var usuario = repositorio.ObtenerPorEmail(login.Usuario);
                    if (usuario == null || usuario.Clave != hashed)
                    {
                        ModelState.AddModelError("", "El email o la clave no son correctos");
                        return View(login);
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                        new Claim(ClaimTypes.Name, usuario.Email),
                        new Claim("FullName", usuario.Nombre + " " + usuario.Apellido),
                        new Claim(ClaimTypes.Role, usuario.RolNombre),
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return Redirect(returnUrl ?? "/Home/Index");
                }
                return View(login);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(login);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public IActionResult Perfil()
        {
            var id = User.UsuarioId();
            return RedirectToAction("Ver", new { id });
        }

        [Authorize]
        public IActionResult Ver(int id)
        {
            var usuario = repositorio.ObtenerPorId(id);
            if (usuario == null)
                return NotFound();

            if (!User.IsInRole("Administrador") && User.UsuarioId() != id)
                return RedirectToAction("Restringido", "Home");

            return View(usuario);
        }

        [Authorize(Policy = "Administrador")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Policy = "Administrador")]
        [HttpGet]
        public IActionResult ObtenerTodos(int pagina = 1, int tamano = 5)
        {
            try
            {
                pagina = Math.Max(pagina, 1);
                tamano = tamano > 0 ? tamano : 5;
                var usuarios = repositorio.ObtenerLista(pagina, tamano);
                var total = repositorio.ObtenerCantidad();
                var totalPaginas = total % tamano == 0 ? total / tamano : total / tamano + 1;
                var datos = usuarios.Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellido,
                    u.Dni,
                    u.Direccion,
                    u.Telefono,
                    u.Email,
                    u.Rol,
                    RolNombre = u.RolNombre,
                    Avatar = u.Avatar ?? ""
                });
                return Json(new
                {
                    datos = datos,
                    paginaActual = pagina,
                    totalPaginas = totalPaginas,
                    totalRegistros = total
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(500, new { error = "Error al cargar usuarios" });
            }
        }

        [HttpGet]
        public IActionResult ObtenerPorId(int id)
        {
            try
            {
                var u = repositorio.ObtenerPorId(id);
                if (u == null)
                    return NotFound(new { error = "Usuario no encontrado" });

                return Json(new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellido,
                    u.Dni,
                    u.Direccion,
                    u.Telefono,
                    u.Email,
                    u.Rol,
                    RolNombre = u.RolNombre,
                    u.Avatar,
                    Clave = ""
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al obtener usuario {Id}", id);
                return StatusCode(500, new { error = "Error al cargar el usuario" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "Administrador")]
        public IActionResult Create(Usuario usuario, IFormFile? avatarFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existenteEmail = repositorio.ObtenerPorEmail(usuario.Email);
                if (existenteEmail != null)
                    return BadRequest(new { error = "Ya existe un usuario con ese email" });

                var existenteDni = repositorio.ObtenerPorDni(usuario.Dni);
                if (existenteDni != null)
                    return BadRequest(new { error = "Ya existe un usuario con ese DNI" });

                usuario.Clave = HashearClave(usuario.Clave);

                if (avatarFile != null && avatarFile.Length > 0)
                {
                    string wwwPath = env.WebRootPath;
                    string path = Path.Combine(wwwPath, "Uploads", "Avatars");
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    int id = repositorio.Alta(usuario);

                    string extension = Path.GetExtension(avatarFile.FileName);
                    string fileName = $"avatar_{id}{extension}";
                    string rutaCompleta = Path.Combine(path, fileName);

                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        avatarFile.CopyTo(stream);
                    }

                    usuario.Avatar = $"/Uploads/Avatars/{fileName}";
                    repositorio.Modificacion(usuario);
                }
                else
                {
                    int id = repositorio.Alta(usuario);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al crear usuario");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Editar([FromBody] Usuario usuario)
        {
            try
            {
                if (usuario == null)
                    return BadRequest(new { error = "Datos inválidos" });

                var existente = repositorio.ObtenerPorId(usuario.Id);
                if (existente == null)
                    return NotFound(new { error = "Usuario no encontrado" });

                if (string.IsNullOrWhiteSpace(usuario.Nombre) ||
                    string.IsNullOrWhiteSpace(usuario.Apellido) ||
                    string.IsNullOrWhiteSpace(usuario.Email) ||
                    string.IsNullOrWhiteSpace(usuario.Dni))
                    return BadRequest(new { error = "Complete nombre, apellido, DNI y email" });

                var otroEmail = repositorio.ObtenerPorEmail(usuario.Email);
                if (otroEmail != null && otroEmail.Id != usuario.Id)
                    return BadRequest(new { error = "Ya existe otro usuario con ese email" });

                var otroDni = repositorio.ObtenerPorDni(usuario.Dni);
                if (otroDni != null && otroDni.Id != usuario.Id)
                    return BadRequest(new { error = "Ya existe otro usuario con ese DNI" });

                if (!User.IsInRole("Administrador"))
                    usuario.Rol = existente.Rol;

                if (string.IsNullOrWhiteSpace(usuario.Clave))
                    usuario.Clave = existente.Clave;
                else
                    usuario.Clave = HashearClave(usuario.Clave);

                repositorio.Modificacion(usuario);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al editar usuario {Id}", usuario.Id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize(Policy = "Administrador")]
        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (User.UsuarioId() == id)
                    return BadRequest(new { error = "No puedes eliminar tu propio usuario" });

                repositorio.Baja(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al eliminar usuario {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CambiarClave([FromBody] CambioClaveView modelo)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var usuario = repositorio.ObtenerPorId(modelo.Id);
                if (usuario == null)
                    return NotFound(new { error = "Usuario no encontrado" });

                if (!string.IsNullOrEmpty(modelo.ClaveVieja))
                {
                    var claveActualHash = HashearClave(modelo.ClaveVieja);
                    if (usuario.Clave != claveActualHash)
                        return BadRequest(new { error = "La contraseña actual es incorrecta" });
                }

                usuario.Clave = HashearClave(modelo.ClaveNueva);
                repositorio.Modificacion(usuario);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al cambiar clave");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CambiarAvatar(int id, IFormFile avatar)
        {
            try
            {
                if (avatar == null || avatar.Length == 0)
                    return BadRequest(new { error = "Debe seleccionar una imagen" });

                var usuario = repositorio.ObtenerPorId(id);
                if (usuario == null)
                    return NotFound(new { error = "Usuario no encontrado" });

                string wwwPath = env.WebRootPath;
                string path = Path.Combine(wwwPath, "Uploads", "Avatars");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                if (!string.IsNullOrEmpty(usuario.Avatar))
                {
                    string rutaAnterior = Path.Combine(wwwPath, usuario.Avatar.TrimStart('/'));
                    if (System.IO.File.Exists(rutaAnterior))
                        System.IO.File.Delete(rutaAnterior);
                }

                string extension = Path.GetExtension(avatar.FileName);
                string fileName = $"avatar_{usuario.Id}{extension}";
                string rutaCompleta = Path.Combine(path, fileName);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                usuario.Avatar = $"/Uploads/Avatars/{fileName}";
                repositorio.Modificacion(usuario);

                return Json(new { success = true, avatarUrl = usuario.Avatar });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al cambiar avatar");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: Usuario/BuscarSocios?term=...
        [HttpGet]
        public IActionResult BuscarSocios(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                    return Json(new List<object>());

                // Buscar usuarios con rol "Socio" (Rol = 3)
                var resultados = repositorio.ObtenerLista(1, int.MaxValue)
                    .Where(u => u.Rol == 3) // Solo socios
                    .Where(u => 
                        u.Dni.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        u.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        u.Apellido.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(u => new
                    {
                        id = u.Id,
                        text = $"{u.Nombre} {u.Apellido} (DNI: {u.Dni})"
                    })
                    .Take(20)
                    .ToList();

                return Json(resultados);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al buscar socios");
                return Json(new List<object>());
            }
        }

        private string HashearClave(string clave)
        {
            string salt = config["Salt"] ?? "";
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: clave,
                salt: Encoding.ASCII.GetBytes(salt),
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 1000,
                numBytesRequested: 256 / 8));
        }
    }
}