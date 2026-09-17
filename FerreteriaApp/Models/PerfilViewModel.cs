using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Edición del perfil (Cliente o Empleado)
    public class PerfilViewModel
    {
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(100), Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio"), MaxLength(100), Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "El teléfono debe tener solo números (8 a 15 dígitos)")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria"), MaxLength(200)]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;

        // Solo lectura, para mostrar
        public string TipoUsuario { get; set; } = string.Empty;
        public string? Cargo { get; set; }
        public string? TipoCliente { get; set; }
    }
}
