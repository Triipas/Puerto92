using Microsoft.AspNetCore.Mvc;
using Puerto92.Services;
using Puerto92.ViewModels.Shared;
using System.Security.Claims;

namespace Puerto92.ViewComponents
{
    /// <summary>
    /// View Component para el Sidebar de navegación
    /// </summary>
    public class SidebarViewComponent : ViewComponent
    {
        private readonly INavigationService _navigationService;

        public SidebarViewComponent(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public IViewComponentResult Invoke()
        {
            // Obtener ClaimsPrincipal desde HttpContext
            var claimsPrincipal = HttpContext.User;
            
            var currentController = ViewContext.RouteData.Values["controller"]?.ToString() ?? "";
            var currentAction = ViewContext.RouteData.Values["action"]?.ToString() ?? "";
            
            var viewModel = new SidebarViewModel
            {
                UserName = claimsPrincipal.Identity?.Name ?? "Usuario",
                UserRole = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "",
                CurrentController = currentController,
                CurrentAction = currentAction,
                NavigationItems = _navigationService.GetNavigationItems(claimsPrincipal)
            };

            // Establecer nombre amigable del rol
            viewModel.UserRoleDisplay = _navigationService.GetRoleDisplayName(viewModel.UserRole);

            // Marcar item activo con lógica mejorada
            foreach (var item in viewModel.NavigationItems)
            {
                // Coincidencia exacta de controlador
                var controllerMatch = item.Controller.Equals(currentController, StringComparison.OrdinalIgnoreCase);
                
                if (controllerMatch)
                {
                    // Si el item tiene acción específica diferente de "Index", debe coincidir exactamente
                    if (!string.IsNullOrEmpty(item.Action) && !item.Action.Equals("Index", StringComparison.OrdinalIgnoreCase))
                    {
                        item.IsActive = item.Action.Equals(currentAction, StringComparison.OrdinalIgnoreCase);
                    }
                    // Si el item es Index o no tiene acción, marcar como activo solo si la acción actual es Index
                    else
                    {
                        item.IsActive = currentAction.Equals("Index", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return View(viewModel);
        }
    }

    /// <summary>
    /// View Component para información del usuario
    /// </summary>
    public class UserInfoViewComponent : ViewComponent
    {
        private readonly INavigationService _navigationService;

        public UserInfoViewComponent(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public IViewComponentResult Invoke()
        {
            // Obtener ClaimsPrincipal desde HttpContext
            var claimsPrincipal = HttpContext.User;
            var role = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "";
            
            var viewModel = new UserInfoViewModel
            {
                UserName = claimsPrincipal.Identity?.Name ?? "Usuario",
                FullName = claimsPrincipal.Identity?.Name ?? "Usuario", // TODO: Obtener de BD
                Role = role,
                RoleDisplay = _navigationService.GetRoleDisplayName(role),
                LocalName = "Local Principal" // TODO: Obtener de BD
            };

            return View(viewModel);
        }
    }

    /// <summary>
    /// View Component para tarjetas de estadísticas
    /// </summary>
    public class StatsCardViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(StatsCardViewModel model)
        {
            return View(model);
        }
    }
}