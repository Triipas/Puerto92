using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para el registro de kardex de bebidas con conteo por ubicación
    /// </summary>
    public class KardexBebidas
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID de la asignación relacionada
        /// </summary>
        [Required]
        public int AsignacionId { get; set; }
        public AsignacionKardex? Asignacion { get; set; }

        /// <summary>
        /// Fecha del kardex
        /// </summary>
        [Required]
        public DateTime Fecha { get; set; }

        /// <summary>
        /// ID del local
        /// </summary>
        [Required]
        public int LocalId { get; set; }
        public Local? Local { get; set; }

        /// <summary>
        /// ID del empleado responsable
        /// </summary>
        [Required]
        public string EmpleadoId { get; set; } = string.Empty;
        public Usuario? Empleado { get; set; }

        /// <summary>
        /// Estado del kardex: Borrador, Completado, Aprobado, Rechazado
        /// </summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Borrador";

        /// <summary>
        /// Fecha de inicio del registro
        /// </summary>
        public DateTime? FechaInicio { get; set; }

        /// <summary>
        /// Fecha de finalización del registro
        /// </summary>
        public DateTime? FechaFinalizacion { get; set; }

        /// <summary>
        /// Fecha de envío al supervisor
        /// </summary>
        public DateTime? FechaEnvio { get; set; }

        /// <summary>
        /// Observaciones generales del kardex
        /// </summary>
        public string? Observaciones { get; set; }

        /// <summary>
        /// Fecha de aprobación del kardex
        /// </summary>
        public DateTime? FechaAprobacion { get; set; }

        /// <summary>
        /// Observaciones del revisor al aprobar
        /// </summary>
        [StringLength(500)]
        public string? ObservacionesRevision { get; set; }

        /// <summary>
        /// Motivo del rechazo (si aplica)
        /// </summary>
        [StringLength(1000)]
        public string? MotivoRechazo { get; set; }

        /// <summary>
        /// Fecha de rechazo (si aplica)
        /// </summary>
        public DateTime? FechaRechazo { get; set; }

        /// <summary>
        /// Detalle de productos del kardex
        /// </summary>
        public ICollection<KardexBebidasDetalle> Detalles { get; set; } = new List<KardexBebidasDetalle>();
    }

    /// <summary>
    /// Detalle de cada producto en el kardex de bebidas
    /// </summary>
    public class KardexBebidasDetalle
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del kardex padre
        /// </summary>
        [Required]
        public int KardexBebidasId { get; set; }
        public KardexBebidas? KardexBebidas { get; set; }

        /// <summary>
        /// ID del producto (de la tabla Productos)
        /// </summary>
        [Required]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        /// <summary>
        /// Inventario inicial (del cierre anterior o del sistema)
        /// </summary>
        [Required]
        public decimal InventarioInicial { get; set; }

        /// <summary>
        /// Ingresos/Compras del día
        /// </summary>
        public decimal Ingresos { get; set; }

        /// <summary>
        /// Conteo en almacén
        /// </summary>
        public decimal? ConteoAlmacen { get; set; }

        /// <summary>
        /// Conteo en refrigeradora 1
        /// </summary>
        public decimal? ConteoRefri1 { get; set; }

        /// <summary>
        /// Conteo en refrigeradora 2
        /// </summary>
        public decimal? ConteoRefri2 { get; set; }

        /// <summary>
        /// Conteo en refrigeradora 3
        /// </summary>
        public decimal? ConteoRefri3 { get; set; }

        /// <summary>
        /// Conteo final calculado (suma de todas las ubicaciones)
        /// </summary>
        public decimal ConteoFinal { get; set; }

        /// <summary>
        /// Ventas calculadas (Inv. Inicial + Ingresos - Conteo Final)
        /// </summary>
        public decimal Ventas { get; set; }

        /// <summary>
        /// Diferencia porcentual (para alertas)
        /// </summary>
        public decimal? DiferenciaPorcentual { get; set; }

        /// <summary>
        /// Indica si tiene diferencia significativa (> 10%)
        /// </summary>
        public bool TieneDiferenciaSignificativa { get; set; }

        /// <summary>
        /// Observaciones específicas del producto
        /// </summary>
        public string? Observaciones { get; set; }

        /// <summary>
        /// Orden de visualización
        /// </summary>
        public int Orden { get; set; }
    }

    /// <summary>
    /// Estados del kardex
    /// </summary>
    public static class EstadoKardex
    {
        public const string Borrador = "Borrador";
        public const string Completado = "Completado";
        public const string Enviado = "Enviado";
        public const string Aprobado = "Aprobado";
        public const string Rechazado = "Rechazado";
    }
}