using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para el kardex de salón (utensilios)
    /// </summary>
    public class KardexSalonViewModel
    {
        public int Id { get; set; }
        public int AsignacionId { get; set; }
        public DateTime Fecha { get; set; }
        public int LocalId { get; set; }
        public string EmpleadoId { get; set; } = string.Empty;
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "Borrador";
        
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        
        public string? Observaciones { get; set; }
        
        public List<KardexSalonDetalleViewModel> Detalles { get; set; } = new();
        
        // Información adicional
        public int TotalUtensilios { get; set; }
        public int UtensiliosCompletos { get; set; }
        public int UtensiliosConFaltantes { get; set; }
        public decimal PorcentajeAvance { get; set; }
    }

    /// <summary>
    /// ViewModel para el detalle de cada utensilio
    /// </summary>
    public class KardexSalonDetalleViewModel
    {
        public int Id { get; set; }
        public int UtensilioId { get; set; }
        
        // Información del utensilio
        public string Categoria { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        
        // Datos del kardex
        public int InventarioInicial { get; set; }
        public int? UnidadesContadas { get; set; }
        
        // Calculados
        public int Diferencia { get; set; }
        public bool TieneFaltantes { get; set; }
        public string? DescripcionFaltantes { get; set; }
        
        public string? Observaciones { get; set; }
        public int Orden { get; set; }
        
        // Estado de completitud
        public bool EstaCompleto { get; set; }
    }

    /// <summary>
    /// ViewModel para autoguardado
    /// </summary>
    public class AutoguardadoKardexSalonRequest
    {
        public int KardexId { get; set; }
        public int DetalleId { get; set; }
        public int? UnidadesContadas { get; set; }
    }

    /// <summary>
    /// ViewModel para guardar descripción de faltantes
    /// </summary>
    public class DescripcionFaltantesRequest
    {
        [Required]
        public int KardexId { get; set; }
        
        [Required]
        public int DetalleId { get; set; }
        
        [Required(ErrorMessage = "Debe especificar qué pasó con los faltantes")]
        [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string DescripcionFaltantes { get; set; } = string.Empty;
    }
}