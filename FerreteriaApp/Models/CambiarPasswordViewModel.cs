using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Usuario.cambiarPassword(nueva)
    public class CambiarPasswordViewModel
    {
        [Required(ErrorMessage = "Ingresá tu contraseña actual"), DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string PasswordActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá la nueva contraseña")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string PasswordNueva { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("PasswordNueva", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
