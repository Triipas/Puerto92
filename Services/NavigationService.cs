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

            // === ADMINISTRADOR LOCAL ===
            if (user.IsInRole("Administrador Local"))
            {
                items.Add(new NavigationItemViewModel
                {
                    Icon = "fa-calendar-days",
                    Title = "Asignaciones",
                    Controller = "Asignaciones",
                    Action = "Index",
                    RequiredRoles = new List<string> { "Administrador Local" }
                });
            }

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

            // SUPERVISORA DE CALIDAD - CATÁLOGO DE PRODUCTOS
            if (user.IsInRole("Supervisora de Calidad"))
            {
                items.Add(new NavigationItemViewModel
                {
                    Icon = "fa-box",
                    Title = "Catálogo de Productos",
                    Controller = "Productos",
                    Action = "Index",
                    RequiredRoles = new List<string> { "Supervisora de Calidad" }
                });
            }

            if (user.IsInRole("Supervisora de Calidad"))
            {
                items.Add(new NavigationItemViewModel
                {
                    Icon = "fa-truck-field",
                    Title = "Proveedores",
                    Controller = "Proveedores",
                    Action = "Index",
                    RequiredRoles = new List<string> { "Supervisora de Calidad" }
                });
            }

            // CONTADOR - CATÁLOGO DE UTENSILIOS
            if (user.IsInRole("Contador"))
            {
                items.Add(new NavigationItemViewModel
                {
                    Icon = "fa-utensils",
                    Title = "Catálogo de Utensilios",
                    Controller = "Utensilios",
                    Action = "Index",
                    RequiredRoles = new List<string> { "Contador" }
                });
            }

            // === SOLO ADMIN MAESTRO - CONFIGURACIÓN ===
            if (user.IsInRole("Admin Maestro"))
            {
                items.Add(new NavigationItemViewModel
                {
                    Icon = "fa-gear",
                    Title = "Configuración",
                    Controller = "Categorias",
                    Action = "Index",
                    RequiredRoles = new List<string> { "Admin Maestro" }
                });
            }

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