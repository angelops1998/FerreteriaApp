using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    public class DetalleVenta
    {
        [Key]
        public int IdDetalleVenta { get; set; }

        public int IdVenta { get; set; }
        public Venta? Venta { get; set; }

        public int IdProducto { get; set; }
        public Producto? Producto { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }

        // Precio al momento de la venta (si el producto cambia de precio, el historial no cambia)
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio unitario debe ser mayor a 0")]
        [Display(Name = "Precio unitario")]
        public decimal PrecioUnitario { get; set; }

        [Display(Name = "Subtotal")]
        public decimal Subtotal { get; set; }

        public decimal CalcularSubtotal()
        {
            Subtotal = PrecioUnitario * Cantidad;
            return Subtotal;
        }
    }
}
