using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Una fila del resumen mensual: lo vendido y lo comprado en un mes
    public class FilaMes
    {
        public int Mes { get; set; }
        public string Nombre => ReporteFinancieroViewModel.NombresMeses[Mes - 1];
        public int CantidadVentas { get; set; }
        public decimal Ventas { get; set; }
        public int CantidadCompras { get; set; }
        public decimal Compras { get; set; }
        public decimal Diferencia => Ventas - Compras;
    }

    // Resumen financiero de un año: ventas cobradas vs. compras pagadas, mes a mes
    public class ReporteFinancieroViewModel
    {
        public static readonly string[] NombresMeses =
        {
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

        [Display(Name = "Año")]
        public int Anio { get; set; }

        // Años en los que hubo movimientos, para el combo
        public List<int> Anios { get; set; } = new();

        public List<FilaMes> Meses { get; set; } = new();

        public decimal TotalVentas => Meses.Sum(m => m.Ventas);
        public decimal TotalCompras => Meses.Sum(m => m.Compras);
        public decimal Diferencia => TotalVentas - TotalCompras;
        public int CantidadVentas => Meses.Sum(m => m.CantidadVentas);
        public int CantidadCompras => Meses.Sum(m => m.CantidadCompras);
    }
}
