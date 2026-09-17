using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Venta registrada por un empleado en el mostrador (Empleado.registrarVenta)
    public class RegistrarVentaViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Elegí el cliente")]
        [Display(Name = "Cliente")]
        public int IdCliente { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Elegí la forma de pago")]
        [Display(Name = "Forma de pago")]
        public int IdFormaPago { get; set; }

        [MaxLength(500)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        public List<LineaVentaInput> Items { get; set; } = new();
    }

    public class LineaVentaInput
    {
        public int IdProducto { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; } = 1;
    }
}
