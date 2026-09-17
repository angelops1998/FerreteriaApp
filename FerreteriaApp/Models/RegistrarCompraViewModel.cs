using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Compra a un proveedor registrada por un empleado (Empleado.registrarCompra)
    public class RegistrarCompraViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Elegí el proveedor")]
        [Display(Name = "Proveedor")]
        public int IdProveedor { get; set; }

        [MaxLength(500)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        public List<LineaCompraInput> Items { get; set; } = new();
    }

    public class LineaCompraInput
    {
        public int IdProducto { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; } = 1;

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio unitario debe ser mayor a 0")]
        public decimal PrecioUnitario { get; set; }
    }
}
