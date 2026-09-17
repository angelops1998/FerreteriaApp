using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Helpers;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    [Authorize]
    public class VentasController(ApplicationDbContext context, UserManager<Usuario> userManager) : Controller
    {
        // ---------- CHECKOUT WEB (Cliente.realizarCompra) ----------

        [Authorize(Roles = "Cliente")]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var items = CarritoSesion.Obtener(HttpContext.Session);
            if (items.Count == 0)
            {
                TempData["Info"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Carrito");
            }

            var cliente = (Cliente?)await userManager.GetUserAsync(User);
            if (cliente == null) return Challenge();

            await CargarFormasPagoAsync();
            return View(new CheckoutViewModel
            {
                Items = items,
                DireccionEntrega = cliente.Direccion,
                Telefono = cliente.Telefono
            });
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var items = CarritoSesion.Obtener(HttpContext.Session);
            if (items.Count == 0)
            {
                TempData["Info"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Carrito");
            }

            var cliente = (Cliente?)await userManager.GetUserAsync(User);
            if (cliente == null) return Challenge();

            model.Items = items;
            model.DireccionEntrega = cliente.Direccion;
            model.Telefono = cliente.Telefono;

            if (!ModelState.IsValid)
            {
                await CargarFormasPagoAsync(model.IdFormaPago);
                return View(model);
            }

            // Transacción: o se guarda todo (venta + descuento de stock) o nada
            using var transaction = await context.Database.BeginTransactionAsync();

            var venta = new Venta
            {
                IdCliente = cliente.Id,
                IdFormaPago = model.IdFormaPago,
                Observaciones = model.Observaciones,
                Fecha = DateTime.UtcNow,
                Estado = EstadoVenta.Pendiente
            };

            // Se verifica el stock de cada producto leyendo la base de datos justo antes de confirmar
            var ids = items.Select(i => i.IdProducto).ToList();
            var productos = await context.Productos.Include(p => p.Inventarios)
                .Where(p => ids.Contains(p.IdProducto)).ToListAsync();

            foreach (var item in items)
            {
                var producto = productos.FirstOrDefault(p => p.IdProducto == item.IdProducto);
                if (producto == null || !producto.Estado)
                {
                    ModelState.AddModelError(string.Empty, $"\"{item.Nombre}\" ya no está disponible.");
                    continue;
                }
                try
                {
                    venta.AgregarDetalle(producto, item.Cantidad); // valida stock y usa el precio actual
                    producto.ActualizarStock(-item.Cantidad);       // no permitir vender si no hay stock
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (!ModelState.IsValid || !venta.ValidarDatos())
            {
                await transaction.RollbackAsync();
                if (venta.Detalles.Count == 0) ModelState.AddModelError(string.Empty, "La venta no tiene productos válidos.");
                await CargarFormasPagoAsync(model.IdFormaPago);
                return View(model);
            }

            venta.CalcularTotal();
            context.Ventas.Add(venta);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            CarritoSesion.Vaciar(HttpContext.Session);

            TempData["Exito"] = $"¡Compra #{venta.IdVenta} realizada con éxito! Te contactaremos para coordinar la entrega.";
            return RedirectToAction(nameof(Details), new { id = venta.IdVenta });
        }

        // ---------- MIS COMPRAS (Cliente.verHistorial) ----------

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> MisCompras()
        {
            var idCliente = User.GetUsuarioId();
            var ventas = await context.Ventas
                .AsNoTracking()
                .Include(v => v.Detalles)
                .Include(v => v.FormaPago)
                .Where(v => v.IdCliente == idCliente)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();
            return View(ventas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var venta = await context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .Include(v => v.FormaPago)
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null) return NotFound();

            // Solo el cliente dueño de la venta o el personal pueden verla
            if (venta.IdCliente != User.GetUsuarioId() && !User.EsPersonal())
                return Forbid();

            return View(venta);
        }

        // El cliente puede cancelar su compra mientras esté pendiente
        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var venta = await CargarVentaParaCambioAsync(id);
            if (venta == null || venta.IdCliente != User.GetUsuarioId()) return NotFound();

            if (venta.Estado != EstadoVenta.Pendiente)
            {
                TempData["Error"] = "Solo se pueden cancelar compras pendientes.";
                return RedirectToAction(nameof(Details), new { id });
            }

            CambiarEstado(venta, EstadoVenta.Cancelada, null);
            await context.SaveChangesAsync();

            TempData["Info"] = $"Compra #{venta.IdVenta} cancelada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- GESTIÓN DE VENTAS (Admin y Empleado) ----------

        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Manage(EstadoVenta? estado)
        {
            var query = context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .Include(v => v.FormaPago)
                .Include(v => v.Detalles)
                .AsQueryable();

            if (estado.HasValue)
                query = query.Where(v => v.Estado == estado.Value);

            ViewBag.Estado = estado;
            return View(await query.OrderByDescending(v => v.Fecha).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> CambiarEstado(int id, EstadoVenta estado)
        {
            var venta = await CargarVentaParaCambioAsync(id);
            if (venta == null) return NotFound();

            if (venta.Estado == estado)
                return RedirectToAction(nameof(Details), new { id });

            try
            {
                CambiarEstado(venta, estado, User.GetUsuarioId());
                await context.SaveChangesAsync();
                TempData["Exito"] = $"Venta #{venta.IdVenta} ahora está \"{estado}\".";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Venta registrada por un empleado en el mostrador (Empleado.registrarVenta)
        [Authorize(Roles = "Admin,Empleado")]
        [HttpGet]
        public async Task<IActionResult> Registrar()
        {
            await CargarListasRegistrarAsync();
            return View(new RegistrarVentaViewModel { Items = { new LineaVentaInput() } });
        }

        [Authorize(Roles = "Admin,Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarVentaViewModel model)
        {
            model.Items = model.Items.Where(i => i.IdProducto > 0).ToList();
            if (model.Items.Count == 0)
                ModelState.AddModelError(string.Empty, "Agregá al menos un producto a la venta.");

            if (!ModelState.IsValid)
            {
                await CargarListasRegistrarAsync(model);
                return View(model);
            }

            using var transaction = await context.Database.BeginTransactionAsync();

            var venta = new Venta
            {
                IdCliente = model.IdCliente,
                IdFormaPago = model.IdFormaPago,
                Observaciones = model.Observaciones,
                Fecha = DateTime.UtcNow
            };

            var ids = model.Items.Select(i => i.IdProducto).ToList();
            var productos = await context.Productos.Include(p => p.Inventarios)
                .Where(p => ids.Contains(p.IdProducto)).ToListAsync();

            // Si el mismo producto está en dos filas, se suman las cantidades
            foreach (var grupo in model.Items.GroupBy(i => i.IdProducto))
            {
                var producto = productos.FirstOrDefault(p => p.IdProducto == grupo.Key);
                var cantidad = grupo.Sum(i => i.Cantidad);
                if (producto == null) continue;
                try
                {
                    venta.AgregarDetalle(producto, cantidad);
                    producto.ActualizarStock(-cantidad);
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (!ModelState.IsValid || !venta.ValidarDatos())
            {
                await transaction.RollbackAsync();
                await CargarListasRegistrarAsync(model);
                return View(model);
            }

            venta.CalcularTotal();
            venta.CerrarVenta(User.GetUsuarioId()); // venta de mostrador: queda completada por el empleado
            context.Ventas.Add(venta);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Exito"] = $"Venta #{venta.IdVenta} registrada por {Formato.Precio(venta.Total)}.";
            return RedirectToAction(nameof(Details), new { id = venta.IdVenta });
        }

        // ---------- MÉTODOS AUXILIARES ----------

        private async Task<Venta?> CargarVentaParaCambioAsync(int id) =>
            await context.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto).ThenInclude(p => p!.Inventarios)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

        // Cambia el estado ajustando el stock: Pendiente/Completada lo tienen descontado,
        // Cancelada/Devuelta lo devuelven. Al completar se registra el empleado (cerrarVenta).
        private static void CambiarEstado(Venta venta, EstadoVenta nuevo, int? idEmpleado)
        {
            var descontabaAntes = venta.DescuentaStock();
            var estadoAnterior = venta.Estado;
            venta.Estado = nuevo;
            var descuentaAhora = venta.DescuentaStock();

            try
            {
                if (descontabaAntes && !descuentaAhora)
                    foreach (var d in venta.Detalles) d.Producto?.ActualizarStock(d.Cantidad);
                else if (!descontabaAntes && descuentaAhora)
                    foreach (var d in venta.Detalles) d.Producto?.ActualizarStock(-d.Cantidad);
            }
            catch (InvalidOperationException)
            {
                venta.Estado = estadoAnterior;
                throw new InvalidOperationException("No hay stock suficiente para volver a activar la venta.");
            }

            if (nuevo == EstadoVenta.Completada && idEmpleado.HasValue)
                venta.CerrarVenta(idEmpleado.Value);
        }

        private async Task CargarFormasPagoAsync(int? seleccionada = null)
        {
            var formas = await context.FormasPago.AsNoTracking().Where(f => f.Estado).OrderBy(f => f.Nombre).ToListAsync();
            ViewBag.FormasPago = new SelectList(formas, "IdFormaPago", "Nombre", seleccionada);
        }

        private async Task CargarListasRegistrarAsync(RegistrarVentaViewModel? model = null)
        {
            var clientes = await context.Clientes.AsNoTracking().Where(c => c.Estado).OrderBy(c => c.Apellido).ThenBy(c => c.Nombre)
                .Select(c => new { c.Id, Texto = c.Apellido + ", " + c.Nombre + " (" + c.Email + ")" }).ToListAsync();
            ViewBag.Clientes = new SelectList(clientes, "Id", "Texto", model?.IdCliente);
            await CargarFormasPagoAsync(model?.IdFormaPago);
            ViewBag.Productos = await context.Productos.AsNoTracking().Where(p => p.Estado).OrderBy(p => p.Nombre).ToListAsync();
        }
    }
}
