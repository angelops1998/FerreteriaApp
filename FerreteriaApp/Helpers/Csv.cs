using System.Globalization;
using System.Text;

namespace FerreteriaApp.Helpers
{
    // Arma un archivo CSV (se abre directo en Excel) a partir de encabezados y filas.
    // Usa punto y coma como separador y UTF-8 con BOM para que Excel muestre bien los acentos.
    public static class Csv
    {
        public static byte[] Generar(string[] encabezados, IEnumerable<object?[]> filas)
        {
            var sb = new StringBuilder();
            sb.AppendLine("sep=;"); // le indica a Excel el separador, sin importar el idioma de Windows
            sb.AppendLine(string.Join(";", encabezados.Select(Escapar)));

            foreach (var fila in filas)
                sb.AppendLine(string.Join(";", fila.Select(Escapar)));

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        // Convierte cada valor a texto: los decimales con dos decimales y punto, las fechas en formato local
        private static string Escapar(object? valor)
        {
            var texto = valor switch
            {
                null => "",
                decimal d => d.ToString("F2", CultureInfo.InvariantCulture),
                DateTime f => Formato.Fecha(f),
                bool b => b ? "Sí" : "No",
                _ => valor.ToString() ?? ""
            };

            if (texto.Contains(';') || texto.Contains('"') || texto.Contains('\n'))
                texto = "\"" + texto.Replace("\"", "\"\"") + "\"";

            return texto;
        }
    }
}
