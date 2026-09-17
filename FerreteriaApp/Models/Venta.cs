using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    public enum EstadoVenta
    {
        Pendiente = 0,
        Completada = 1,
        Cancelada = 2,
        Devuelta = 3
    }

    // Una venta de la ferretería a un cliente. Puede originarse en la web (el cliente compra
    // desde el catálogo) o registrarla un empleado en el mostrador.
    public class Venta
    {
        [Key]
        public int IdVenta { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "El cliente es obligatorio")]
        [Display(Name = "Cliente")]
        public int IdCliente { get; set; }
        public Cliente? Cliente { get; set; }

        // Empleado que registró o completó la venta. Es null mientras una venta web está pendiente.
        [Display(Name = "Empleado")]
        public int? IdEmpleado { get; set; }
        public Empleado? Empleado { get; set; }

        [Required(ErrorMessage = "La forma de pago es obligatoria")]
        [Display(Name = "Forma de pago")]
        public int IdFormaPago { get; set; }
        public FormaPago? FormaPago { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El total debe ser mayor a 0")]
        [Display(Name = "Total")]
        public decimal Total { get; set; }

        [Display(Name = "Estado")]
        public EstadoVenta Estado { get; set; } = EstadoVenta.Pendiente;

        [MaxLength(500)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();

        // ---- Métodos del diagrama ----

        public decimal CalcularTotal()
        {
            Total = Detalles.Sum(d => d.CalcularSubtotal());
            return Total;
        }

        // agregarDetalle(producto, cantidad): agrega una línea con el precio de venta actual del producto
        public DetalleVenta AgregarDetalle(Producto producto, int cantidad)
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a 0");
            if (producto.Stock < cantidad)
                throw new InvalidOperationException($"\"{producto.Nombre}\": stock insuficiente (disponible: {producto.Stock}, pedido: {cantidad}).");

            var detalle = new DetalleVenta
            {
                IdProducto = producto.IdProducto,
                Producto = producto,
                Cantidad = cantidad,
                PrecioUnitario = producto.PrecioVenta
            };
            detalle.CalcularSubtotal();
            Detalles.Add(detalle);
            return detalle;
        }

        public bool ValidarDatos() =>
            Fecha != default && Detalles.Count > 0 && Detalles.All(d => d.Cantidad > 0 && d.PrecioUnitario > 0) && CalcularTotal() > 0;

        // cerrarVenta(): la marca como completada y registra qué empleado la cerró
        public void CerrarVenta(int idEmpleado)
        {
            Estado = EstadoVenta.Completada;
            IdEmpleado = idEmpleado;
        }

        // En estos estados el stock está descontado; en Cancelada/Devuelta se devolvió
        public bool DescuentaStock() => Estado == EstadoVenta.Pendiente || Estado == EstadoVenta.Completada;
    }
}
