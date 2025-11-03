namespace Puerto92.ViewModels.Shared
{
    /// <summary>
    /// ViewModel para items de navegación del sidebar
    /// </summary>
    public class NavigationItemViewModel
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = "Index";
        public List<string> RequiredRoles { get; set; } = new();
        public bool IsActive { get; set; }
        public int? Badge { get; set; } // Para notificaciones
        public List<NavigationItemViewModel> SubItems { get; set; } = new();
    }

    /// <summary>
    /// ViewModel para el Sidebar completo
    /// </summary>
    public class SidebarViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string UserRoleDisplay { get; set; } = string.Empty;
        public List<NavigationItemViewModel> NavigationItems { get; set; } = new();
        public string CurrentController { get; set; } = string.Empty;
        public string CurrentAction { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel para información del usuario
    /// </summary>
    public class UserInfoViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string RoleDisplay { get; set; } = string.Empty;
        public string LocalName { get; set; } = string.Empty;
        public DateTime? LastAccess { get; set; }
    }

    /// <summary>
    /// ViewModel para tarjetas de estadísticas
    /// </summary>
    public class StatsCardViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = "primary"; // primary, success, danger, warning
        public string? Link { get; set; }
    }
}