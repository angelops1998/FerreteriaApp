using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerreteriaApp.Models
{
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria"), MaxLength(1000)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Required, Range(0.01, 999999.99, ErrorMessage = "El precio de venta debe ser mayor a 0")]
        [Display(Name = "Precio de venta")]
        public decimal PrecioVenta { get; set; }

        [Required, Range(0.01, 999999.99, ErrorMessage = "El precio de compra debe ser mayor a 0")]
        [Display(Name = "Precio de compra")]
        public decimal PrecioCompra { get; set; }

        [Required, Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        [Display(Name = "Stock")]
        public int Stock { get; set; }

        [Required, Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        [Display(Name = "Stock mínimo")]
        public int StockMinimo { get; set; }

        [Display(Name = "Activo (visible en el catálogo)")]
        public bool Estado { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "La categoría es obligatoria")]
        [Display(Name = "Categoría")]
        public int IdCategoria { get; set; }
        public Categoria? Categoria { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El proveedor es obligatorio")]
        [Display(Name = "Proveedor")]
        public int IdProveedor { get; set; }
        public Proveedor? Proveedor { get; set; }

        // Campo extra (no está en el diagrama): ruta de la imagen del producto
        [Display(Name = "URL de imagen")]
        public string? ImagenUrl { get; set; }

        public ICollection<Inventario> Inventarios { get; set; } = new List<Inventario>();

        // Solo para subir la imagen desde el formulario; no se guarda en la base de datos
        [NotMapped]
        [Display(Name = "Imagen")]
        public IFormFile? ImagenArchivo { get; set; }

        // ---- Métodos del diagrama ----

        // actualizarStock(cantidad): suma (o resta si es negativo) unidades al stock
        // y deja sincronizado el registro de Inventario.
        public void ActualizarStock(int cantidad)
        {
            if (Stock + cantidad < 0)
                throw new InvalidOperationException($"El stock de \"{Nombre}\" no puede quedar negativo.");

            Stock += cantidad;
            foreach (var inv in Inventarios)
                inv.ActualizarStock(cantidad);
        }

        // verificarStock(): true si hay stock por encima del mínimo
        public bool VerificarStock() => Stock > StockMinimo;

        public bool ValidarDatos() =>
            !string.IsNullOrWhiteSpace(Nombre) && PrecioVenta > 0 && PrecioCompra > 0 &&
            Stock >= 0 && StockMinimo >= 0 && IdCategoria > 0 && IdProveedor > 0;
    }
}
