using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Puerto92.Services;
using System.Security.Claims;

namespace Puerto92.Controllers
{
    [Authorize]
    public class NotificacionesController : BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificacionesController> _logger;

        public NotificacionesController(
            INotificationService notificationService,
            ILogger<NotificacionesController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener notificaciones del usuario actual
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotificaciones(bool soloNoLeidas = false, int cantidad = 10)
        {
            try
            {
                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(usuarioId))
                {
                    return Unauthorized();
                }

                var notificaciones = await _notificationService.ObtenerNotificacionesUsuarioAsync(
                    usuarioId, 
                    soloNoLeidas, 
                    cantidad);

                var resultado = notificaciones.Select(n => new
                {
                    id = n.Id,
                    tipo = n.Tipo,
                    titulo = n.Titulo,
                    mensaje = n.Mensaje,
                    urlAccion = n.UrlAccion,
                    textoAccion = n.TextoAccion,
                    icono = n.Icono,
                    color = n.Color,
                    prioridad = n.Prioridad,
                    leida = n.Leida,
                    fechaCreacion = n.FechaCreacion,
                    mostrarPopup = n.MostrarPopup
                });

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener notificaciones");
                return JsonError("Error al cargar las notificaciones");
            }
        }

        /// <summary>
        /// Obtener cantidad de notificaciones no leídas
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetContadorNoLeidas()
        {
            try
            {
                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(usuarioId))
                {
                    return Json(new { count = 0 });
                }

                var count = await _notificationService.ContarNoLeidasAsync(usuarioId);
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al contar notificaciones no leídas");
                return Json(new { count = 0 });
            }
        }

        /// <summary>
        /// Marcar notificación como leída
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarcarComoLeida(int id)
        {
            try
            {
                await _notificationService.MarcarComoLeidaAsync(id);
                return JsonSuccess("Notificación marcada como leída");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al marcar notificación {id} como leída");
                return JsonError("Error al marcar la notificación");
            }
        }

        /// <summary>
        /// Marcar todas las notificaciones como leídas
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarcarTodasComoLeidas()
        {
            try
            {
                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(usuarioId))
                {
                    return Unauthorized();
                }

                await _notificationService.MarcarTodasComoLeidasAsync(usuarioId);
                return JsonSuccess("Todas las notificaciones marcadas como leídas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar todas las notificaciones como leídas");
                return JsonError("Error al marcar las notificaciones");
            }
        }

        /// <summary>
        /// Eliminar notificación
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                await _notificationService.EliminarNotificacionAsync(id);
                return JsonSuccess("Notificación eliminada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar notificación {id}");
                return JsonError("Error al eliminar la notificación");
            }
        }
    }
}