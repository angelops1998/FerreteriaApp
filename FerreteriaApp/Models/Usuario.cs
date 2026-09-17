using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace FerreteriaApp.Models
{
    // Clase base del diagrama: Usuario. Hereda de IdentityUser<int> para que Identity maneje
    // el email, la contraseña (hasheada) y los roles, con claves numéricas (idUsuario: int).
    // Empleado y Cliente heredan de esta clase (herencia TPT: una tabla por tipo).
    public class Usuario : IdentityUser<int>
    {
        [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio"), MaxLength(100)]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        // Estado no puede ser nulo: true = activo, false = desactivado (no puede iniciar sesión)
        [Display(Name = "Activo")]
        public bool Estado { get; set; } = true;

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();

        // validarDatos(): las reglas están en las Data Annotations y en las opciones de Identity
        public bool ValidarDatos() =>
            !string.IsNullOrWhiteSpace(Nombre) && !string.IsNullOrWhiteSpace(Apellido) && !string.IsNullOrWhiteSpace(Email);
    }
}
