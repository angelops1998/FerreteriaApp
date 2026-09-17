using System.Security.Claims;

namespace FerreteriaApp.Helpers
{
    public static class UsuarioExtensions
    {
        // Id numérico del usuario logueado (Identity lo guarda como texto en el claim NameIdentifier)
        public static int GetUsuarioId(this ClaimsPrincipal user)
        {
            var valor = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(valor, out var id) ? id : 0;
        }

        public static bool EsPersonal(this ClaimsPrincipal user)
            => user.IsInRole("Admin") || user.IsInRole("Empleado");
    }
}
