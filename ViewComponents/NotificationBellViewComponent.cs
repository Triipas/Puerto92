using Microsoft.AspNetCore.Mvc;
using Puerto92.Services;
using System.Security.Claims;

namespace Puerto92.ViewComponents
{
    /// <summary>
    /// View Component para el ícono de notificaciones (campana)
    /// </summary>
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationBellViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var usuarioId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(usuarioId))
            {
                return View(0); // Sin notificaciones si no está autenticado
            }

            // Contar notificaciones no leídas
            var count = await _notificationService.ContarNoLeidasAsync(usuarioId);

            return View(count);
        }
    }
}