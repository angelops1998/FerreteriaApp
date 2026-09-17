using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FormasPagoController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var formas = await context.FormasPago.AsNoTracking().Include(f => f.Ventas).OrderBy(f => f.Nombre).ToListAsync();
            return View(formas);
        }

        public IActionResult Create() => View(new FormaPago());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FormaPago formaPago)
        {
            if (!ModelState.IsValid) return View(formaPago);

            if (await context.FormasPago.AnyAsync(f => f.Nombre == formaPago.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe una forma de pago con ese nombre.");
                return View(formaPago);
            }

            context.FormasPago.Add(formaPago);
            await context.SaveChangesAsync();
            TempData["Exito"] = $"Forma de pago \"{formaPago.Nombre}\" creada.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var formaPago = await context.FormasPago.FindAsync(id);
            if (formaPago == null) return NotFound();
            return View(formaPago);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FormaPago formaPago)
        {
            if (id != formaPago.IdFormaPago) return NotFound();
            if (!ModelState.IsValid) return View(formaPago);

            context.FormasPago.Update(formaPago);
            await context.SaveChangesAsync();
            TempData["Exito"] = $"Forma de pago \"{formaPago.Nombre}\" actualizada.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var formaPago = await context.FormasPago.AsNoTracking().Include(f => f.Ventas).FirstOrDefaultAsync(f => f.IdFormaPago == id);
            if (formaPago == null) return NotFound();
            return View(formaPago);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var formaPago = await context.FormasPago.Include(f => f.Ventas).FirstOrDefaultAsync(f => f.IdFormaPago == id);
            if (formaPago == null) return RedirectToAction(nameof(Index));

            if (formaPago.Ventas.Count > 0)
            {
                formaPago.Estado = false;
                TempData["Info"] = $"\"{formaPago.Nombre}\" ya se usó en ventas, por lo que se desactivó en lugar de eliminarse.";
            }
            else
            {
                context.FormasPago.Remove(formaPago);
                TempData["Exito"] = $"Forma de pago \"{formaPago.Nombre}\" eliminada.";
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
