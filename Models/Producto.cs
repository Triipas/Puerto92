using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para representar un producto del catálogo base
    /// </summary>
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Código único del producto (autogenerado si no se proporciona)
        /// Formato: PROD-001, PROD-002, etc.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del producto
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// ID de la categoría del producto
        /// </summary>
        [Required]
        public int CategoriaId { get; set; }

        /// <summary>
        /// Navegación a la categoría
        /// </summary>
        public virtual Categoria? Categoria { get; set; }

        /// <summary>
        /// Unidad de medida: Unidad, Kilogramo, Litro, Caja, Docena, etc.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Unidad { get; set; } = string.Empty;

        /// <summary>
        /// Precio de compra del producto
        /// </summary>
        [Required]
        [Range(0.01, 999999.99)]
        public decimal PrecioCompra { get; set; }

        /// <summary>
        /// Precio de venta del producto
        /// </summary>
        [Required]
        [Range(0.01, 999999.99)]
        public decimal PrecioVenta { get; set; }

        /// <summary>
        /// Descripción adicional del producto (opcional)
        /// </summary>
        [StringLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Estado del producto (Activo/Inactivo)
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
    /// Enumeración para unidades de medida de productos
    /// </summary>
    public static class UnidadMedidaProducto
    {
        public const string Unidad = "Unidad";
        public const string Kilogramo = "Kilogramo";
        public const string Litro = "Litro";
        public const string Caja = "Caja";
        public const string Docena = "Docena";
        public const string Bolsa = "Bolsa";
        public const string Paquete = "Paquete";

        public static readonly string[] Unidades = 
        { 
            Unidad, 
            Kilogramo, 
            Litro, 
            Caja, 
            Docena, 
            Bolsa, 
            Paquete 
        };
    }
}