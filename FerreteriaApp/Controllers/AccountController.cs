using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FerreteriaApp.Data;
using FerreteriaApp.Models;

namespace FerreteriaApp.Controllers
{
    // Constructor primario: userManager y signInManager quedan disponibles
    // en toda la clase sin declarar campos ni constructor explícito.
    public class AccountController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager) : Controller
    {
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            // Usuario.login(email, password): un usuario desactivado (Estado = false) no puede entrar
            var usuario = await userManager.FindByEmailAsync(model.Email);
            if (usuario != null && !usuario.Estado)
            {
                ModelState.AddModelError(string.Empty, "La cuenta está desactivada. Contacte al administrador.");
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Si venía de una página protegida (ej. finalizar compra), vuelve a ella
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                // El personal va al panel; los clientes al catálogo
                if (usuario is Empleado)
                    return RedirectToAction("Index", "Admin");
                return RedirectToAction("Index", "Productos");
            }

            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        // El registro público crea un Cliente con el rol "Cliente"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistroViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var cliente = new Cliente
            {
                UserName = model.Email,
                Email = model.Email,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Telefono = model.Telefono,
                Direccion = model.Direccion,
                TipoCliente = "Regular",
                Estado = true
            };

            var result = await userManager.CreateAsync(cliente, model.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(cliente, DbInitializer.RolCliente);
                await signInManager.SignInAsync(cliente, isPersistent: false);
                TempData["Exito"] = $"¡Bienvenido/a, {cliente.NombreCompleto}! Tu cuenta fue creada.";
                return RedirectToAction("Index", "Productos");
            }

            // Como el usuario es el email, Identity repite el error de duplicado; se muestra uno solo
            foreach (var error in result.Errors.Where(e => e.Code != "DuplicateUserName"))
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ---------- PERFIL ----------

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            var usuario = await userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();
            return View(ArmarPerfil(usuario));
        }

        // Cliente.actualizarDatos() / datos del Empleado
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(PerfilViewModel model)
        {
            var usuario = await userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var vm = ArmarPerfil(usuario);
                vm.Nombre = model.Nombre; vm.Apellido = model.Apellido; vm.Telefono = model.Telefono; vm.Direccion = model.Direccion;
                return View(vm);
            }

            usuario.Nombre = model.Nombre;
            usuario.Apellido = model.Apellido;
            switch (usuario)
            {
                case Cliente c:
                    c.Telefono = model.Telefono;
                    c.Direccion = model.Direccion;
                    break;
                case Empleado e:
                    e.Telefono = model.Telefono;
                    e.Direccion = model.Direccion;
                    break;
            }
            await userManager.UpdateAsync(usuario);

            TempData["Exito"] = "Perfil actualizado correctamente.";
            return RedirectToAction(nameof(Perfil));
        }

        // Usuario.cambiarPassword(nueva)
        [Authorize]
        [HttpGet]
        public IActionResult CambiarPassword() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var usuario = await userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            var result = await userManager.ChangePasswordAsync(usuario, model.PasswordActual, model.PasswordNueva);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await signInManager.RefreshSignInAsync(usuario);
            TempData["Exito"] = "Contraseña cambiada correctamente.";
            return RedirectToAction(nameof(Perfil));
        }

        private static PerfilViewModel ArmarPerfil(Usuario usuario) => usuario switch
        {
            Cliente c => new PerfilViewModel
            {
                Email = c.Email ?? "", Nombre = c.Nombre, Apellido = c.Apellido,
                Telefono = c.Telefono, Direccion = c.Direccion,
                TipoUsuario = "Cliente", TipoCliente = c.TipoCliente
            },
            Empleado e => new PerfilViewModel
            {
                Email = e.Email ?? "", Nombre = e.Nombre, Apellido = e.Apellido,
                Telefono = e.Telefono, Direccion = e.Direccion,
                TipoUsuario = "Empleado", Cargo = e.Cargo
            },
            _ => new PerfilViewModel { Email = usuario.Email ?? "", Nombre = usuario.Nombre, Apellido = usuario.Apellido, TipoUsuario = "Usuario" }
        };
    }
}
