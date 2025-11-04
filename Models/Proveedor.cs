using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para representar un proveedor de insumos y materiales
    /// </summary>
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// RUC del proveedor (11 dígitos numéricos)
        /// </summary>
        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "El RUC debe tener exactamente 11 dígitos")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe contener solo números")]
        public string RUC { get; set; } = string.Empty;

        /// <summary>
        /// Nombre o razón social del proveedor
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Categoría del proveedor - REFERENCIA a categoría existente en el sistema
        /// Puede ser de cualquier tipo: Bebidas, Cocina o Utensilios
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Categoria { get; set; } = string.Empty;

        /// <summary>
        /// Teléfono de contacto del proveedor
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        /// <summary>
        /// Email del proveedor (opcional)
        /// </summary>
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// Persona de contacto del proveedor
        /// </summary>
        [StringLength(100)]
        public string? PersonaContacto { get; set; }

        /// <summary>
        /// Dirección física del proveedor (opcional)
        /// </summary>
        [StringLength(300)]
        public string? Direccion { get; set; }

        /// <summary>
        /// Estado del proveedor (Activo/Inactivo)
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última modificación
        /// </summary>
        public DateTime? FechaModificacion { get; set; }

        /// <summary>
        /// Usuario que creó el registro
        /// </summary>
        [StringLength(100)]
        public string? CreadoPor { get; set; }

        /// <summary>
        /// Usuario que modificó el registro por última vez
        /// </summary>
        [StringLength(100)]
        public string? ModificadoPor { get; set; }
    }
}