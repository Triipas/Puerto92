using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de Personal Presente
    /// </summary>
    public class PersonalPresenteViewModel
    {
        public int KardexId { get; set; }
        public string TipoKardex { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public int LocalId { get; set; }
        public string EmpleadoResponsableId { get; set; } = string.Empty;
        public string EmpleadoResponsableNombre { get; set; } = string.Empty;
        
        public List<EmpleadoDisponibleDto> EmpleadosDisponibles { get; set; } = new();
        
        public int TotalEmpleados { get; set; }
        public int TotalSeleccionados { get; set; }
        public DateTime HoraActual { get; set; } = DateTime.Now;
        public TimeSpan HoraLimiteEnvio { get; set; } = new TimeSpan(17, 30, 0); // 5:30 PM
        public bool DentroDeHorario { get; set; }
        public bool EnvioHabilitadoManualmente { get; set; } = false;
        public string? MotivoHabilitacionManual { get; set; }
        public string? HabilitadoPor { get; set; }
    }

    /// <summary>
    /// DTO para empleados disponibles
    /// </summary>
    public class EmpleadoDisponibleDto
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool EsResponsablePrincipal { get; set; } = false;
        public bool Seleccionado { get; set; } = false;
    }

    /// <summary>
    /// Request para guardar personal presente
    /// </summary>
    public class PersonalPresenteRequest
    {
        [Required]
        public int KardexId { get; set; }
        
        [Required]
        public string TipoKardex { get; set; } = string.Empty;
        
        [Required]
        public List<string> EmpleadosPresentes { get; set; } = new();
        
        public string? ObservacionesKardex { get; set; }
    }

    /// <summary>
    /// Response para el personal presente
    /// </summary>
    public class PersonalPresenteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRegistrados { get; set; }
    }
}