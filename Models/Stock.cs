using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para el stock actual de productos
    /// </summary>
    public class StockProducto
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del producto
        /// </summary>
        [Required]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        /// <summary>
        /// ID del local
        /// </summary>
        [Required]
        public int LocalId { get; set; }
        public Local? Local { get; set; }

        /// <summary>
        /// Cantidad actual en stock
        /// </summary>
        [Required]
        public decimal CantidadActual { get; set; }

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime FechaUltimaActualizacion { get; set; } = DateTime.Now;

        /// <summary>
        /// ID del último kardex que actualizó este stock
        /// </summary>
        public int? UltimoKardexId { get; set; }

        /// <summary>
        /// Tipo del último kardex que actualizó
        /// </summary>
        [StringLength(50)]
        public string? UltimoKardexTipo { get; set; }
    }

    /// <summary>
    /// Modelo para el stock actual de utensilios
    /// </summary>
    public class StockUtensilio
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del utensilio
        /// </summary>
        [Required]
        public int UtensilioId { get; set; }
        public Utensilio? Utensilio { get; set; }

        /// <summary>
        /// ID del local
        /// </summary>
        [Required]
        public int LocalId { get; set; }
        public Local? Local { get; set; }

        /// <summary>
        /// Cantidad actual en stock
        /// </summary>
        [Required]
        public int CantidadActual { get; set; }

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime FechaUltimaActualizacion { get; set; } = DateTime.Now;

        /// <summary>
        /// ID del último kardex que actualizó este stock
        /// </summary>
        public int? UltimoKardexId { get; set; }

        /// <summary>
        /// Tipo del último kardex que actualizó
        /// </summary>
        [StringLength(50)]
        public string? UltimoKardexTipo { get; set; }
    }

    /// <summary>
    /// Historial de movimientos de stock
    /// </summary>
    public class HistorialStock
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tipo: Producto o Utensilio
        /// </summary>
        [Required]
        [StringLength(20)]
        public string TipoItem { get; set; } = string.Empty;

        /// <summary>
        /// ID del producto o utensilio
        /// </summary>
        [Required]
        public int ItemId { get; set; }

        /// <summary>
        /// ID del local
        /// </summary>
        [Required]
        public int LocalId { get; set; }
        public Local? Local { get; set; }

        /// <summary>
        /// Cantidad anterior
        /// </summary>
        public decimal CantidadAnterior { get; set; }

        /// <summary>
        /// Cantidad nueva
        /// </summary>
        public decimal CantidadNueva { get; set; }

        /// <summary>
        /// Diferencia
        /// </summary>
        public decimal Diferencia { get; set; }

        /// <summary>
        /// Tipo de movimiento: Entrada, Salida, Ajuste, Aprobacion Kardex
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TipoMovimiento { get; set; } = string.Empty;

        /// <summary>
        /// ID del kardex relacionado (si aplica)
        /// </summary>
        public int? KardexId { get; set; }

        /// <summary>
        /// Tipo de kardex relacionado
        /// </summary>
        [StringLength(50)]
        public string? KardexTipo { get; set; }

        /// <summary>
        /// Usuario que realizó el movimiento
        /// </summary>
        [StringLength(100)]
        public string? UsuarioId { get; set; }

        /// <summary>
        /// Fecha y hora del movimiento
        /// </summary>
        public DateTime FechaHora { get; set; } = DateTime.Now;

        /// <summary>
        /// Observaciones
        /// </summary>
        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}