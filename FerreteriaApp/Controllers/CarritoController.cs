using Microsoft.AspNetCore.Mvc;
using FerreteriaApp.Data;
using FerreteriaApp.Helpers;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    // Carrito de compras guardado en la sesión (no en la base de datos).
    // Cualquier visitante puede armar su carrito; para confirmar la compra hay que ser Cliente.
    public class CarritoController(ApplicationDbContext context) : Controller
    {
        public IActionResult Index()
        {
            var items = CarritoSesion.Obtener(HttpContext.Session);

            // Se refresca el stock actual de cada producto para avisar si ya no alcanza
            var ids = items.Select(i => i.IdProducto).ToList();
            ViewBag.Stock = context.Productos
                .Where(p => ids.Contains(p.IdProducto))
                .ToDictionary(p => p.IdProducto, p => p.Stock);

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int idProducto, int cantidad = 1, string? returnUrl = null)
        {
            var producto = await context.Productos.FindAsync(idProducto);

            if (producto == null || !producto.Estado)
            {
                TempData["Error"] = "El producto no está disponible.";
                return Volver(returnUrl);
            }

            if (producto.Stock <= 0)
            {
                TempData["Error"] = $"\"{producto.Nombre}\" no tiene stock disponible.";
                return Volver(returnUrl);
            }

            cantidad = Math.Max(1, cantidad);

            var items = CarritoSesion.Obtener(HttpContext.Session);
            var item = items.FirstOrDefault(i => i.IdProducto == idProducto);

            if (item == null)
            {
                item = new CarritoItem
                {
                    IdProducto = producto.IdProducto,
                    Nombre = producto.Nombre,
                    PrecioUnitario = producto.PrecioVenta,
                    ImagenUrl = producto.ImagenUrl,
                    Cantidad = 0
                };
                items.Add(item);
            }

            // No se puede pedir más de lo que hay en stock
            var nuevaCantidad = Math.Min(item.Cantidad + cantidad, producto.Stock);
            if (nuevaCantidad == item.Cantidad)
                TempData["Info"] = $"Ya tenés el máximo disponible de \"{producto.Nombre}\" ({producto.Stock}) en el carrito.";
            else
                TempData["Exito"] = $"\"{producto.Nombre}\" agregado al carrito.";

            item.Cantidad = nuevaCantidad;
            item.PrecioUnitario = producto.PrecioVenta; // por si cambió el precio
            CarritoSesion.Guardar(HttpContext.Session, items);

            return Volver(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int idProducto, int cantidad)
        {
            var items = CarritoSesion.Obtener(HttpContext.Session);
            var item = items.FirstOrDefault(i => i.IdProducto == idProducto);
            if (item == null) return RedirectToAction(nameof(Index));

            if (cantidad <= 0)
            {
                items.Remove(item);
            }
            else
            {
                var producto = await context.Productos.FindAsync(idProducto);
                var stock = producto?.Stock ?? 0;
                item.Cantidad = Math.Min(cantidad, stock);
                if (item.Cantidad < cantidad)
                    TempData["Info"] = $"Solo hay {stock} unidades de \"{item.Nombre}\".";
                if (item.Cantidad == 0) items.Remove(item);
            }

            CarritoSesion.Guardar(HttpContext.Session, items);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Quitar(int idProducto)
        {
            var items = CarritoSesion.Obtener(HttpContext.Session);
            items.RemoveAll(i => i.IdProducto == idProducto);
            CarritoSesion.Guardar(HttpContext.Session, items);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Vaciar()
        {
            CarritoSesion.Vaciar(HttpContext.Session);
            TempData["Info"] = "Carrito vaciado.";
            return RedirectToAction(nameof(Index));
        }

        // Vuelve a la página desde donde se agregó el producto (catálogo o detalle)
        private IActionResult Volver(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Productos");
        }
    }
}
