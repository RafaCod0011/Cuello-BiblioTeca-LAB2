using Cuello_BiblioTeca_LAB2.Models;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
            field => $"El campo '{field}' debe ser un número.");
    });

// Repositorios
builder.Services.AddScoped<IRepositorioUsuario, RepositorioUsuario>();
builder.Services.AddScoped<IRepositorioAutor, RepositorioAutor>();
builder.Services.AddScoped<IRepositorioCategoria, RepositorioCategoria>();
builder.Services.AddScoped<IRepositorioLibro, RepositorioLibro>();
builder.Services.AddScoped<IRepositorioEjemplar, RepositorioEjemplar>();
builder.Services.AddScoped<IRepositorioPrestamo, RepositorioPrestamo>();
builder.Services.AddScoped<IRepositorioReserva, RepositorioReserva>();
builder.Services.AddScoped<IRepositorioMulta, RepositorioMulta>();

// Autenticación y Autorización
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => // Web MVC
    {
        options.LoginPath = "/Usuario/Login";
        options.LogoutPath = "/Usuario/Logout";
        options.AccessDeniedPath = "/Home/Restringido";
        options.Cookie.Name = "CuelloBibliotecaCookie";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(options => // API REST
    {
        var secreto = builder.Configuration["TokenAuthentication:SecretKey"];
        if (string.IsNullOrEmpty(secreto))
            throw new Exception("Falta configurar TokenAuthentication:SecretKey");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["TokenAuthentication:Issuer"],
            ValidAudience = builder.Configuration["TokenAuthentication:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(secreto)),
            
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
         options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // Este evento se activa cuando el token es validado correctamente
                var idUsuario = context?.Principal?.Identity?.Name;
                var rol = context?.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[JWT] ✅ Token Válido. Usuario ID: {idUsuario} | Rol: {rol}");
                Console.ResetColor();
                
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // Este evento se activa cuando el token es inválido (ej. expirado, firma incorrecta)
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[JWT] ❌ Error de Autenticación: {context.Exception.Message}");
                Console.ResetColor();
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrador", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("Bibliotecario", policy => policy.RequireRole("Administrador", "Bibliotecario"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();