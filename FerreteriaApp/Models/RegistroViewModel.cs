using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Formulario de registro público: crea un Cliente
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(100), Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio"), MaxLength(100), Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio"), EmailAddress(ErrorMessage = "Email inválido"), Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Password mínimo 8 caracteres, con mayúsculas, minúsculas y números (las reglas exactas
        // las aplica Identity según Program.cs; acá se valida el largo en el navegador)
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "El teléfono debe tener solo números (8 a 15 dígitos)")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria"), MaxLength(200)]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;
    }
}
