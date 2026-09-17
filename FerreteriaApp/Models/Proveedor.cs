using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    public class Proveedor
    {
        [Key]
        public int IdProveedor { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(150)]
        [Display(Name = "Nombre / Razón social")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUC/NIT es obligatorio"), MaxLength(20)]
        [Display(Name = "RUC / NIT")]
        public string Ruc { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "El teléfono debe tener solo números (8 a 15 dígitos)")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria"), MaxLength(200)]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool Estado { get; set; } = true;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
        public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    }
}
