using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    public class CheckoutViewModel
    {
        // Range(1, ...) porque un combo sin elegir llega como 0 y [Required] no lo detecta en un int
        [Range(1, int.MaxValue, ErrorMessage = "Elegí una forma de pago")]
        [Display(Name = "Forma de pago")]
        public int IdFormaPago { get; set; }

        [MaxLength(500)]
        [Display(Name = "Observaciones (opcional)")]
        public string? Observaciones { get; set; }

        // Solo para mostrar el resumen
        public List<CarritoItem> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Subtotal);
        public string DireccionEntrega { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
    }
}
