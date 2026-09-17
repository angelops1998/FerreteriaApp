using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Empleado hereda de Usuario (tabla Empleados, clave = IdUsuario).
    // Puede registrar ventas, registrar compras y gestionar el inventario.
    public class Empleado : Usuario
    {
        [Required(ErrorMessage = "El cargo es obligatorio"), MaxLength(50)]
        [Display(Name = "Cargo")]
        public string Cargo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "El teléfono debe tener solo números (8 a 15 dígitos)")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria"), MaxLength(200)]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;
    }
}
