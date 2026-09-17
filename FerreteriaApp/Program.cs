using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Helpers;
using FerreteriaApp.Models;

// Cultura invariante en toda la app: los precios se leen y escriben siempre con punto decimal (99.50),
// sin importar el idioma de Windows o del servidor. La moneda se muestra con el helper Formato.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Render (y otros hostings) indican por la variable de entorno PORT en qué puerto escuchar
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity con nuestras clases Usuario y Rol (claves int).
// Reglas de contraseña del diagrama: mínimo 8 caracteres, con mayúsculas, minúsculas y números.
builder.Services.AddIdentity<Usuario, Rol>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>() // mensajes de Identity en español
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// Sesión: acá se guarda el carrito de compras (no es una tabla de la base de datos)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews(options =>
{
    // Mensajes genéricos del model binding en español (ej. cuando un combo llega vacío o un número es inválido)
    var m = options.ModelBindingMessageProvider;
    m.SetValueMustNotBeNullAccessor(_ => "Este campo es obligatorio.");
    m.SetMissingBindRequiredValueAccessor(campo => $"El campo '{campo}' es obligatorio.");
    m.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
    m.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    m.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    m.SetUnknownValueIsInvalidAccessor(campo => $"El valor ingresado no es válido para {campo}.");
    m.SetMissingKeyOrValueAccessor(() => "Falta un valor obligatorio.");
    m.SetNonPropertyValueMustBeANumberAccessor(() => "Debe ser un número.");
    m.SetValueMustBeANumberAccessor(campo => $"{campo} debe ser un número.");
});

// Moneda y zona horaria de la tienda (se configuran en appsettings.json → "Tienda")
Formato.Moneda = builder.Configuration["Tienda:Moneda"] ?? "Bs";
Formato.ZonaHoraria = builder.Configuration["Tienda:ZonaHoraria"] ?? "America/La_Paz";

var app = builder.Build();

// En Render la app corre detrás de un proxy que maneja el HTTPS:
// con esto la app reconoce que la petición original fue https (cookies seguras, redirecciones correctas)
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownIPNetworks.Clear(); // el proxy de Render no es localhost
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Al arrancar: aplica las migraciones pendientes y carga los datos iniciales
// (roles, administrador, formas de pago, categorías, proveedores, productos e inventario).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    await DbInitializer.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.Run();
