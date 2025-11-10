using Puerto92.Models;

namespace Puerto92.Services
{
    /// <summary>
    /// Interfaz para el servicio de notificaciones
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Crear una notificación
        /// </summary>
        Task<Notificacion> CrearNotificacionAsync(
            string usuarioId,
            string tipo,
            string titulo,
            string mensaje,
            string? urlAccion = null,
            string? textoAccion = null,
            string icono = "bell",
            string color = "primary",
            string prioridad = "Media",
            bool mostrarPopup = true,
            string? datosAdicionales = null,
            DateTime? fechaExpiracion = null);

        /// <summary>
        /// Crear notificación de asignación de kardex
        /// </summary>
        Task<Notificacion> CrearNotificacionAsignacionKardexAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string? empleadosAdicionales = null,
            bool esResponsableCompartidas = false);

        /// <summary>
        /// Crear notificación de reasignación de kardex
        /// </summary>
        Task<Notificacion> CrearNotificacionReasignacionKardexAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string motivo);
            
        /// <summary>
        /// Crear notificación de cancelación de kardex
        /// </summary>
        Task<Notificacion> CrearNotificacionCancelacionKardexAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string motivo);

        /// <summary>
        /// Crear notificación de que su kardex fue reasignado a otra persona
        /// </summary>
        Task<Notificacion> CrearNotificacionKardexReasignadoAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string nuevoResponsable,
            string motivo);

        /// <summary>
        /// Crear notificación cuando el administrador recibe un kardex completado
        /// </summary>
        Task<Notificacion> CrearNotificacionKardexRecibidoAsync(
            string administradorId,
            string tipoKardex,
            string empleadoResponsable,
            DateTime fecha);

        /// <summary>
        /// Crear notificación cuando un kardex es aprobado
        /// </summary>
        Task<Notificacion> CrearNotificacionKardexAprobadoAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string? observaciones = null);

        /// <summary>
        /// Crear notificación cuando un kardex es rechazado
        /// </summary>
        Task<Notificacion> CrearNotificacionKardexRechazadoAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string motivo);

        /// <summary>
        /// Obtener notificaciones de un usuario
        /// </summary>
        Task<List<Notificacion>> ObtenerNotificacionesUsuarioAsync(
            string usuarioId,
            bool soloNoLeidas = false,
            int cantidad = 10);

        /// <summary>
        /// Obtener cantidad de notificaciones no leídas
        /// </summary>
        Task<int> ContarNoLeidasAsync(string usuarioId);

        /// <summary>
        /// Marcar notificación como leída
        /// </summary>
        Task MarcarComoLeidaAsync(int notificacionId);

        /// <summary>
        /// Marcar todas las notificaciones de un usuario como leídas
        /// </summary>
        Task MarcarTodasComoLeidasAsync(string usuarioId);

        /// <summary>
        /// Eliminar notificación
        /// </summary>
        Task EliminarNotificacionAsync(int notificacionId);

        /// <summary>
        /// Limpiar notificaciones expiradas
        /// </summary>
        Task LimpiarNotificacionesExpiradasAsync();
    }
}