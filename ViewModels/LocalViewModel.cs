using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    public class LocalViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del local es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Nombre del Local")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Distrito")]
        public string? Distrito { get; set; }

        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Ciudad")]
        public string? Ciudad { get; set; }

        [StringLength(20, ErrorMessage = "Máximo 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Display(Name = "Estado")]
        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; }

        // Propiedades adicionales para la vista
        public int CantidadUsuarios { get; set; }
        public int CantidadProductos { get; set; }
        public decimal ValorInventario { get; set; }
    }
}