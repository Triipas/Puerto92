using System.ComponentModel.DataAnnotations;

namespace Puerto92.ViewModels
{
    // ==========================================
    // VIEWMODELS PARA REVISIÓN DE KARDEX
    // ==========================================
    /// <summary>
    /// ViewModel para la revisión consolidada de kardex de cocina (3 cocineros)
    /// </summary>
    public class KardexCocinaConsolidadoViewModel
    {
        public DateTime Fecha { get; set; }
        public int LocalId { get; set; }
        public string LocalNombre { get; set; } = string.Empty;
        
        // Los 3 kardex individuales
        public KardexCocinaIndividualDto? KardexCocinaFria { get; set; }
        public KardexCocinaIndividualDto? KardexCocinaCaliente { get; set; }
        public KardexCocinaIndividualDto? KardexParrilla { get; set; }
        
        // Categorías consolidadas
        public List<CategoriaCocinaConsolidadaViewModel> CategoriasConsolidadas { get; set; } = new();
        
        // Personal presente consolidado
        public List<EmpleadoPresenteDto> PersonalPresenteTotal { get; set; } = new();
        
        // Estadísticas
        public int TotalProductos { get; set; }
        public int ProductosConDiferencia { get; set; }
        public decimal PorcentajeProductosConDiferencia { get; set; }
    }

    /// <summary>
    /// Datos de un kardex individual de cocina
    /// </summary>
    public class KardexCocinaIndividualDto
    {
        public int Id { get; set; }
        public string TipoCocina { get; set; } = string.Empty;
        public string EmpleadoId { get; set; } = string.Empty;
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaEnvio { get; set; }
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// Categoría consolidada con productos de los 3 cocineros
    /// </summary>
    public class CategoriaCocinaConsolidadaViewModel
    {
        public string NombreCategoria { get; set; } = string.Empty;
        public bool EsEspecial { get; set; }
        public bool Expandida { get; set; } = true;
        
        /// <summary>
        /// Para categorías especiales, indica qué tipo de cocina es responsable
        /// </summary>
        public string? TipoCocinaResponsable { get; set; }
        
        public List<ProductoCocinaConsolidadoViewModel> Productos { get; set; } = new();
        
        // Estadísticas
        public int TotalProductos => Productos.Count;
        public int ProductosConDiferencia => Productos.Count(p => p.TieneDiferenciaSignificativa);
    }

    /// <summary>
    /// Producto consolidado con conteos de múltiples cocineros
    /// </summary>
    public class ProductoCocinaConsolidadoViewModel
    {
        public int ProductoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal CantidadAPedir { get; set; }
        public decimal Ingresos { get; set; }
        
        /// <summary>
        /// Conteos de los 3 cocineros (solo para categorías compartidas)
        /// </summary>
        public decimal? StockFinalCocinaFria { get; set; }
        public decimal? StockFinalCocinaCaliente { get; set; }
        public decimal? StockFinalParrilla { get; set; }
        
        /// <summary>
        /// Promedio calculado para categorías compartidas
        /// </summary>
        public decimal? StockFinalPromedio { get; set; }
        
        /// <summary>
        /// Stock final para categorías específicas (solo 1 cocinero)
        /// </summary>
        public decimal? StockFinalEspecifico { get; set; }
        
        /// <summary>
        /// Diferencia entre promedio y cantidad esperada
        /// </summary>
        public decimal Diferencia { get; set; }
        
        /// <summary>
        /// Porcentaje de diferencia
        /// </summary>
        public decimal? DiferenciaPorcentual { get; set; }
        
        /// <summary>
        /// Si tiene diferencia mayor al 10%
        /// </summary>
        public bool TieneDiferenciaSignificativa { get; set; }
        
        public string? Observaciones { get; set; }
        public int Orden { get; set; }
    }

    /// <summary>
    /// ViewModel para revisión de kardex de Salón
    /// </summary>
    public class KardexSalonRevisionViewModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int LocalId { get; set; }
        public string EmpleadoId { get; set; } = string.Empty;
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        
        public DateTime? FechaEnvio { get; set; }
        public string? DescripcionFaltantes { get; set; }
        public string? Observaciones { get; set; }
        
        public List<KardexSalonDetalleViewModel> Detalles { get; set; } = new();
        public List<EmpleadoPresenteDto> PersonalPresente { get; set; } = new();
        
        // Estadísticas
        public int TotalUtensilios { get; set; }
        public int UtensiliosConFaltantes { get; set; }
        public int TotalFaltantes { get; set; }
    }

    /// <summary>
    /// ViewModel para revisión de kardex de Bebidas
    /// </summary>
    public class KardexBebidasRevisionViewModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int LocalId { get; set; }
        public string EmpleadoId { get; set; } = string.Empty;
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        
        public DateTime? FechaEnvio { get; set; }
        public string? Observaciones { get; set; }
        
        public List<KardexBebidasDetalleViewModel> Detalles { get; set; } = new();
        public List<EmpleadoPresenteDto> PersonalPresente { get; set; } = new();
        
        // Estadísticas
        public int TotalProductos { get; set; }
        public int ProductosConDiferencia { get; set; }
    }

    /// <summary>
    /// ViewModel para revisión de kardex de Vajilla
    /// </summary>
    public class KardexVajillaRevisionViewModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int LocalId { get; set; }
        public string EmpleadoId { get; set; } = string.Empty;
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        
        public DateTime? FechaEnvio { get; set; }
        public string? DescripcionFaltantes { get; set; }
        public int? CantidadRotos { get; set; }
        public int? CantidadExtraviados { get; set; }
        public string? Observaciones { get; set; }
        
        public List<KardexVajillaDetalleViewModel> Detalles { get; set; } = new();
        public List<EmpleadoPresenteDto> PersonalPresente { get; set; } = new();
        
        // Estadísticas
        public int TotalUtensilios { get; set; }
        public int UtensiliosConFaltantes { get; set; }
        public int TotalFaltantes { get; set; }
    }

    /// <summary>
    /// DTO para empleados presentes
    /// </summary>
    public class EmpleadoPresenteDto
    {
        public string EmpleadoId { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool EsResponsablePrincipal { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    /// <summary>
    /// Request para aprobar/rechazar kardex
    /// </summary>
    public class AprobarRechazarKardexRequest
    {
        public int KardexId { get; set; }
        public string TipoKardex { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty; // "Aprobar" o "Rechazar"
        public string? MotivoRechazo { get; set; }
        public string? ObservacionesRevision { get; set; }
        
        /// <summary>
        /// Para kardex de cocina consolidado
        /// </summary>
        public List<int>? KardexIdsConsolidados { get; set; }
    }
}
