using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Una línea del reporte de usuarios. Para los clientes se agregan sus compras del período.
    public class FilaUsuario
    {
        public Usuario Usuario { get; set; } = null!;
        public string Rol { get; set; } = string.Empty;

        // Cargo si es empleado, tipo (Regular/Mayorista) si es cliente
        public string? Detalle { get; set; }
        public string? Telefono { get; set; }

        public int CantidadCompras { get; set; }
        public decimal TotalComprado { get; set; }
        public DateTime? UltimaCompra { get; set; }

        public bool EsCliente => Usuario is Cliente;
        public decimal Promedio => CantidadCompras == 0 ? 0 : TotalComprado / CantidadCompras;
    }

    // Reporte de usuarios: administradores, empleados y clientes. Las fechas definen el período
    // que se analiza: altas nuevas y compras hechas por los clientes dentro de ese rango.
    public class ReporteUsuariosViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "Desde")]
        public DateTime Desde { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Hasta")]
        public DateTime Hasta { get; set; }

        [Display(Name = "Rol")]
        public string? Rol { get; set; }

        [Display(Name = "Estado")]
        public bool? Activo { get; set; }

        [Display(Name = "Solo dados de alta en el período")]
        public bool SoloNuevos { get; set; }

        public List<FilaUsuario> Usuarios { get; set; } = new();

        // Desgloses: cuántos usuarios hay por rol y cuántas altas hubo cada mes del período
        public List<ResumenFila> PorRol { get; set; } = new();
        public List<ResumenFila> AltasPorMes { get; set; } = new();

        public int Total => Usuarios.Count;
        public int Activos => Usuarios.Count(u => u.Usuario.Estado);
        public int Inactivos => Usuarios.Count(u => !u.Usuario.Estado);
        public int Nuevos { get; set; }
        public int ClientesQueCompraron => Usuarios.Count(u => u.CantidadCompras > 0);
        public decimal TotalComprado => Usuarios.Sum(u => u.TotalComprado);

        // Ranking de los clientes que más compraron (para el gráfico y el podio)
        public List<FilaUsuario> MejoresClientes => Usuarios
            .Where(u => u.CantidadCompras > 0)
            .OrderByDescending(u => u.TotalComprado)
            .ToList();
    }
}
