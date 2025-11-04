using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para asignación de responsables de kardex
    /// </summary>
    public class AsignacionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de kardex es obligatorio")]
        [Display(Name = "Tipo de Kardex")]
        public string TipoKardex { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un empleado")]
        [Display(Name = "Empleado Responsable")]
        public string EmpleadoId { get; set; } = string.Empty;

        public string? EmpleadoNombre { get; set; }
        public string? EmpleadoRol { get; set; }

        public int LocalId { get; set; }
        public string? LocalNombre { get; set; }

        public string Estado { get; set; } = "Pendiente";
        public bool EsReasignacion { get; set; }
        public string? EmpleadoOriginal { get; set; }
        public string? MotivoReasignacion { get; set; }
        public DateTime? FechaReasignacion { get; set; }
        public bool RegistroIniciado { get; set; }
        public bool NotificacionEnviada { get; set; }
    }

    /// <summary>
    /// ViewModel para reasignación de kardex
    /// </summary>
    public class ReasignacionViewModel
    {
        public int AsignacionId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un nuevo empleado")]
        [Display(Name = "Nuevo Responsable")]
        public string NuevoEmpleadoId { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
        [Display(Name = "Motivo de la Reasignación")]
        public string? Motivo { get; set; }

        // Datos de la asignación actual
        public string TipoKardex { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string EmpleadoActualId { get; set; } = string.Empty;
        public string EmpleadoActualNombre { get; set; } = string.Empty;
        public bool RegistroIniciado { get; set; }
    }

    /// <summary>
    /// ViewModel para el calendario de asignaciones
    /// </summary>
    public class CalendarioAsignacionesViewModel
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string TipoKardexActual { get; set; } = string.Empty;
        
        public List<AsignacionViewModel> Asignaciones { get; set; } = new();
        public Dictionary<string, int> Estadisticas { get; set; } = new();
        
        // Para facilitar la navegación del calendario
        public DateTime PrimerDiaMes { get; set; }
        public DateTime UltimoDiaMes { get; set; }
        public int TotalDiasMes { get; set; }
        public int DiaSemanaInicio { get; set; } // 0 = Domingo, 1 = Lunes, etc.
    }

    /// <summary>
    /// ViewModel para guardar múltiples asignaciones
    /// </summary>
    public class GuardarAsignacionesViewModel
    {
        public List<AsignacionViewModel> Asignaciones { get; set; } = new();
    }

    /// <summary>
    /// ViewModel para empleados disponibles por rol
    /// </summary>
    public class EmpleadoDisponibleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Disponible { get; set; } = true;
        public string? MotivoNoDisponible { get; set; }
    }

    /// <summary>
    /// ViewModel para estadísticas de asignaciones
    /// </summary>
    public class EstadisticasAsignacionesViewModel
    {
        public int TotalPendientes { get; set; }
        public int TotalAsignadas { get; set; }
        public int TotalEmpleadas { get; set; }
        public int TotalReasignaciones { get; set; }
        public int TotalCompletadas { get; set; }
    }
}