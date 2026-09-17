using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FerreteriaApp.Models
{
    // Rol del diagrama. Hereda de IdentityRole<int>: "Name" es el nombre del rol (columna "Nombre").
    // Roles del sistema: Admin, Empleado y Cliente.
    public class Rol : IdentityRole<int>
    {
        public Rol() { }
        public Rol(string nombre, string? descripcion = null) : base(nombre) { Descripcion = descripcion; }

        [MaxLength(200)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }
    }
}
