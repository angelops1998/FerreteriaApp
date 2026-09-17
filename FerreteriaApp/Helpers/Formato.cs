using System.Globalization;
using FerreteriaApp.Models;

namespace FerreteriaApp.Helpers
{
    // Formato de moneda y fecha en un solo lugar.
    // Los valores se cargan desde appsettings.json ("Tienda") en Program.cs.
    public static class Formato
    {
        public static string Moneda { get; set; } = "Bs";
        public static string ZonaHoraria { get; set; } = "America/La_Paz";

        public static string Precio(decimal valor)
            => $"{Moneda} {valor.ToString("N2", CultureInfo.InvariantCulture)}";

        public static string Fecha(DateTime utc)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(ZonaHoraria);
                var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
                return local.ToString("dd/MM/yyyy HH:mm");
            }
            catch (TimeZoneNotFoundException)
            {
                return utc.ToString("dd/MM/yyyy HH:mm");
            }
        }

        // Color de la etiqueta según el estado de la venta
        public static string EstadoBadge(EstadoVenta estado) => estado switch
        {
            EstadoVenta.Pendiente => "bg-warning text-dark",
            EstadoVenta.Completada => "bg-success",
            EstadoVenta.Cancelada => "bg-danger",
            EstadoVenta.Devuelta => "bg-secondary",
            _ => "bg-secondary"
        };

        // Color de la etiqueta según el estado de la compra
        public static string EstadoBadge(EstadoCompra estado) => estado switch
        {
            EstadoCompra.Pendiente => "bg-warning text-dark",
            EstadoCompra.Completada => "bg-success",
            EstadoCompra.Cancelada => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
