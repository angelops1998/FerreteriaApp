using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Data;
using FerreteriaApp.Helpers;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    // Panel de administración: resumen general (Admin y Empleado) y gestión de usuarios (solo Admin)
    [Authorize(Roles = "Admin,Empleado")]
    public class AdminController(ApplicationDbContext context, UserManager<Usuario> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalProductos = await context.Productos.CountAsync();
            ViewBag.TotalClientes = await context.Clientes.CountAsync();
            ViewBag.TotalEmpleados = await context.Empleados.CountAsync();
            ViewBag.TotalVentas = await context.Ventas.CountAsync();
            ViewBag.VentasPendientes = await context.Ventas.CountAsync(v => v.Estado == EstadoVenta.Pendiente);
            ViewBag.ComprasPendientes = await context.Compras.CountAsync(c => c.Estado == EstadoCompra.Pendiente);
            ViewBag.IngresosVentas = await context.Ventas
                .Where(v => v.Estado == EstadoVenta.Completada)
                .SumAsync(v => (decimal?)v.Total) ?? 0m;
            ViewBag.GastosCompras = await context.Compras
                .Where(c => c.Estado == EstadoCompra.Completada)
                .SumAsync(c => (decimal?)c.Total) ?? 0m;

            // Inventario.verificarStock(): productos en o por debajo del mínimo
            ViewBag.StockBajo = await context.Inventarios
                .AsNoTracking()
                .Include(i => i.Producto)
                .Where(i => i.Producto!.Estado && i.StockActual <= i.StockMinimo)
                .OrderBy(i => i.StockActual)
                .Take(10)
                .ToListAsync();

            ViewBag.UltimasVentas = await context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .OrderByDescending(v => v.Fecha)
                .Take(5)
                .ToListAsync();

            // Productos más vendidos (por cantidad) en ventas completadas o pendientes
            ViewBag.MasVendidos = await context.DetalleVentas
                .AsNoTracking()
                .Where(d => d.Venta!.Estado == EstadoVenta.Completada || d.Venta.Estado == EstadoVenta.Pendiente)
                .GroupBy(d => d.Producto!.Nombre)
                .Select(g => new { Nombre = g.Key, Cantidad = g.Sum(d => d.Cantidad), Total = g.Sum(d => d.Subtotal) })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ---------- USUARIOS (solo Admin) ----------

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await userManager.Users.AsNoTracking().OrderBy(u => u.Apellido).ThenBy(u => u.Nombre).ToListAsync();

            var roles = new Dictionary<int, IList<string>>();
            foreach (var u in usuarios)
                roles[u.Id] = await userManager.GetRolesAsync(u);

            ViewBag.Roles = roles;
            return View(usuarios);
        }

        // Activa o desactiva un usuario (Usuario.estado)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var usuario = await userManager.FindByIdAsync(id.ToString());
            if (usuario == null) return NotFound();

            if (usuario.Id == User.GetUsuarioId())
            {
                TempData["Error"] = "No podés desactivar tu propia cuenta.";
                return RedirectToAction(nameof(Usuarios));
            }

            usuario.Estado = !usuario.Estado;
            await userManager.UpdateAsync(usuario);

            TempData["Info"] = usuario.Estado ? $"{usuario.Email} activado." : $"{usuario.Email} desactivado.";
            return RedirectToAction(nameof(Usuarios));
        }

        // Cambia el rol de un empleado entre Empleado y Admin (los clientes siempre son Cliente)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(int id)
        {
            var usuario = await userManager.FindByIdAsync(id.ToString());
            if (usuario == null) return NotFound();

            if (usuario is not Empleado)
            {
                TempData["Error"] = "Solo los empleados pueden tener el rol de administrador.";
                return RedirectToAction(nameof(Usuarios));
            }

            if (usuario.Id == User.GetUsuarioId())
            {
                TempData["Error"] = "No podés cambiar tu propio rol.";
                return RedirectToAction(nameof(Usuarios));
            }

            var rolesActuales = await userManager.GetRolesAsync(usuario);
            await userManager.RemoveFromRolesAsync(usuario, rolesActuales);

            if (rolesActuales.Contains(DbInitializer.RolAdmin))
            {
                await userManager.AddToRoleAsync(usuario, DbInitializer.RolEmpleado);
                TempData["Info"] = $"{usuario.Email} ahora es Empleado.";
            }
            else
            {
                await userManager.AddToRoleAsync(usuario, DbInitializer.RolAdmin);
                TempData["Exito"] = $"{usuario.Email} ahora es Administrador.";
            }

            return RedirectToAction(nameof(Usuarios));
        }

        // Cambia el tipo de cliente (Regular <-> Mayorista)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarTipoCliente(int id)
        {
            var cliente = await context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            cliente.TipoCliente = cliente.TipoCliente == "Mayorista" ? "Regular" : "Mayorista";
            await context.SaveChangesAsync();

            TempData["Info"] = $"{cliente.Email} ahora es cliente {cliente.TipoCliente}.";
            return RedirectToAction(nameof(Usuarios));
        }

        // ---------- EMPLEADOS (solo Admin) ----------

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CrearEmpleado() => View(new EmpleadoViewModel());

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEmpleado(EmpleadoViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "La contraseña es obligatoria.");
            if (!ModelState.IsValid) return View(model);

            var empleado = new Empleado
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Cargo = model.Cargo,
                Telefono = model.Telefono,
                Direccion = model.Direccion,
                Estado = model.Estado
            };

            var result = await userManager.CreateAsync(empleado, model.Password!);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors.Where(e => e.Code != "DuplicateUserName"))
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            var rol = model.Rol == DbInitializer.RolAdmin ? DbInitializer.RolAdmin : DbInitializer.RolEmpleado;
            await userManager.AddToRoleAsync(empleado, rol);

            TempData["Exito"] = $"Empleado {empleado.NombreCompleto} creado con rol {rol}.";
            return RedirectToAction(nameof(Usuarios));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditarEmpleado(int id)
        {
            var empleado = await context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();

            var roles = await userManager.GetRolesAsync(empleado);
            return View(new EmpleadoViewModel
            {
                Id = empleado.Id, Nombre = empleado.Nombre, Apellido = empleado.Apellido, Email = empleado.Email ?? "",
                Cargo = empleado.Cargo, Telefono = empleado.Telefono, Direccion = empleado.Direccion,
                Estado = empleado.Estado, Rol = roles.Contains(DbInitializer.RolAdmin) ? DbInitializer.RolAdmin : DbInitializer.RolEmpleado
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEmpleado(int id, EmpleadoViewModel model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var empleado = await context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();

            empleado.Nombre = model.Nombre;
            empleado.Apellido = model.Apellido;
            empleado.Cargo = model.Cargo;
            empleado.Telefono = model.Telefono;
            empleado.Direccion = model.Direccion;
            if (empleado.Id != User.GetUsuarioId()) empleado.Estado = model.Estado;

            if (empleado.Email != model.Email)
            {
                await userManager.SetEmailAsync(empleado, model.Email);
                await userManager.SetUserNameAsync(empleado, model.Email);
            }

            var result = await userManager.UpdateAsync(empleado);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            // Contraseña nueva solo si se escribió algo
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(empleado);
                var pwd = await userManager.ResetPasswordAsync(empleado, token, model.Password);
                if (!pwd.Succeeded)
                {
                    foreach (var error in pwd.Errors) ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }
            }

            // Rol (no se puede cambiar el propio)
            if (empleado.Id != User.GetUsuarioId())
            {
                var rolesActuales = await userManager.GetRolesAsync(empleado);
                var rolNuevo = model.Rol == DbInitializer.RolAdmin ? DbInitializer.RolAdmin : DbInitializer.RolEmpleado;
                if (!rolesActuales.Contains(rolNuevo))
                {
                    await userManager.RemoveFromRolesAsync(empleado, rolesActuales);
                    await userManager.AddToRoleAsync(empleado, rolNuevo);
                }
            }

            TempData["Exito"] = $"Empleado {empleado.NombreCompleto} actualizado.";
            return RedirectToAction(nameof(Usuarios));
        }
    }
}
