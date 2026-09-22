using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Una línea del reporte de productos vendidos
    public class FilaProductoVendido
    {
        public Producto Producto { get; set; } = null!;
        public int Unidades { get; set; }
        public int CantidadVentas { get; set; }
        public decimal Ingresos { get; set; }

        // Costo estimado con el precio de compra actual del producto
        public decimal Costo { get; set; }
        public decimal Ganancia => Ingresos - Costo;
        public decimal Margen => Ingresos == 0 ? 0 : Ganancia / Ingresos * 100;
    }

    // Reporte de productos: los más vendidos, su rentabilidad y los que no se vendieron
    public class ReporteProductosViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "Desde")]
        public DateTime Desde { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Hasta")]
        public DateTime Hasta { get; set; }

        [Display(Name = "Categoría")]
        public int? IdCategoria { get; set; }

        [Display(Name = "Ordenar por")]
        public string Orden { get; set; } = "unidades"; // unidades | ingresos | ganancia

        public List<FilaProductoVendido> Vendidos { get; set; } = new();
        public List<Producto> SinVentas { get; set; } = new();

        public int UnidadesTotales => Vendidos.Sum(v => v.Unidades);
        public decimal IngresosTotales => Vendidos.Sum(v => v.Ingresos);
        public decimal CostoTotal => Vendidos.Sum(v => v.Costo);
        public decimal GananciaTotal => IngresosTotales - CostoTotal;
        public decimal MargenPromedio => IngresosTotales == 0 ? 0 : GananciaTotal / IngresosTotales * 100;
    }
}
