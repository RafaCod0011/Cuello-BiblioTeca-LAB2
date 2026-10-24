using Cuello_BiblioTeca_LAB2.Models;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;

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
    .AddCookie(options =>
    {
        options.LoginPath = "/Usuario/Login";
        options.LogoutPath = "/Usuario/Logout";
        options.AccessDeniedPath = "/Home/Restringido";
        options.Cookie.Name = "CuelloBibliotecaCookie";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
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