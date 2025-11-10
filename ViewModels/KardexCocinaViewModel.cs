using System.ComponentModel.DataAnnotations;
using Puerto92.Models;

namespace Puerto92.ViewModels
{
    /// <summary>
    /// ViewModel para el kardex de cocina
    /// </summary>
    public class KardexCocinaViewModel
    {
        public int Id { get; set; }
        public int AsignacionId { get; set; }
        public DateTime Fecha { get; set; }
        public int LocalId { get; set; }
        public string EmpleadoId { get; set; } = string.Empty;
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string TipoCocina { get; set; } = string.Empty;
        public string Estado { get; set; } = "Borrador";
        
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        
        public string? Observaciones { get; set; }
        
        /// <summary>
        /// Detalles agrupados por categoría
        /// </summary>
        public List<KardexCocinaCategoriaViewModel> Categorias { get; set; } = new();
        
        // Información adicional
        public int TotalProductos { get; set; }
        public int ProductosCompletos { get; set; }
        public decimal PorcentajeAvance { get; set; }
    }

    /// <summary>
    /// ViewModel para categoría con sus productos
    /// </summary>
    public class KardexCocinaCategoriaViewModel
    {
        public string NombreCategoria { get; set; } = string.Empty;
        public bool EsEspecial { get; set; }
        public bool Expandida { get; set; } = true; // Por defecto expandidas
        public List<KardexCocinaDetalleViewModel> Productos { get; set; } = new();
        public int TotalProductos => Productos.Count;
        public int ProductosCompletos => Productos.Count(p => p.EstaCompleto);
    }

    /// <summary>
    /// ViewModel para el detalle de cada producto
    /// </summary>
    public class KardexCocinaDetalleViewModel
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        
        // Información del producto
        public string Categoria { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        
        // Datos del kardex
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal CantidadAPedir { get; set; }
        public decimal Ingresos { get; set; }
        public decimal? StockFinal { get; set; }
        
        public string? Observaciones { get; set; }
        public int Orden { get; set; }
        
        // Estado de completitud
        public bool EstaCompleto => StockFinal.HasValue;
        public bool PermiteDecimales => UnidadMedidaCocina.PermiteDecimales(UnidadMedida);
    }

    /// <summary>
    /// ViewModel para autoguardado
    /// </summary>
    public class AutoguardadoKardexCocinaRequest
    {
        public int KardexId { get; set; }
        public int DetalleId { get; set; }
        public string? Campo { get; set; } // "StockFinal", "UnidadMedida"
        public decimal? ValorNumerico { get; set; }
        public string? ValorTexto { get; set; }
    }
}