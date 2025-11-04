using System.ComponentModel.DataAnnotations;
using Puerto92.Models;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para operaciones con proveedores
    /// </summary>
    public class ProveedorViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El RUC es obligatorio")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "El RUC debe tener exactamente 11 dígitos")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe contener solo números")]
        [Display(Name = "RUC")]
        public string RUC { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre o razón social es obligatorio")]
        [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
        [Display(Name = "Nombre o Razón Social")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar una categoría")]
        [Display(Name = "Categoría")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email (opcional)")]
        public string? Email { get; set; }

        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Persona de Contacto (opcional)")]
        public string? PersonaContacto { get; set; }

        [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
        [Display(Name = "Dirección (opcional)")]
        public string? Direccion { get; set; }

        [Display(Name = "Estado")]
        public bool Activo { get; set; } = true;

        // Propiedades adicionales para la vista
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string? CreadoPor { get; set; }
        public string? ModificadoPor { get; set; }
    }

    /// <summary>
    /// ViewModel simplificado para autocompletado de proveedores en pedidos
    /// </summary>
    public class ProveedorAutocompletarViewModel
    {
        public int Id { get; set; }
        public string RUC { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? PersonaContacto { get; set; }
    }
}