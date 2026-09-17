using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Helpers;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    // Compras de mercadería a proveedores (Empleado.registrarCompra). Al completarse suben el stock.
    [Authorize(Roles = "Admin,Empleado")]
    public class ComprasController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index(EstadoCompra? estado)
        {
            var query = context.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Empleado)
                .Include(c => c.Detalles)
                .AsQueryable();

            if (estado.HasValue)
                query = query.Where(c => c.Estado == estado.Value);

            ViewBag.Estado = estado;
            return View(await query.OrderByDescending(c => c.Fecha).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var compra = await context.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Empleado)
                .Include(c => c.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(c => c.IdCompra == id);

            if (compra == null) return NotFound();
            return View(compra);
        }

        [HttpGet]
        public async Task<IActionResult> Registrar()
        {
            await CargarListasAsync();
            return View(new RegistrarCompraViewModel { Items = { new LineaCompraInput() } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarCompraViewModel model)
        {
            model.Items = model.Items.Where(i => i.IdProducto > 0).ToList();
            if (model.Items.Count == 0)
                ModelState.AddModelError(string.Empty, "Agregá al menos un producto a la compra.");

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(model);
                return View(model);
            }

            var compra = new Compra
            {
                IdProveedor = model.IdProveedor,
                IdEmpleado = User.GetUsuarioId(),
                Observaciones = model.Observaciones,
                Fecha = DateTime.UtcNow,
                Estado = EstadoCompra.Pendiente
            };

            var ids = model.Items.Select(i => i.IdProducto).ToList();
            var productos = await context.Productos.Where(p => ids.Contains(p.IdProducto)).ToListAsync();

            foreach (var linea in model.Items)
            {
                var producto = productos.FirstOrDefault(p => p.IdProducto == linea.IdProducto);
                if (producto == null) continue;
                try
                {
                    compra.AgregarDetalle(producto, linea.Cantidad, linea.PrecioUnitario);
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError(string.Empty, $"{producto.Nombre}: {ex.Message}");
                }
            }

            if (!ModelState.IsValid || !compra.ValidarDatos())
            {
                await CargarListasAsync(model);
                return View(model);
            }

            compra.CalcularTotal();
            context.Compras.Add(compra);
            await context.SaveChangesAsync();

            TempData["Exito"] = $"Compra #{compra.IdCompra} registrada por {Formato.Precio(compra.Total)}. Al completarla se sumará el stock.";
            return RedirectToAction(nameof(Details), new { id = compra.IdCompra });
        }

        // Pendiente -> Completada: suma el stock. Completada -> Cancelada: lo resta (sin dejarlo negativo).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, EstadoCompra estado)
        {
            var compra = await context.Compras
                .Include(c => c.Detalles).ThenInclude(d => d.Producto).ThenInclude(p => p!.Inventarios)
                .FirstOrDefaultAsync(c => c.IdCompra == id);

            if (compra == null) return NotFound();
            if (compra.Estado == estado) return RedirectToAction(nameof(Details), new { id });

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var sumabaAntes = compra.Estado == EstadoCompra.Completada;
                var sumaAhora = estado == EstadoCompra.Completada;

                if (!sumabaAntes && sumaAhora)
                {
                    // Ingresa la mercadería: sube el stock y se actualiza el precio de compra del producto
                    foreach (var d in compra.Detalles)
                    {
                        d.Producto!.ActualizarStock(d.Cantidad);
                        d.Producto.PrecioCompra = d.PrecioUnitario;
                    }
                }
                else if (sumabaAntes && !sumaAhora)
                {
                    // Se anula una compra ya ingresada: se descuenta, pero no se permite stock negativo
                    foreach (var d in compra.Detalles)
                        d.Producto!.ActualizarStock(-d.Cantidad);
                }

                compra.Estado = estado;
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Exito"] = $"Compra #{compra.IdCompra} ahora está \"{estado}\".";
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"No se puede cambiar el estado: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task CargarListasAsync(RegistrarCompraViewModel? model = null)
        {
            var proveedores = await context.Proveedores.AsNoTracking().Where(p => p.Estado).OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.Proveedores = new SelectList(proveedores, "IdProveedor", "Nombre", model?.IdProveedor);
            ViewBag.Productos = await context.Productos.AsNoTracking().Where(p => p.Estado).OrderBy(p => p.Nombre).ToListAsync();
        }
    }
}
