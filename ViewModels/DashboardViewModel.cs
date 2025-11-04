namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para el Dashboard principal
    /// Se adapta según el rol del usuario
    /// </summary>
    public class DashboardViewModel
    {
        // === INFORMACIÓN DEL USUARIO ===
        public string NombreUsuario { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string LocalNombre { get; set; } = string.Empty;
        public DateTime? UltimoAcceso { get; set; }

        // === ESTADÍSTICAS GENERALES ===
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int TotalLocales { get; set; }
        public int LocalesActivos { get; set; }
        public int TotalProductos { get; set; }
        public int ProductosActivos { get; set; }
        public int TotalUtensilios { get; set; }
        public int UtensiliosActivos { get; set; }
        public int TotalProveedores { get; set; }
        public int ProveedoresActivos { get; set; }

        // === PARA CONTADOR (UTENSILIOS) ===
        public decimal ValorInventarioUtensilios { get; set; }
        public int UtensiliosCocina { get; set; }
        public int UtensiliosMozos { get; set; }
        public int UtensiliosVajilla { get; set; }

        // === PARA SUPERVISORA (PRODUCTOS) ===
        public int ProductosBebidas { get; set; }
        public int ProductosCocina { get; set; }

        // === PARA ADMINISTRADOR LOCAL (ASIGNACIONES) ===
        public int AsignacionesPendientes { get; set; }
        public int KardexHoy { get; set; }
        public int AsignacionesHoy { get; set; }
        public int AsignacionesCompletadas { get; set; }

        // === ÚLTIMOS USUARIOS (ADMIN MAESTRO) ===
        public List<UsuarioResumeViewModel> UltimosUsuarios { get; set; } = new();
    }

    /// <summary>
    /// ViewModel resumido para mostrar usuarios en el dashboard
    /// </summary>
    public class UsuarioResumeViewModel
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }
    }
}