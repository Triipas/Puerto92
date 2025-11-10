using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para el registro de kardex de cocina (Fría, Caliente, Parrilla)
    /// </summary>
    public class KardexCocina
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
        /// Tipo de cocina: "Cocina Fría", "Cocina Caliente", "Parrilla"
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TipoCocina { get; set; } = string.Empty;

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
        public ICollection<KardexCocinaDetalle> Detalles { get; set; } = new List<KardexCocinaDetalle>();
    }

    /// <summary>
    /// Detalle de cada producto en el kardex de cocina
    /// </summary>
    public class KardexCocinaDetalle
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del kardex padre
        /// </summary>
        [Required]
        public int KardexCocinaId { get; set; }
        public KardexCocina? KardexCocina { get; set; }

        /// <summary>
        /// ID del producto
        /// </summary>
        [Required]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        /// <summary>
        /// Unidad de medida: KG, L, UNID, UD, PAQUETE
        /// </summary>
        [Required]
        [StringLength(20)]
        public string UnidadMedida { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad a pedir (calculado automáticamente)
        /// </summary>
        public decimal CantidadAPedir { get; set; }

        /// <summary>
        /// Ingresos del día (pre-llenado desde compras)
        /// </summary>
        public decimal Ingresos { get; set; }

        /// <summary>
        /// Stock final contado por el cocinero
        /// </summary>
        public decimal? StockFinal { get; set; }

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
    /// Unidades de medida para kardex de cocina
    /// </summary>
    public static class UnidadMedidaCocina
    {
        public const string Kilogramo = "KG";
        public const string Litro = "L";
        public const string Unidad = "UNID";
        public const string UnidadCorta = "UD";
        public const string Paquete = "PAQUETE";

        public static readonly string[] Todas = new[]
        {
            Kilogramo,
            Litro,
            Unidad,
            UnidadCorta,
            Paquete
        };

        /// <summary>
        /// Verificar si la unidad permite decimales
        /// </summary>
        public static bool PermiteDecimales(string unidad)
        {
            return unidad == Kilogramo || unidad == Litro;
        }
    }

    /// <summary>
    /// Tipos de cocina para kardex especializados
    /// </summary>
    public static class TipoCocinaKardex
    {
        public const string CocinaFria = "Cocina Fría";
        public const string CocinaCaliente = "Cocina Caliente";
        public const string Parrilla = "Parrilla";

        public static readonly string[] Todos = new[]
        {
            CocinaFria,
            CocinaCaliente,
            Parrilla
        };

        /// <summary>
        /// Obtener categoría especial por tipo de cocina
        /// </summary>
        public static string ObtenerCategoriaEspecial(string tipoCocina)
        {
            return tipoCocina switch
            {
                CocinaFria => "FRÍOS Y CEVICHES",
                CocinaCaliente => "CALIENTES Y ARROCES",
                Parrilla => "CROCANTES",
                _ => ""
            };
        }
    }
}