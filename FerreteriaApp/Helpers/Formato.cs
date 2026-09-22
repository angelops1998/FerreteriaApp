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

        // Zona horaria de la tienda. Si el servidor no la conoce, se usa UTC.
        public static TimeZoneInfo Zona
        {
            get
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(ZonaHoraria); }
                catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
                catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
            }
        }

        // Las fechas se guardan en UTC en la base de datos; estas dos funciones pasan
        // de UTC a la hora local de la tienda y al revés (para filtrar por fecha en los reportes).
        public static DateTime ALocal(DateTime utc)
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

        public static DateTime AUtc(DateTime local)
            => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zona);

        public static string Fecha(DateTime utc) => ALocal(utc).ToString("dd/MM/yyyy HH:mm");

        public static string FechaCorta(DateTime utc) => ALocal(utc).ToString("dd/MM/yyyy");

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
