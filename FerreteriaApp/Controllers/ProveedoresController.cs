using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProveedoresController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var proveedores = await context.Proveedores
                .AsNoTracking()
                .Include(p => p.Productos)
                .Include(p => p.Compras)
                .OrderBy(p => p.Nombre)
                .ToListAsync();
            return View(proveedores);
        }

        public IActionResult Create() => View(new Proveedor());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            if (!ModelState.IsValid) return View(proveedor);

            if (await context.Proveedores.AnyAsync(p => p.Ruc == proveedor.Ruc))
            {
                ModelState.AddModelError("Ruc", "Ya existe un proveedor con ese RUC/NIT.");
                return View(proveedor);
            }

            context.Proveedores.Add(proveedor);
            await context.SaveChangesAsync();
            TempData["Exito"] = $"Proveedor \"{proveedor.Nombre}\" creado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var proveedor = await context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        // Proveedor.actualizar()
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Proveedor proveedor)
        {
            if (id != proveedor.IdProveedor) return NotFound();
            if (!ModelState.IsValid) return View(proveedor);

            if (await context.Proveedores.AnyAsync(p => p.Ruc == proveedor.Ruc && p.IdProveedor != id))
            {
                ModelState.AddModelError("Ruc", "Ya existe un proveedor con ese RUC/NIT.");
                return View(proveedor);
            }

            context.Proveedores.Update(proveedor);
            await context.SaveChangesAsync();
            TempData["Exito"] = $"Proveedor \"{proveedor.Nombre}\" actualizado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var proveedor = await context.Proveedores
                .AsNoTracking()
                .Include(p => p.Productos)
                .Include(p => p.Compras)
                .FirstOrDefaultAsync(p => p.IdProveedor == id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await context.Proveedores.Include(p => p.Productos).Include(p => p.Compras).FirstOrDefaultAsync(p => p.IdProveedor == id);
            if (proveedor == null) return RedirectToAction(nameof(Index));

            // Si tiene productos o compras asociadas, se desactiva (no se puede borrar por las claves foráneas)
            if (proveedor.Productos.Count > 0 || proveedor.Compras.Count > 0)
            {
                proveedor.Estado = false;
                TempData["Info"] = $"\"{proveedor.Nombre}\" tiene productos o compras asociadas, por lo que se desactivó en lugar de eliminarse.";
            }
            else
            {
                context.Proveedores.Remove(proveedor);
                TempData["Exito"] = $"Proveedor \"{proveedor.Nombre}\" eliminado.";
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
