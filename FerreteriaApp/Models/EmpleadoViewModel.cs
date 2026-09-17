using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Alta y edición de empleados desde el panel de administración
    public class EmpleadoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(100), Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio"), MaxLength(100), Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio"), EmailAddress(ErrorMessage = "Email inválido"), Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Solo obligatoria al crear; al editar se deja vacía para no cambiarla
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "El cargo es obligatorio"), MaxLength(50), Display(Name = "Cargo")]
        public string Cargo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "El teléfono debe tener solo números (8 a 15 dígitos)")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria"), MaxLength(200), Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;

        [Required, Display(Name = "Rol")]
        public string Rol { get; set; } = "Empleado";

        [Display(Name = "Activo")]
        public bool Estado { get; set; } = true;
    }
}
