using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    // Solo el administrador gestiona las categorías
    [Authorize(Roles = "Admin")]
    public class CategoriasController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var categorias = await context.Categorias
                .AsNoTracking()
                .Include(c => c.Productos)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
            return View(categorias);
        }

        public IActionResult Create() => View(new Categoria());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            if (!ModelState.IsValid) return View(categoria);

            if (await context.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe una categoría con ese nombre.");
                return View(categoria);
            }

            context.Categorias.Add(categoria);
            await context.SaveChangesAsync();
            TempData["Exito"] = $"Categoría \"{categoria.Nombre}\" creada.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var categoria = await context.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // Categoria.actualizar()
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria categoria)
        {
            if (id != categoria.IdCategoria) return NotFound();
            if (!ModelState.IsValid) return View(categoria);

            if (await context.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre && c.IdCategoria != id))
            {
                ModelState.AddModelError("Nombre", "Ya existe una categoría con ese nombre.");
                return View(categoria);
            }

            context.Categorias.Update(categoria);
            await context.SaveChangesAsync();
            TempData["Exito"] = $"Categoría \"{categoria.Nombre}\" actualizada.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await context.Categorias
                .AsNoTracking()
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.IdCategoria == id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await context.Categorias.Include(c => c.Productos).FirstOrDefaultAsync(c => c.IdCategoria == id);
            if (categoria == null) return RedirectToAction(nameof(Index));

            // La categoría es obligatoria en cada producto: si tiene productos, se desactiva en vez de borrarse
            if (categoria.Productos.Count > 0)
            {
                categoria.Estado = false;
                TempData["Info"] = $"\"{categoria.Nombre}\" tiene productos asociados, por lo que se desactivó en lugar de eliminarse.";
            }
            else
            {
                context.Categorias.Remove(categoria);
                TempData["Exito"] = $"Categoría \"{categoria.Nombre}\" eliminada.";
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
