using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    public class CategoriaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de categoría es obligatorio")]
        [Display(Name = "Tipo de Categoría")]
        public string Tipo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Nombre de la Categoría")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El orden es obligatorio")]
        [Range(1, 999, ErrorMessage = "El orden debe estar entre 1 y 999")]
        [Display(Name = "Orden de Visualización")]
        public int Orden { get; set; }

        [Display(Name = "Estado")]
        public bool Activo { get; set; } = true;

        // Propiedades calculadas para la vista
        public int CantidadProductos { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? CreadoPor { get; set; }
    }
}