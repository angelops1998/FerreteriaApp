using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    public enum EstadoCompra
    {
        Pendiente = 0,
        Completada = 1,
        Cancelada = 2
    }

    // Compra de mercadería a un proveedor, registrada por un empleado.
    // Al completarse, el stock de los productos aumenta.
    public class Compra
    {
        [Key]
        public int IdCompra { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "El proveedor es obligatorio")]
        [Display(Name = "Proveedor")]
        public int IdProveedor { get; set; }
        public Proveedor? Proveedor { get; set; }

        [Required]
        [Display(Name = "Empleado")]
        public int IdEmpleado { get; set; }
        public Empleado? Empleado { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El total debe ser mayor a 0")]
        [Display(Name = "Total")]
        public decimal Total { get; set; }

        [Display(Name = "Estado")]
        public EstadoCompra Estado { get; set; } = EstadoCompra.Pendiente;

        [MaxLength(500)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();

        // ---- Métodos del diagrama ----

        public decimal CalcularTotal()
        {
            Total = Detalles.Sum(d => d.CalcularSubtotal());
            return Total;
        }

        // agregarDetalle(producto, cantidad): agrega una línea al precio de compra indicado
        public DetalleCompra AgregarDetalle(Producto producto, int cantidad, decimal precioUnitario)
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a 0");
            if (precioUnitario <= 0) throw new ArgumentException("El precio unitario debe ser mayor a 0");

            var detalle = new DetalleCompra
            {
                IdProducto = producto.IdProducto,
                Producto = producto,
                Cantidad = cantidad,
                PrecioUnitario = precioUnitario
            };
            detalle.CalcularSubtotal();
            Detalles.Add(detalle);
            return detalle;
        }

        public bool ValidarDatos() =>
            Fecha != default && Detalles.Count > 0 && Detalles.All(d => d.Cantidad > 0 && d.PrecioUnitario > 0) && CalcularTotal() > 0;
    }
}
