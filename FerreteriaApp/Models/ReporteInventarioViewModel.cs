using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Reporte de inventario valorizado: cuánto stock hay y cuánto vale (a precio de compra y de venta)
    public class ReporteInventarioViewModel
    {
        [Display(Name = "Categoría")]
        public int? IdCategoria { get; set; }

        [Display(Name = "Solo productos bajo el mínimo")]
        public bool SoloBajos { get; set; }

        [Display(Name = "Incluir productos inactivos")]
        public bool IncluirInactivos { get; set; }

        public List<Producto> Productos { get; set; } = new();

        // Valor del stock por categoría (Cantidad = unidades, Total = valor a precio de compra)
        public List<ResumenFila> PorCategoria { get; set; } = new();

        public int CantidadProductos => Productos.Count;
        public int ProductosBajoMinimo => Productos.Count(p => !p.VerificarStock());
        public int ProductosSinStock => Productos.Count(p => p.Stock == 0);
        public int UnidadesTotales => Productos.Sum(p => p.Stock);
        public decimal ValorCosto => Productos.Sum(p => p.Stock * p.PrecioCompra);
        public decimal ValorVenta => Productos.Sum(p => p.Stock * p.PrecioVenta);
        public decimal GananciaPotencial => ValorVenta - ValorCosto;
    }
}
