using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para el sistema de notificaciones del sistema
    /// </summary>
    public class Notificacion
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del usuario destinatario de la notificación
        /// </summary>
        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        /// <summary>
        /// Navegación al usuario
        /// </summary>
        public virtual Usuario? Usuario { get; set; }

        /// <summary>
        /// Tipo de notificación (AsignacionKardex, CambioPassword, Alerta, etc.)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = string.Empty;

        /// <summary>
        /// Título de la notificación
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje de la notificación
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// URL de acción (opcional) - a dónde redirigir al hacer clic
        /// </summary>
        [StringLength(500)]
        public string? UrlAccion { get; set; }

        /// <summary>
        /// Texto del botón de acción (opcional)
        /// </summary>
        [StringLength(100)]
        public string? TextoAccion { get; set; }

        /// <summary>
        /// Datos adicionales en formato JSON (opcional)
        /// </summary>
        public string? DatosAdicionales { get; set; }

        /// <summary>
        /// Icono de la notificación (clase Font Awesome)
        /// </summary>
        [StringLength(50)]
        public string Icono { get; set; } = "bell";

        /// <summary>
        /// Color de la notificación (primary, success, warning, danger, info)
        /// </summary>
        [StringLength(20)]
        public string Color { get; set; } = "primary";

        /// <summary>
        /// Prioridad de la notificación (Alta, Media, Baja)
        /// </summary>
        [StringLength(20)]
        public string Prioridad { get; set; } = "Media";

        /// <summary>
        /// Indica si la notificación fue leída
        /// </summary>
        public bool Leida { get; set; } = false;

        /// <summary>
        /// Fecha y hora de lectura
        /// </summary>
        public DateTime? FechaLectura { get; set; }

        /// <summary>
        /// Fecha y hora de creación
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha y hora de expiración (opcional)
        /// </summary>
        public DateTime? FechaExpiracion { get; set; }

        /// <summary>
        /// Indica si se debe mostrar como pop-up
        /// </summary>
        public bool MostrarPopup { get; set; } = true;
    }

    /// <summary>
    /// Tipos de notificaciones del sistema
    /// </summary>
    public static class TipoNotificacion
    {
    public const string AsignacionKardex = "AsignacionKardex";
    public const string ReasignacionKardex = "ReasignacionKardex";
    public const string CancelacionKardex = "CancelacionKardex";
    public const string RecordatorioKardex = "RecordatorioKardex";
    public const string KardexRecibido = "KardexRecibido";
    public const string KardexAprobado = "KardexAprobado";
    public const string KardexRechazado = "KardexRechazado";
    public const string CambioPassword = "CambioPassword";
    public const string AlertaSistema = "AlertaSistema";
    public const string Mensaje = "Mensaje";
    }

    /// <summary>
    /// Prioridades de notificaciones
    /// </summary>
    public static class PrioridadNotificacion
    {
        public const string Alta = "Alta";
        public const string Media = "Media";
        public const string Baja = "Baja";
    }

    /// <summary>
    /// Colores de notificaciones
    /// </summary>
    public static class ColorNotificacion
    {
        public const string Primary = "primary";    // Azul
        public const string Success = "success";    // Verde
        public const string Warning = "warning";    // Naranja
        public const string Danger = "danger";      // Rojo
        public const string Info = "info";          // Celeste
    }
}