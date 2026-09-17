using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    public class DetalleCompra
    {
        [Key]
        public int IdDetalleCompra { get; set; }

        public int IdCompra { get; set; }
        public Compra? Compra { get; set; }

        public int IdProducto { get; set; }
        public Producto? Producto { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }

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
