using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para el kardex de vajilla y utensilios de cocina
    /// </summary>
    public class KardexVajillaViewModel
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
        
        public string? DescripcionFaltantes { get; set; }
        public int? CantidadRotos { get; set; }
        public int? CantidadExtraviados { get; set; }
        public string? Observaciones { get; set; }
        
        public List<KardexVajillaDetalleViewModel> Detalles { get; set; } = new();
        
        // Información adicional
        public int TotalUtensilios { get; set; }
        public int UtensiliosCompletos { get; set; }
        public int UtensiliosConFaltantes { get; set; }
        public decimal PorcentajeAvance { get; set; }
        public bool RequiereDescripcionFaltantes { get; set; }
    }

    /// <summary>
    /// ViewModel para el detalle de cada utensilio
    /// </summary>
    public class KardexVajillaDetalleViewModel
    {
        public int Id { get; set; }
        public int UtensilioId { get; set; }
        
        // Información del utensilio
        public string Categoria { get; set; } = string.Empty; // Utensilios Cocina, Menajería Cocina, Equipos
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        
        // Datos del kardex
        public int InventarioInicial { get; set; }
        public int? UnidadesContadas { get; set; }
        public int Diferencia { get; set; }
        public bool TieneFaltantes { get; set; }
        
        public string? Observaciones { get; set; }
        public int Orden { get; set; }
        
        // Estado de completitud
        public bool EstaCompleto { get; set; }
    }

    /// <summary>
    /// ViewModel para autoguardado
    /// </summary>
    public class AutoguardadoKardexVajillaRequest
    {
        public int KardexId { get; set; }
        public int DetalleId { get; set; }
        public int? Valor { get; set; }
    }
}