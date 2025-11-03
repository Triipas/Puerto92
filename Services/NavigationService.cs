using Puerto92.ViewModels.Shared;
using System.Security.Claims;

namespace Puerto92.Services
{
    public interface INavigationService
    {
        List<NavigationItemViewModel> GetNavigationItems(ClaimsPrincipal user);
        string GetRoleDisplayName(string role);
    }

    public class NavigationService : INavigationService
    {
        /// <summary>
        /// Obtiene los items de navegación según el rol del usuario
        /// </summary>
        public List<NavigationItemViewModel> GetNavigationItems(ClaimsPrincipal user)
        {
            var items = new List<NavigationItemViewModel>();

            // Dashboard - Todos los roles
            items.Add(new NavigationItemViewModel
            {
                Icon = "fa-chart-line",
                Title = "Dashboard",
                Controller = "Home",
                Action = "Index",
                RequiredRoles = new List<string>()
            });

            // === ADMIN MAESTRO Y ADMINISTRADOR LOCAL ===
            if (user.IsInRole("Admin Maestro") || user.IsInRole("Administrador Local"))
            {
                items.Add(new NavigationItemViewModel
                {
                    Icon = "fa-users",
                    Title = "Usuarios",
                    Controller = "Usuarios",
                    Action = "Index",
                    RequiredRoles = new List<string> { "Admin Maestro", "Administrador Local" }
                });
            }

            // === SOLO ADMIN MAESTRO ===
            if (user.IsInRole("Admin Maestro"))
            {
                items.Add(new NavigationItemViewModel
                {
                    Icon = "fa-store",
                    Title = "Locales",
                    Controller = "Locales",
                    Action = "Index",
                    RequiredRoles = new List<string> { "Admin Maestro" }
                });
            }

            // Configuración - Todos los roles (acción específica para diferenciar de Dashboard)
            items.Add(new NavigationItemViewModel
            {
                Icon = "fa-gear",
                Title = "Configuración",
                Controller = "Home",
                Action = "Configuracion", // ← Acción específica
                RequiredRoles = new List<string>()
            });

            return items;
        }

        /// <summary>
        /// Obtiene el nombre amigable del rol
        /// </summary>
        public string GetRoleDisplayName(string role)
        {
            return role switch
            {
                "Admin Maestro" => "Admin Maestro",
                "Administrador Local" => "Admin Local",
                "Supervisora de Calidad" => "Supervisora",
                "Contador" => "Contador",
                "Jefe de Cocina" => "Jefe de Cocina",
                "Mozo" => "Mozo",
                "Cocinero" => "Cocinero",
                "Vajillero" => "Vajillero",
                _ => "Usuario"
            };
        }
    }
}