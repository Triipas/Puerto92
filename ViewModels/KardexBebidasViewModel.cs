using System.ComponentModel.DataAnnotations;
using Puerto92.Models;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para el kardex de bebidas con conteo por ubicación
    /// </summary>
    public class KardexBebidasViewModel
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
        
        public List<KardexBebidasDetalleViewModel> Detalles { get; set; } = new();
        
        // Información adicional
        public int TotalProductos { get; set; }
        public int ProductosCompletos { get; set; }
        public int ProductosConDiferencia { get; set; }
        public decimal PorcentajeAvance { get; set; }
    }

    /// <summary>
    /// ViewModel para el detalle de cada producto
    /// </summary>
    public class KardexBebidasDetalleViewModel
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        
        // Información del producto
        public string Categoria { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        
        // Datos del kardex
        public decimal InventarioInicial { get; set; }
        public decimal Ingresos { get; set; }
        
        // Conteos por ubicación
        public decimal? ConteoAlmacen { get; set; }
        public decimal? ConteoRefri1 { get; set; }
        public decimal? ConteoRefri2 { get; set; }
        public decimal? ConteoRefri3 { get; set; }
        
        // Calculados
        public decimal ConteoFinal { get; set; }
        public decimal Ventas { get; set; }
        public decimal? DiferenciaPorcentual { get; set; }
        public bool TieneDiferenciaSignificativa { get; set; }
        
        public string? Observaciones { get; set; }
        public int Orden { get; set; }
        
        // Estado de completitud
        public bool EstaCompleto { get; set; }
    }

    /// <summary>
    /// ViewModel para autoguardado
    /// </summary>
    public class AutoguardadoKardexRequest
    {
        public int KardexId { get; set; }
        public int DetalleId { get; set; }
        public string Campo { get; set; } = string.Empty; // "ConteoAlmacen", "ConteoRefri1", etc.
        public decimal? Valor { get; set; }
    }

    /// <summary>
    /// ViewModel para mi kardex (vista general)
    /// </summary>
    public class MiKardexViewModel
    {
        public bool TieneAsignacionActiva { get; set; }
        public AsignacionKardex? AsignacionActiva { get; set; }
        public string TipoKardex { get; set; } = string.Empty;
        public DateTime FechaAsignada { get; set; }
        
        public bool ExisteKardexBorrador { get; set; }
        public int? KardexBorradorId { get; set; }
        public decimal? PorcentajeAvanceBorrador { get; set; }
        
        public string? MensajeInformativo { get; set; }
        public bool PuedeIniciarRegistro { get; set; }
    }
}