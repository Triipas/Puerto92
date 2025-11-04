using System.ComponentModel.DataAnnotations;
using Puerto92.Models;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para operaciones con productos
    /// </summary>
    public class ProductoViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Código")]
        [StringLength(20)]
        public string? Codigo { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Nombre del Producto")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar una categoría")]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        [Display(Name = "Nombre de Categoría")]
        public string? CategoriaNombre { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una unidad")]
        [Display(Name = "Unidad de Medida")]
        public string Unidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio de compra es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio de compra debe ser mayor a 0")]
        [Display(Name = "Precio Compra (S/)")]
        public decimal PrecioCompra { get; set; }

        [Required(ErrorMessage = "El precio de venta es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio de venta debe ser mayor a 0")]
        [Display(Name = "Precio Venta (S/)")]
        public decimal PrecioVenta { get; set; }

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
    /// ViewModel para carga masiva de productos
    /// </summary>
    public class CargaMasivaProductosViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un archivo")]
        [Display(Name = "Archivo CSV")]
        public IFormFile Archivo { get; set; } = null!;
    }

    /// <summary>
    /// Resultado de la validación de carga masiva
    /// </summary>
    public class CargaMasivaProductosResultado
    {
        public bool Exitoso { get; set; }
        public int ProductosCargados { get; set; }
        public int FilasConError { get; set; }
        public List<string> Errores { get; set; } = new();
        public string Mensaje { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para importar productos desde CSV
    /// </summary>
    public class ProductoImportDto
    {
        public int NumeroFila { get; set; }
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public string PrecioCompraStr { get; set; } = string.Empty;
        public decimal PrecioCompra { get; set; }
        public string PrecioVentaStr { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public string? Descripcion { get; set; }
        public List<string> Errores { get; set; } = new();
        public bool EsValido => !Errores.Any();
    }
}