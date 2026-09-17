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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
