using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Helpers;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    // Sección de reportes (Admin y Empleado). Cada reporte se puede ver en pantalla,
    // imprimir (o guardar como PDF desde el navegador) y exportar a CSV con ?formato=csv.
    [Authorize(Roles = "Admin,Empleado")]
    public class ReportesController(ApplicationDbContext context) : Controller
    {
        public IActionResult Index() => View();

        // ---------- VENTAS ----------

        public async Task<IActionResult> Ventas(DateTime? desde, DateTime? hasta, EstadoVenta? estado, int? idFormaPago, string? formato)
        {
            var (inicio, fin, inicioUtc, finUtc) = RangoFechas(desde, hasta);
            var model = new ReporteVentasViewModel { Desde = inicio, Hasta = fin, Estado = estado, IdFormaPago = idFormaPago };

            var query = context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .Include(v => v.FormaPago)
                .Include(v => v.Detalles)
                .Where(v => v.Fecha >= inicioUtc && v.Fecha < finUtc);

            if (estado.HasValue) query = query.Where(v => v.Estado == estado.Value);
            if (idFormaPago.HasValue) query = query.Where(v => v.IdFormaPago == idFormaPago.Value);

            model.Ventas = await query.OrderBy(v => v.Fecha).ToListAsync();

            model.PorEstado = Agrupar(model.Ventas, v => v.Estado.ToString(), v => v.Total);
            var completadas = model.Ventas.Where(v => v.Estado == EstadoVenta.Completada).ToList();
            model.PorFormaPago = Agrupar(completadas, v => v.FormaPago?.Nombre ?? "—", v => v.Total);
            model.PorEmpleado = Agrupar(completadas, v => v.Empleado?.NombreCompleto ?? "Sin asignar", v => v.Total);
            model.PorDia = AgruparPorDia(completadas, v => v.Fecha, v => v.Total);

            if (formato == "csv")
            {
                var filas = model.Ventas.Select(v => new object?[]
                {
                    v.IdVenta, v.Fecha, v.Cliente?.NombreCompleto, v.Cliente?.Email, v.Empleado?.NombreCompleto ?? "",
                    v.FormaPago?.Nombre, v.Detalles.Sum(d => d.Cantidad), v.Total, v.Estado, v.Observaciones
                });
                return ArchivoCsv(["N°", "Fecha", "Cliente", "Email", "Empleado", "Forma de pago", "Unidades", "Total", "Estado", "Observaciones"], filas, $"ventas_{inicio:yyyyMMdd}_{fin:yyyyMMdd}");
            }

            ViewBag.FormasPago = new SelectList(await context.FormasPago.OrderBy(f => f.Nombre).ToListAsync(), "IdFormaPago", "Nombre", idFormaPago);
            return View(model);
        }

        // ---------- COMPRAS ----------

        public async Task<IActionResult> Compras(DateTime? desde, DateTime? hasta, EstadoCompra? estado, int? idProveedor, string? formato)
        {
            var (inicio, fin, inicioUtc, finUtc) = RangoFechas(desde, hasta);
            var model = new ReporteComprasViewModel { Desde = inicio, Hasta = fin, Estado = estado, IdProveedor = idProveedor };

            var query = context.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Empleado)
                .Include(c => c.Detalles)
                .Where(c => c.Fecha >= inicioUtc && c.Fecha < finUtc);

            if (estado.HasValue) query = query.Where(c => c.Estado == estado.Value);
            if (idProveedor.HasValue) query = query.Where(c => c.IdProveedor == idProveedor.Value);

            model.Compras = await query.OrderBy(c => c.Fecha).ToListAsync();

            model.PorEstado = Agrupar(model.Compras, c => c.Estado.ToString(), c => c.Total);
            var completadas = model.Compras.Where(c => c.Estado == EstadoCompra.Completada).ToList();
            model.PorProveedor = Agrupar(completadas, c => c.Proveedor?.Nombre ?? "—", c => c.Total);
            model.PorEmpleado = Agrupar(completadas, c => c.Empleado?.NombreCompleto ?? "—", c => c.Total);
            model.PorDia = AgruparPorDia(completadas, c => c.Fecha, c => c.Total);

            if (formato == "csv")
            {
                var filas = model.Compras.Select(c => new object?[]
                {
                    c.IdCompra, c.Fecha, c.Proveedor?.Nombre, c.Proveedor?.Ruc, c.Empleado?.NombreCompleto,
                    c.Detalles.Sum(d => d.Cantidad), c.Total, c.Estado, c.Observaciones
                });
                return ArchivoCsv(["N°", "Fecha", "Proveedor", "RUC/NIT", "Empleado", "Unidades", "Total", "Estado", "Observaciones"], filas, $"compras_{inicio:yyyyMMdd}_{fin:yyyyMMdd}");
            }

            ViewBag.Proveedores = new SelectList(await context.Proveedores.OrderBy(p => p.Nombre).ToListAsync(), "IdProveedor", "Nombre", idProveedor);
            return View(model);
        }

        // ---------- INVENTARIO ----------

        public async Task<IActionResult> Inventario(int? idCategoria, bool soloBajos, bool incluirInactivos, string? formato)
        {
            var model = new ReporteInventarioViewModel { IdCategoria = idCategoria, SoloBajos = soloBajos, IncluirInactivos = incluirInactivos };

            var query = context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .AsQueryable();

            if (!incluirInactivos) query = query.Where(p => p.Estado);
            if (idCategoria.HasValue) query = query.Where(p => p.IdCategoria == idCategoria.Value);
            if (soloBajos) query = query.Where(p => p.Stock <= p.StockMinimo);

            model.Productos = await query
                .OrderBy(p => p.Categoria!.Nombre).ThenBy(p => p.Nombre)
                .ToListAsync();

            model.PorCategoria = model.Productos
                .GroupBy(p => p.Categoria?.Nombre ?? "—")
                .Select(g => new ResumenFila { Nombre = g.Key, Cantidad = g.Sum(p => p.Stock), Total = g.Sum(p => p.Stock * p.PrecioCompra) })
                .OrderByDescending(r => r.Total)
                .ToList();

            if (formato == "csv")
            {
                var filas = model.Productos.Select(p => new object?[]
                {
                    p.IdProducto, p.Nombre, p.Categoria?.Nombre, p.Proveedor?.Nombre, p.Stock, p.StockMinimo,
                    p.PrecioCompra, p.PrecioVenta, p.Stock * p.PrecioCompra, p.Stock * p.PrecioVenta,
                    p.VerificarStock() ? "OK" : (p.Stock == 0 ? "Sin stock" : "Bajo mínimo"), p.Estado
                });
                return ArchivoCsv(["ID", "Producto", "Categoría", "Proveedor", "Stock", "Stock mínimo", "Precio compra", "Precio venta", "Valor a costo", "Valor a venta", "Situación", "Activo"], filas, "inventario");
            }

            ViewBag.Categorias = new SelectList(await context.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "IdCategoria", "Nombre", idCategoria);
            return View(model);
        }

        // ---------- PRODUCTOS VENDIDOS / RENTABILIDAD ----------

        public async Task<IActionResult> Productos(DateTime? desde, DateTime? hasta, int? idCategoria, string orden = "unidades", string? formato = null)
        {
            var (inicio, fin, inicioUtc, finUtc) = RangoFechas(desde, hasta);
            var model = new ReporteProductosViewModel { Desde = inicio, Hasta = fin, IdCategoria = idCategoria, Orden = orden };

            // Solo detalles de ventas completadas dentro del rango
            var detalles = await context.DetalleVentas
                .AsNoTracking()
                .Include(d => d.Producto).ThenInclude(p => p!.Categoria)
                .Where(d => d.Venta!.Estado == EstadoVenta.Completada
                         && d.Venta.Fecha >= inicioUtc && d.Venta.Fecha < finUtc)
                .ToListAsync();

            if (idCategoria.HasValue)
                detalles = detalles.Where(d => d.Producto!.IdCategoria == idCategoria.Value).ToList();

            var vendidos = detalles
                .GroupBy(d => d.IdProducto)
                .Select(g => new FilaProductoVendido
                {
                    Producto = g.First().Producto!,
                    Unidades = g.Sum(d => d.Cantidad),
                    CantidadVentas = g.Select(d => d.IdVenta).Distinct().Count(),
                    Ingresos = g.Sum(d => d.Subtotal),
                    Costo = g.Sum(d => d.Cantidad) * g.First().Producto!.PrecioCompra
                });

            model.Vendidos = (orden switch
            {
                "ingresos" => vendidos.OrderByDescending(v => v.Ingresos),
                "ganancia" => vendidos.OrderByDescending(v => v.Ganancia),
                _ => vendidos.OrderByDescending(v => v.Unidades)
            }).ThenBy(v => v.Producto.Nombre).ToList();

            // Productos activos que no aparecen en ninguna venta del período
            var idsVendidos = model.Vendidos.Select(v => v.Producto.IdProducto).ToList();
            var sinVentas = context.Productos.AsNoTracking().Include(p => p.Categoria)
                .Where(p => p.Estado && !idsVendidos.Contains(p.IdProducto));
            if (idCategoria.HasValue) sinVentas = sinVentas.Where(p => p.IdCategoria == idCategoria.Value);
            model.SinVentas = await sinVentas.OrderBy(p => p.Nombre).ToListAsync();

            if (formato == "csv")
            {
                var filas = model.Vendidos.Select(v => new object?[]
                {
                    v.Producto.IdProducto, v.Producto.Nombre, v.Producto.Categoria?.Nombre, v.Unidades, v.CantidadVentas,
                    v.Ingresos, v.Costo, v.Ganancia, v.Margen
                });
                return ArchivoCsv(["ID", "Producto", "Categoría", "Unidades", "Ventas", "Ingresos", "Costo estimado", "Ganancia", "Margen %"], filas, $"productos_{inicio:yyyyMMdd}_{fin:yyyyMMdd}");
            }

            ViewBag.Categorias = new SelectList(await context.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "IdCategoria", "Nombre", idCategoria);
            return View(model);
        }

        // ---------- USUARIOS ----------

        public async Task<IActionResult> Usuarios(DateTime? desde, DateTime? hasta, string? rol, bool? activo, bool soloNuevos, string? formato)
        {
            var (inicio, fin, inicioUtc, finUtc) = RangoFechas(desde, hasta);
            var model = new ReporteUsuariosViewModel { Desde = inicio, Hasta = fin, Rol = rol, Activo = activo, SoloNuevos = soloNuevos };

            // El rol vive en la tabla UsuarioRoles (Identity): se trae todo junto en una sola consulta
            var rolesPorUsuario = await (from ur in context.UserRoles
                                         join r in context.Roles on ur.RoleId equals r.Id
                                         select new { ur.UserId, Rol = r.Name })
                                        .ToDictionaryAsync(x => x.UserId, x => x.Rol ?? "");

            // Con herencia TPT, EF devuelve cada usuario como Empleado o Cliente según corresponda
            var usuarios = await context.Users.AsNoTracking().ToListAsync();

            // Compras de los clientes dentro del período (solo ventas completadas)
            var compras = (await context.Ventas.AsNoTracking()
                    .Where(v => v.Estado == EstadoVenta.Completada && v.Fecha >= inicioUtc && v.Fecha < finUtc)
                    .Select(v => new { v.IdCliente, v.Fecha, v.Total })
                    .ToListAsync())
                .GroupBy(v => v.IdCliente)
                .ToDictionary(g => g.Key, g => new { Cantidad = g.Count(), Total = g.Sum(v => v.Total), Ultima = g.Max(v => v.Fecha) });

            var filas = usuarios.Select(u =>
            {
                compras.TryGetValue(u.Id, out var c);
                return new FilaUsuario
                {
                    Usuario = u,
                    Rol = rolesPorUsuario.GetValueOrDefault(u.Id, "Sin rol"),
                    Detalle = u is Empleado e ? e.Cargo : (u as Cliente)?.TipoCliente,
                    Telefono = u is Empleado emp ? emp.Telefono : (u as Cliente)?.Telefono,
                    CantidadCompras = c?.Cantidad ?? 0,
                    TotalComprado = c?.Total ?? 0m,
                    UltimaCompra = c?.Ultima
                };
            }).ToList();

            // Las altas del período se cuentan sobre todos los usuarios, aunque después se filtre la lista
            model.Nuevos = filas.Count(f => f.Usuario.FechaCreacion >= inicioUtc && f.Usuario.FechaCreacion < finUtc);

            model.PorRol = filas
                .GroupBy(f => f.Rol)
                .Select(g => new ResumenFila { Nombre = g.Key, Cantidad = g.Count(), Total = g.Sum(f => f.TotalComprado) })
                .OrderByDescending(r => r.Cantidad)
                .ToList();

            model.AltasPorMes = filas
                .Where(f => f.Usuario.FechaCreacion >= inicioUtc && f.Usuario.FechaCreacion < finUtc)
                .GroupBy(f => new DateTime(Formato.ALocal(f.Usuario.FechaCreacion).Year, Formato.ALocal(f.Usuario.FechaCreacion).Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new ResumenFila { Nombre = g.Key.ToString("MM/yyyy"), Cantidad = g.Count(), Total = 0m })
                .ToList();

            if (!string.IsNullOrEmpty(rol)) filas = filas.Where(f => f.Rol == rol).ToList();
            if (activo.HasValue) filas = filas.Where(f => f.Usuario.Estado == activo.Value).ToList();
            if (soloNuevos) filas = filas.Where(f => f.Usuario.FechaCreacion >= inicioUtc && f.Usuario.FechaCreacion < finUtc).ToList();

            model.Usuarios = filas
                .OrderByDescending(f => f.TotalComprado)
                .ThenBy(f => f.Usuario.Apellido).ThenBy(f => f.Usuario.Nombre)
                .ToList();

            if (formato == "csv")
            {
                var csv = model.Usuarios.Select(f => new object?[]
                {
                    f.Usuario.Id, f.Usuario.NombreCompleto, f.Usuario.Email, f.Rol, f.Detalle, f.Telefono,
                    f.Usuario.Estado, f.Usuario.FechaCreacion, f.CantidadCompras, f.TotalComprado, f.UltimaCompra
                });
                return ArchivoCsv(["ID", "Usuario", "Email", "Rol", "Cargo / Tipo", "Teléfono", "Activo", "Fecha de alta", "Compras", "Total comprado", "Última compra"], csv, $"usuarios_{inicio:yyyyMMdd}_{fin:yyyyMMdd}");
            }

            return View(model);
        }

        // ---------- RESUMEN FINANCIERO MENSUAL ----------

        public async Task<IActionResult> Financiero(int? anio, string? formato)
        {
            var hoy = Formato.ALocal(DateTime.UtcNow);
            var model = new ReporteFinancieroViewModel { Anio = anio ?? hoy.Year };

            var inicio = Formato.AUtc(new DateTime(model.Anio, 1, 1));
            var fin = Formato.AUtc(new DateTime(model.Anio + 1, 1, 1));

            var ventas = await context.Ventas.AsNoTracking()
                .Where(v => v.Estado == EstadoVenta.Completada && v.Fecha >= inicio && v.Fecha < fin)
                .Select(v => new { v.Fecha, v.Total })
                .ToListAsync();

            var compras = await context.Compras.AsNoTracking()
                .Where(c => c.Estado == EstadoCompra.Completada && c.Fecha >= inicio && c.Fecha < fin)
                .Select(c => new { c.Fecha, c.Total })
                .ToListAsync();

            // Los 12 meses del año, aunque no tengan movimientos (así la tabla siempre se ve completa)
            for (int mes = 1; mes <= 12; mes++)
            {
                var vm = ventas.Where(v => Formato.ALocal(v.Fecha).Month == mes).ToList();
                var cm = compras.Where(c => Formato.ALocal(c.Fecha).Month == mes).ToList();
                model.Meses.Add(new FilaMes
                {
                    Mes = mes,
                    CantidadVentas = vm.Count,
                    Ventas = vm.Sum(v => v.Total),
                    CantidadCompras = cm.Count,
                    Compras = cm.Sum(c => c.Total)
                });
            }

            // Años con movimientos para el combo (siempre incluye el actual)
            var fechas = await context.Ventas.Select(v => v.Fecha).Union(context.Compras.Select(c => c.Fecha)).ToListAsync();
            model.Anios = fechas.Select(f => Formato.ALocal(f).Year).Append(hoy.Year).Append(model.Anio)
                .Distinct().OrderByDescending(a => a).ToList();

            if (formato == "csv")
            {
                var filas = model.Meses.Select(m => new object?[]
                {
                    m.Nombre, m.CantidadVentas, m.Ventas, m.CantidadCompras, m.Compras, m.Diferencia
                });
                return ArchivoCsv(["Mes", "Cant. ventas", "Ventas", "Cant. compras", "Compras", "Diferencia"], filas, $"financiero_{model.Anio}");
            }

            return View(model);
        }

        // ---------- AUXILIARES ----------

        // Rango por defecto: desde el primer día del mes actual hasta hoy (en hora local de la tienda).
        // Devuelve también los límites en UTC para comparar contra la base de datos:
        // finUtc es el inicio del día siguiente, así el día "hasta" entra completo.
        private static (DateTime inicio, DateTime fin, DateTime inicioUtc, DateTime finUtc) RangoFechas(DateTime? desde, DateTime? hasta)
        {
            var hoy = Formato.ALocal(DateTime.UtcNow).Date;
            var inicio = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
            var fin = (hasta ?? hoy).Date;
            if (fin < inicio) (inicio, fin) = (fin, inicio);
            return (inicio, fin, Formato.AUtc(inicio), Formato.AUtc(fin.AddDays(1)));
        }

        // Agrupa una lista por una clave (forma de pago, empleado, proveedor...) y suma los importes,
        // de mayor a menor. Sirve para todos los desgloses de los reportes.
        private static List<ResumenFila> Agrupar<T>(IEnumerable<T> items, Func<T, string> clave, Func<T, decimal> importe)
            => items
                .GroupBy(clave)
                .Select(g => new ResumenFila { Nombre = g.Key, Cantidad = g.Count(), Total = g.Sum(importe) })
                .OrderByDescending(f => f.Total)
                .ToList();

        // Igual que Agrupar pero por día (en hora local), en orden cronológico
        private static List<ResumenFila> AgruparPorDia<T>(IEnumerable<T> items, Func<T, DateTime> fecha, Func<T, decimal> importe)
            => items
                .GroupBy(i => Formato.ALocal(fecha(i)).Date)
                .OrderBy(g => g.Key)
                .Select(g => new ResumenFila { Nombre = g.Key.ToString("dd/MM/yyyy"), Cantidad = g.Count(), Total = g.Sum(importe) })
                .ToList();

        // Devuelve el reporte como archivo CSV para descargar (se abre en Excel)
        private FileContentResult ArchivoCsv(string[] encabezados, IEnumerable<object?[]> filas, string nombre)
            => File(Csv.Generar(encabezados, filas), "text/csv; charset=utf-8", $"{nombre}.csv");
    }
}
