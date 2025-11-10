using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para representar un utensilio del catálogo base
    /// </summary>
    public class Utensilio
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Código único del utensilio (autogenerado si no se proporciona)
        /// Formato: UTEN-001, UTEN-002, etc.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del utensilio
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Relación con Categoría (de tipo "Utensilios")
        /// </summary>
        [Required]
        public int CategoriaId { get; set; }

        /// <summary>
        /// Navegación a la categoría asociada
        /// </summary>
        [ForeignKey(nameof(CategoriaId))]
        public virtual Categoria? Categoria { get; set; }

        /// <summary>
        /// Unidad de medida: Unidad, Juego, Docena, etc.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Unidad { get; set; } = string.Empty;

        /// <summary>
        /// Precio unitario del utensilio
        /// </summary>
        [Required]
        [Range(0.01, 999999.99)]
        public decimal Precio { get; set; }

        /// <summary>
        /// Descripción adicional del utensilio (opcional)
        /// </summary>
        [StringLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Estado del utensilio (Activo/Inactivo)
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

    /// <summary>
    /// Enumeración para unidades de medida
    /// </summary>
    public static class UnidadMedida
    {
        public const string Unidad = "Unidad";
        public const string Juego = "Juego";
        public const string Docena = "Docena";
        public const string Par = "Par";
        public const string Set = "Set";

        public static readonly string[] Unidades = { Unidad, Juego, Docena, Par, Set };
    }
}