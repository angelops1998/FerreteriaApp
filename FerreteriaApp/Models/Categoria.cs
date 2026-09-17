using System.ComponentModel.DataAnnotations;

namespace FerreteriaApp.Models
{
    public class Categoria
    {
        [Key]
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(300)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Activa")]
        public bool Estado { get; set; } = true;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
