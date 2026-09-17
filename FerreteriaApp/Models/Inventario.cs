using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Registro de inventario de un producto: stock actual, mínimo y última actualización.
    // Se crea uno por producto y se actualiza en cada venta, compra o ajuste manual.
    public class Inventario
    {
        [Key]
        public int IdInventario { get; set; }

        [Required]
        [Display(Name = "Producto")]
        public int IdProducto { get; set; }
        public Producto? Producto { get; set; }

        [Required, Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        [Display(Name = "Stock actual")]
        public int StockActual { get; set; }

        [Required, Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        [Display(Name = "Stock mínimo")]
        public int StockMinimo { get; set; }

        [Display(Name = "Última actualización")]
        public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;

        // ---- Métodos del diagrama ----

        public void ActualizarStock(int cantidad)
        {
            if (StockActual + cantidad < 0)
                throw new InvalidOperationException("El stock del inventario no puede quedar negativo.");

            StockActual += cantidad;
            UltimaActualizacion = DateTime.UtcNow;
        }

        // true si el stock está por encima del mínimo
        public bool VerificarStock() => StockActual > StockMinimo;
    }
}
