using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    // Cliente hereda de Usuario (tabla Clientes, clave = IdUsuario).
    // Es quien se registra desde la web y realiza compras (ventas para la ferretería).
    public class Cliente : Usuario
    {
        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "El teléfono debe tener solo números (8 a 15 dígitos)")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria"), MaxLength(200)]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;

        // Regular o Mayorista (lo cambia el administrador)
        [MaxLength(30)]
        [Display(Name = "Tipo de cliente")]
        public string TipoCliente { get; set; } = "Regular";

        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }
}
