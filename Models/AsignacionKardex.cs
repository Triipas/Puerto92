using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para asignación de responsables de kardex
    /// </summary>
    public class AsignacionKardex
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tipo de kardex: Cocina Fría, Cocina Caliente, Parrilla, Mozo Salón, Mozo Bebidas, Vajilla
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TipoKardex { get; set; } = string.Empty;

        /// <summary>
        /// Fecha para la cual se asigna el kardex
        /// </summary>
        [Required]
        public DateTime Fecha { get; set; }

        /// <summary>
        /// ID del empleado asignado como responsable
        /// </summary>
        [Required]
        public string EmpleadoId { get; set; } = string.Empty;

        /// <summary>
        /// Navegación al empleado asignado
        /// </summary>
        public Usuario? Empleado { get; set; }

        /// <summary>
        /// Local al que pertenece la asignación
        /// </summary>
        [Required]
        public int LocalId { get; set; }

        /// <summary>
        /// Navegación al local
        /// </summary>
        public Local? Local { get; set; }

        /// <summary>
        /// Estado de la asignación: Pendiente, Asignada, En Proceso, Completada
        /// </summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        /// <summary>
        /// Fecha y hora de creación de la asignación
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Usuario que creó la asignación
        /// </summary>
        [StringLength(100)]
        public string? CreadoPor { get; set; }

        /// <summary>
        /// Indica si esta asignación es una reasignación
        /// </summary>
        public bool EsReasignacion { get; set; } = false;

        /// <summary>
        /// ID de la asignación original (si es una reasignación)
        /// </summary>
        public int? AsignacionOriginalId { get; set; }

        /// <summary>
        /// Empleado original (si es una reasignación)
        /// </summary>
        [StringLength(100)]
        public string? EmpleadoOriginal { get; set; }

        /// <summary>
        /// Motivo de la reasignación
        /// </summary>
        [StringLength(500)]
        public string? MotivoReasignacion { get; set; }

        /// <summary>
        /// Fecha de reasignación
        /// </summary>
        public DateTime? FechaReasignacion { get; set; }

        /// <summary>
        /// Usuario que realizó la reasignación
        /// </summary>
        [StringLength(100)]
        public string? ReasignadoPor { get; set; }

        /// <summary>
        /// Indica si el empleado ya inició el registro del kardex
        /// </summary>
        public bool RegistroIniciado { get; set; } = false;

        /// <summary>
        /// Datos parciales del kardex (JSON) si fue iniciado
        /// </summary>
        public string? DatosParciales { get; set; }

        /// <summary>
        /// Fecha de notificación al empleado
        /// </summary>
        public DateTime? FechaNotificacion { get; set; }

        /// <summary>
        /// Indica si la notificación fue enviada
        /// </summary>
        public bool NotificacionEnviada { get; set; } = false;
    }

    /// <summary>
    /// Tipos de kardex disponibles
    /// </summary>
    public static class TipoKardex
    {
        public const string CocinaFria = "Cocina Fría";
        public const string CocinaCaliente = "Cocina Caliente";
        public const string Parrilla = "Parrilla";
        public const string MozoSalon = "Mozo Salón";
        public const string MozoBebidas = "Mozo Bebidas";
        public const string Vajilla = "Vajilla";

        public static readonly string[] Todos = new[]
        {
            CocinaFria,
            CocinaCaliente,
            Parrilla,
            MozoSalon,
            MozoBebidas,
            Vajilla
        };

        /// <summary>
        /// Obtener el rol requerido para un tipo de kardex
        /// </summary>
        public static string[] ObtenerRolesPermitidos(string tipoKardex)
        {
            return tipoKardex switch
            {
                CocinaFria => new[] { "Cocinero", "Jefe de Cocina" },
                CocinaCaliente => new[] { "Cocinero", "Jefe de Cocina" },
                Parrilla => new[] { "Cocinero", "Jefe de Cocina" },
                MozoSalon => new[] { "Mozo" },
                MozoBebidas => new[] { "Mozo" },
                Vajilla => new[] { "Vajillero", "Cocinero", "Jefe de Cocina" }, // Vajilla permite cocineros también
                _ => Array.Empty<string>()
            };
        }

        /// <summary>
        /// Obtener el icono para un tipo de kardex
        /// </summary>
        public static string ObtenerIcono(string tipoKardex)
        {
            return tipoKardex switch
            {
                CocinaFria => "snowflake",
                CocinaCaliente => "fire",
                Parrilla => "fire-burner",
                MozoSalon => "utensils",
                MozoBebidas => "wine-glass",
                Vajilla => "plate-utensils",
                _ => "clipboard-list"
            };
        }

        /// <summary>
        /// Obtener el color para un tipo de kardex
        /// </summary>
        public static string ObtenerColor(string tipoKardex)
        {
            return tipoKardex switch
            {
                CocinaFria => "#3B82F6", // Azul
                CocinaCaliente => "#EF4444", // Rojo
                Parrilla => "#F59E0B", // Naranja
                MozoSalon => "#10B981", // Verde
                MozoBebidas => "#8B5CF6", // Púrpura
                Vajilla => "#06B6D4", // Cyan
                _ => "#64748B" // Gris
            };
        }
    }

    /// <summary>
    /// Estados de asignación
    /// </summary>
    public static class EstadoAsignacion
    {
        public const string Pendiente = "Pendiente";
        public const string Asignada = "Asignada";
        public const string EnProceso = "En Proceso";
        public const string Completada = "Completada";
    }
}