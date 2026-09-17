using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    // Catálogo público + administración de productos (Admin y Empleado).
    // "env" se usa para saber la carpeta wwwroot donde se guardan las imágenes subidas.
    public class ProductosController(ApplicationDbContext context, IWebHostEnvironment env) : Controller
    {
        private const int ProductosPorPagina = 12;
        private static readonly string[] ExtensionesPermitidas = { ".png", ".jpg", ".jpeg", ".webp" };

        // ---------- CATÁLOGO (público) ----------

        public async Task<IActionResult> Index(string? buscar, int? categoriaId, int pagina = 1)
        {
            var query = context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.Estado && p.Categoria!.Estado);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                // ILike = LIKE sin distinguir mayúsculas/minúsculas (específico de PostgreSQL)
                var patron = $"%{buscar.Trim()}%";
                query = query.Where(p => EF.Functions.ILike(p.Nombre, patron) || EF.Functions.ILike(p.Descripcion, patron));
            }

            if (categoriaId.HasValue)
                query = query.Where(p => p.IdCategoria == categoriaId.Value);

            var total = await query.CountAsync();
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)ProductosPorPagina));
            pagina = Math.Clamp(pagina, 1, totalPaginas);

            var productos = await query
                .OrderBy(p => p.Nombre)
                .Skip((pagina - 1) * ProductosPorPagina)
                .Take(ProductosPorPagina)
                .ToListAsync();

            ViewBag.Categorias = await context.Categorias.AsNoTracking().Where(c => c.Estado).OrderBy(c => c.Nombre).ToListAsync();
            ViewBag.Buscar = buscar;
            ViewBag.CategoriaId = categoriaId;
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.Total = total;

            return View(productos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var producto = await context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(p => p.IdProducto == id);

            if (producto == null) return NotFound();

            // Un producto inactivo solo lo puede ver el personal
            if (!producto.Estado && !User.IsInRole("Admin") && !User.IsInRole("Empleado")) return NotFound();

            ViewBag.Relacionados = await context.Productos
                .AsNoTracking()
                .Where(p => p.Estado && p.IdProducto != id && p.IdCategoria == producto.IdCategoria)
                .OrderBy(p => p.Nombre)
                .Take(4)
                .ToListAsync();

            return View(producto);
        }

        // ---------- ADMINISTRACIÓN (Admin y Empleado) ----------

        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Manage(string? buscar)
        {
            var query = context.Productos.AsNoTracking().Include(p => p.Categoria).Include(p => p.Proveedor).AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(p => EF.Functions.ILike(p.Nombre, $"%{buscar.Trim()}%"));

            ViewBag.Buscar = buscar;
            return View(await query.OrderBy(p => p.Nombre).ToListAsync());
        }

        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View(new Producto { StockMinimo = 5 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Create(Producto producto)
        {
            if (producto.ImagenArchivo != null && !ImagenValida(producto.ImagenArchivo))
                ModelState.AddModelError("ImagenArchivo", "Solo se permiten imágenes PNG, JPG o WEBP de hasta 5 MB.");

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(producto.IdCategoria, producto.IdProveedor);
                return View(producto);
            }

            if (producto.ImagenArchivo != null)
                producto.ImagenUrl = await GuardarImagenAsync(producto.ImagenArchivo);

            if (string.IsNullOrWhiteSpace(producto.ImagenUrl))
                producto.ImagenUrl = "/images/default-product.png";

            // Todo producto nace con su registro de inventario
            producto.Inventarios.Add(new Inventario
            {
                StockActual = producto.Stock,
                StockMinimo = producto.StockMinimo,
                UltimaActualizacion = DateTime.UtcNow
            });

            context.Productos.Add(producto);
            await context.SaveChangesAsync();

            TempData["Exito"] = $"Producto \"{producto.Nombre}\" creado correctamente.";
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Edit(int id)
        {
            var producto = await context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            await CargarListasAsync(producto.IdCategoria, producto.IdProveedor);
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            if (id != producto.IdProducto) return NotFound();

            if (producto.ImagenArchivo != null && !ImagenValida(producto.ImagenArchivo))
                ModelState.AddModelError("ImagenArchivo", "Solo se permiten imágenes PNG, JPG o WEBP de hasta 5 MB.");

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(producto.IdCategoria, producto.IdProveedor);
                return View(producto);
            }

            // Se carga el producto original y se copian solo los campos editables,
            // así no se pierde la imagen si no se subió una nueva.
            var original = await context.Productos.Include(p => p.Inventarios).FirstOrDefaultAsync(p => p.IdProducto == id);
            if (original == null) return NotFound();

            original.Nombre = producto.Nombre;
            original.Descripcion = producto.Descripcion;
            original.PrecioVenta = producto.PrecioVenta;
            original.PrecioCompra = producto.PrecioCompra;
            original.IdCategoria = producto.IdCategoria;
            original.IdProveedor = producto.IdProveedor;
            original.Estado = producto.Estado;
            original.StockMinimo = producto.StockMinimo;

            // Si cambió el stock a mano, se ajusta también el inventario (queda registrada la fecha)
            if (original.Stock != producto.Stock)
                original.ActualizarStock(producto.Stock - original.Stock);
            foreach (var inv in original.Inventarios)
                inv.StockMinimo = producto.StockMinimo;

            if (producto.ImagenArchivo != null)
                original.ImagenUrl = await GuardarImagenAsync(producto.ImagenArchivo);
            else if (!string.IsNullOrWhiteSpace(producto.ImagenUrl))
                original.ImagenUrl = producto.ImagenUrl;

            await context.SaveChangesAsync();

            TempData["Exito"] = $"Producto \"{original.Nombre}\" actualizado.";
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await context.Productos.AsNoTracking().Include(p => p.Categoria).FirstOrDefaultAsync(p => p.IdProducto == id);
            if (producto == null) return NotFound();

            ViewBag.TieneMovimientos = await TieneMovimientosAsync(id);
            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await context.Productos.FindAsync(id);
            if (producto == null) return RedirectToAction(nameof(Manage));

            // Si el producto aparece en ventas o compras no se puede borrar (se perdería el historial):
            // en ese caso se desactiva para que deje de mostrarse en el catálogo.
            if (await TieneMovimientosAsync(id))
            {
                producto.Estado = false;
                TempData["Info"] = $"\"{producto.Nombre}\" tiene ventas o compras registradas, por lo que se desactivó en lugar de eliminarse.";
            }
            else
            {
                context.Productos.Remove(producto); // el inventario se borra en cascada
                TempData["Exito"] = $"Producto \"{producto.Nombre}\" eliminado.";
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }

        // ---------- MÉTODOS AUXILIARES ----------

        private async Task<bool> TieneMovimientosAsync(int idProducto) =>
            await context.DetalleVentas.AnyAsync(d => d.IdProducto == idProducto) ||
            await context.DetalleCompras.AnyAsync(d => d.IdProducto == idProducto);

        private async Task CargarListasAsync(int? categoria = null, int? proveedor = null)
        {
            var categorias = await context.Categorias.AsNoTracking().Where(c => c.Estado).OrderBy(c => c.Nombre).ToListAsync();
            var proveedores = await context.Proveedores.AsNoTracking().Where(p => p.Estado).OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.Categorias = new SelectList(categorias, "IdCategoria", "Nombre", categoria);
            ViewBag.Proveedores = new SelectList(proveedores, "IdProveedor", "Nombre", proveedor);
        }

        private static bool ImagenValida(IFormFile archivo)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            return ExtensionesPermitidas.Contains(extension) && archivo.Length > 0 && archivo.Length <= 5 * 1024 * 1024;
        }

        // Guarda la imagen en wwwroot/images/productos con un nombre único y devuelve la URL pública
        private async Task<string> GuardarImagenAsync(IFormFile archivo)
        {
            var carpeta = Path.Combine(env.WebRootPath, "images", "productos");
            Directory.CreateDirectory(carpeta);

            var nombre = $"{Guid.NewGuid():N}{Path.GetExtension(archivo.FileName).ToLowerInvariant()}";
            var ruta = Path.Combine(carpeta, nombre);

            using (var stream = new FileStream(ruta, FileMode.Create))
                await archivo.CopyToAsync(stream);

            return $"/images/productos/{nombre}";
        }
    }
}
