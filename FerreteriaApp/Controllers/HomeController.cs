using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    public class HomeController(ApplicationDbContext context) : Controller
    {
        // Página de inicio: categorías activas y productos destacados
        public async Task<IActionResult> Index()
        {
            ViewBag.Categorias = await context.Categorias
                .AsNoTracking()
                .Include(c => c.Productos.Where(p => p.Estado))
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var destacados = await context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.Estado && p.Categoria!.Estado && p.Stock > 0)
                .OrderByDescending(p => p.PrecioVenta)
                .Take(8)
                .ToListAsync();

            return View(destacados);
        }

        // Manifest de la PWA: le dice al celular el nombre, los íconos y los colores de la app
        // cuando se la agrega a la pantalla de inicio. Toma el nombre de appsettings.json → "Tienda:Nombre".
        [HttpGet("/manifest.webmanifest")]
        public IActionResult Manifest([FromServices] IConfiguration config)
        {
            var nombre = config["Tienda:Nombre"] ?? "Ferretería";
            var manifest = new
            {
                name = nombre,
                short_name = nombre.Length > 12 ? "Ferretería" : nombre,
                description = $"Catálogo, compras y gestión de {nombre}",
                lang = "es",
                start_url = "/",
                scope = "/",
                display = "standalone",   // se abre sin la barra del navegador, como una app
                background_color = "#212529",
                theme_color = "#212529",
                icons = new object[]
                {
                    new { src = "/icons/icon-192.png", sizes = "192x192", type = "image/png", purpose = "any" },
                    new { src = "/icons/icon-512.png", sizes = "512x512", type = "image/png", purpose = "any" },
                    new { src = "/icons/icon-maskable-512.png", sizes = "512x512", type = "image/png", purpose = "maskable" }
                },
                shortcuts = new object[]
                {
                    new { name = "Catálogo", url = "/Productos", icons = new[] { new { src = "/icons/icon-192.png", sizes = "192x192" } } },
                    new { name = "Carrito", url = "/Carrito", icons = new[] { new { src = "/icons/icon-192.png", sizes = "192x192" } } }
                }
            };
            return new JsonResult(manifest) { ContentType = "application/manifest+json" };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
