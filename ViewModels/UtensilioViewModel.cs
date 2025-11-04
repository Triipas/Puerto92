using System.ComponentModel.DataAnnotations;
using Puerto92.Models;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para operaciones con utensilios
    /// </summary>
    public class UtensilioViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Código")]
        [StringLength(20)]
        public string? Codigo { get; set; }

        [Required(ErrorMessage = "El nombre del utensilio es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Nombre del Utensilio")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un tipo")]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar una unidad")]
        [Display(Name = "Unidad de Medida")]
        public string Unidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        [Display(Name = "Precio (S/)")]
        public decimal Precio { get; set; }

        [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
        [Display(Name = "Descripción (opcional)")]
        public string? Descripcion { get; set; }

        [Display(Name = "Estado")]
        public bool Activo { get; set; } = true;

        // Propiedades adicionales para la vista
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string? CreadoPor { get; set; }
        public string? ModificadoPor { get; set; }
    }

    /// <summary>
    /// ViewModel para carga masiva de utensilios
    /// </summary>
    public class CargaMasivaUtensiliosViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un archivo")]
        [Display(Name = "Archivo CSV")]
        public IFormFile Archivo { get; set; } = null!;
    }

    /// <summary>
    /// Resultado de la validación de carga masiva
    /// </summary>
    public class CargaMasivaResultado
    {
        public bool Exitoso { get; set; }
        public int UtensiliosCargados { get; set; }
        public int FilasConError { get; set; }
        public List<string> Errores { get; set; } = new();
        public string Mensaje { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para importar utensilios desde CSV
    /// </summary>
    public class UtensilioImportDto
    {
        public int NumeroFila { get; set; }
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public string PrecioStr { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string? Descripcion { get; set; }
        public List<string> Errores { get; set; } = new();
        public bool EsValido => !Errores.Any();
    }
}