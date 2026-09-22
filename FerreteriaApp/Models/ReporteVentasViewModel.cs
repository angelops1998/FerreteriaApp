using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Reporte de ventas en un rango de fechas, con filtros y desgloses
    public class ReporteVentasViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "Desde")]
        public DateTime Desde { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Hasta")]
        public DateTime Hasta { get; set; }

        [Display(Name = "Estado")]
        public EstadoVenta? Estado { get; set; }

        [Display(Name = "Forma de pago")]
        public int? IdFormaPago { get; set; }

        public List<Venta> Ventas { get; set; } = new();

        // Desgloses (solo sobre ventas completadas, que son las que representan dinero cobrado)
        public List<ResumenFila> PorEstado { get; set; } = new();
        public List<ResumenFila> PorFormaPago { get; set; } = new();
        public List<ResumenFila> PorEmpleado { get; set; } = new();
        public List<ResumenFila> PorDia { get; set; } = new();

        public int CantidadVentas => Ventas.Count;
        public int CantidadCompletadas => Ventas.Count(v => v.Estado == EstadoVenta.Completada);
        public decimal TotalCompletadas => Ventas.Where(v => v.Estado == EstadoVenta.Completada).Sum(v => v.Total);
        public decimal TotalPendientes => Ventas.Where(v => v.Estado == EstadoVenta.Pendiente).Sum(v => v.Total);
        public int UnidadesVendidas => Ventas.Where(v => v.Estado == EstadoVenta.Completada).Sum(v => v.Detalles.Sum(d => d.Cantidad));
        public decimal TicketPromedio => CantidadCompletadas == 0 ? 0 : TotalCompletadas / CantidadCompletadas;
    }
}
