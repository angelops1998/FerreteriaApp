using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Reporte de compras a proveedores en un rango de fechas
    public class ReporteComprasViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "Desde")]
        public DateTime Desde { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Hasta")]
        public DateTime Hasta { get; set; }

        [Display(Name = "Estado")]
        public EstadoCompra? Estado { get; set; }

        [Display(Name = "Proveedor")]
        public int? IdProveedor { get; set; }

        public List<Compra> Compras { get; set; } = new();

        // Desgloses (solo sobre compras completadas: mercadería ya recibida y pagada)
        public List<ResumenFila> PorEstado { get; set; } = new();
        public List<ResumenFila> PorProveedor { get; set; } = new();
        public List<ResumenFila> PorEmpleado { get; set; } = new();
        public List<ResumenFila> PorDia { get; set; } = new();

        public int CantidadCompras => Compras.Count;
        public int CantidadCompletadas => Compras.Count(c => c.Estado == EstadoCompra.Completada);
        public decimal TotalCompletadas => Compras.Where(c => c.Estado == EstadoCompra.Completada).Sum(c => c.Total);
        public decimal TotalPendientes => Compras.Where(c => c.Estado == EstadoCompra.Pendiente).Sum(c => c.Total);
        public int UnidadesCompradas => Compras.Where(c => c.Estado == EstadoCompra.Completada).Sum(c => c.Detalles.Sum(d => d.Cantidad));
    }
}
