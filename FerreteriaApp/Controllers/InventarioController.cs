using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    // Inventario: stock actual, mínimo y última actualización de cada producto (Empleado.gestionarInventario)
    [Authorize(Roles = "Admin,Empleado")]
    public class InventarioController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index(bool soloBajos = false)
        {
            var query = context.Inventarios
                .AsNoTracking()
                .Include(i => i.Producto).ThenInclude(p => p!.Categoria)
                .AsQueryable();

            if (soloBajos)
                query = query.Where(i => i.StockActual <= i.StockMinimo);

            ViewBag.SoloBajos = soloBajos;
            return View(await query.OrderBy(i => i.Producto!.Nombre).ToListAsync());
        }

        // Ajuste manual de stock (por ejemplo, después de un recuento físico)
        [HttpGet]
        public async Task<IActionResult> Ajustar(int id)
        {
            var inventario = await context.Inventarios.Include(i => i.Producto).FirstOrDefaultAsync(i => i.IdInventario == id);
            if (inventario == null) return NotFound();
            return View(inventario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ajustar(int id, int stockActual, int stockMinimo, string? motivo)
        {
            var inventario = await context.Inventarios.Include(i => i.Producto).FirstOrDefaultAsync(i => i.IdInventario == id);
            if (inventario == null) return NotFound();

            if (stockActual < 0 || stockMinimo < 0)
            {
                ModelState.AddModelError(string.Empty, "El stock y el stock mínimo no pueden ser negativos.");
                return View(inventario);
            }

            // El producto y su inventario se mantienen sincronizados
            inventario.ActualizarStock(stockActual - inventario.StockActual);
            inventario.StockMinimo = stockMinimo;
            inventario.Producto!.Stock = stockActual;
            inventario.Producto.StockMinimo = stockMinimo;

            await context.SaveChangesAsync();

            TempData["Exito"] = $"Inventario de \"{inventario.Producto.Nombre}\" ajustado a {stockActual} unidades." +
                                (string.IsNullOrWhiteSpace(motivo) ? "" : $" Motivo: {motivo}");
            return RedirectToAction(nameof(Index));
        }
    }
}
