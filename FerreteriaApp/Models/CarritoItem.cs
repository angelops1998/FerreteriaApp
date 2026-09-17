namespace FerreteriaApp.Models
{
    // Ítem del carrito de compras. El carrito NO es una tabla (no está en el diagrama):
    // se guarda en la sesión del servidor como JSON hasta que el cliente confirma la compra,
    // momento en el que se convierte en una Venta con sus DetalleVenta.
    public class CarritoItem
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public string? ImagenUrl { get; set; }

        public decimal Subtotal => PrecioUnitario * Cantidad;
    }
}
