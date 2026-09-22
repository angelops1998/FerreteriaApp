namespace FerreteriaApp.Models
{
    // Una línea de un desglose de reporte: por ejemplo "Efectivo — 12 ventas — Bs 1.540,00"
    public class ResumenFila
    {
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }
}
