using System.ComponentModel.DataAnnotations;

namespace Puerto92.Models
{
    /// <summary>
    /// Modelo para registrar todas las acciones de auditoría del sistema
    /// </summary>
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tipo de acción realizada (Login, CrearUsuario, EditarUsuario, etc.)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Accion { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada de la acción
        /// </summary>
        [StringLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Usuario que realizó la acción
        /// </summary>
        [StringLength(100)]
        public string? UsuarioAccion { get; set; }

        /// <summary>
        /// ID del usuario que realizó la acción (puede ser null para acciones anónimas)
        /// </summary>
        public string? UsuarioId { get; set; }

        /// <summary>
        /// Usuario afectado por la acción (para creación, edición, eliminación de usuarios)
        /// </summary>
        [StringLength(100)]
        public string? UsuarioAfectado { get; set; }

        /// <summary>
        /// Dirección IP desde donde se realizó la acción
        /// </summary>
        [StringLength(45)] // IPv6 puede ser hasta 45 caracteres
        public string? DireccionIP { get; set; }

        /// <summary>
        /// Fecha y hora de la acción
        /// </summary>
        public DateTime FechaHora { get; set; } = DateTime.Now;

        /// <summary>
        /// Resultado de la acción (Exitoso, Fallido, Error)
        /// </summary>
        [StringLength(50)]
        public string Resultado { get; set; } = "Exitoso";

        /// <summary>
        /// Información adicional en formato JSON
        /// </summary>
        public string? DatosAdicionales { get; set; }

        /// <summary>
        /// Módulo del sistema (Usuarios, Locales, Inventario, etc.)
        /// </summary>
        [StringLength(50)]
        public string? Modulo { get; set; }

        /// <summary>
        /// Nivel de severidad (Info, Warning, Error, Critical)
        /// </summary>
        [StringLength(20)]
        public string NivelSeveridad { get; set; } = "Info";
    }

    /// <summary>
    /// Enumeración para tipos de acciones de auditoría
    /// </summary>
    public static class AccionAuditoria
    {
        // Autenticación
        public const string LoginExitoso = "Login Exitoso";
        public const string LoginFallido = "Login Fallido";
        public const string Logout = "Logout";
        public const string CambioPassword = "Cambio de Contraseña";
        public const string ResetPassword = "Reset de Contraseña";

        // Usuarios
        public const string CrearUsuario = "Crear Usuario";
        public const string EditarUsuario = "Editar Usuario";
        public const string EliminarUsuario = "Eliminar Usuario";
        public const string ActivarUsuario = "Activar Usuario";
        public const string DesactivarUsuario = "Desactivar Usuario";
        public const string CambiarRolUsuario = "Cambiar Rol de Usuario";

        // Locales
        public const string CrearLocal = "Crear Local";
        public const string EditarLocal = "Editar Local";
        public const string DesactivarLocal = "Desactivar Local";
        public const string ActivarLocal = "Activar Local";

        // Sistema
        public const string AccesoDenegado = "Acceso Denegado";
        public const string ErrorSistema = "Error de Sistema";
    }

    /// <summary>
    /// Enumeración para resultados de auditoría
    /// </summary>
    public static class ResultadoAuditoria
    {
        public const string Exitoso = "Exitoso";
        public const string Fallido = "Fallido";
        public const string Error = "Error";
        public const string Denegado = "Denegado";
    }

    /// <summary>
    /// Enumeración para niveles de severidad
    /// </summary>
    public static class NivelSeveridad
    {
        public const string Info = "Info";
        public const string Warning = "Warning";
        public const string Error = "Error";
        public const string Critical = "Critical";
    }
}