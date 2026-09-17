using System.Text.Json;
using FerreteriaApp.Models;

namespace FerreteriaApp.Helpers
{
    // El carrito vive en la sesión del servidor (cookie de sesión + JSON en memoria).
    // No es una tabla: al confirmar la compra se convierte en una Venta.
    public static class CarritoSesion
    {
        private const string Clave = "Carrito";

        public static List<CarritoItem> Obtener(ISession session)
        {
            var json = session.GetString(Clave);
            return string.IsNullOrEmpty(json)
                ? new List<CarritoItem>()
                : JsonSerializer.Deserialize<List<CarritoItem>>(json) ?? new List<CarritoItem>();
        }

        public static void Guardar(ISession session, List<CarritoItem> items)
            => session.SetString(Clave, JsonSerializer.Serialize(items));

        public static void Vaciar(ISession session) => session.Remove(Clave);

        public static int Cantidad(ISession session) => Obtener(session).Sum(i => i.Cantidad);
    }
}
