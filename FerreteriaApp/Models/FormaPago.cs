using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Catálogo de formas de pago: Efectivo, Tarjeta, Transferencia, QR...
    public class FormaPago
    {
        [Key]
        public int IdFormaPago { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(50)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Activa")]
        public bool Estado { get; set; } = true;

        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();

        public bool ValidarDatos() => !string.IsNullOrWhiteSpace(Nombre);
    }
}
