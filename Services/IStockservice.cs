using Puerto92.Models;

namespace Puerto92.Services
{
    /// <summary>
    /// Interfaz para el servicio de gestión de stock
    /// </summary>
    public interface IStockService
    {
        // Stock de Productos
        Task<StockProducto> ObtenerStockProductoAsync(int productoId, int localId);
        Task<StockProducto> ActualizarStockProductoAsync(int productoId, int localId, decimal nuevaCantidad, int kardexId, string kardexTipo, string? observaciones = null);
        Task<List<StockProducto>> ActualizarStockDesdeKardexBebidasAsync(int kardexId);
        Task<List<StockProducto>> ActualizarStockDesdeKardexCocinaAsync(int kardexId);
        
        // Stock de Utensilios
        Task<StockUtensilio> ObtenerStockUtensilioAsync(int utensilioId, int localId);
        Task<StockUtensilio> ActualizarStockUtensilioAsync(int utensilioId, int localId, int nuevaCantidad, int kardexId, string kardexTipo, string? observaciones = null);
        Task<List<StockUtensilio>> ActualizarStockDesdeKardexSalonAsync(int kardexId);
        Task<List<StockUtensilio>> ActualizarStockDesdeKardexVajillaAsync(int kardexId);
        
        // Consultas
        Task<List<StockProducto>> ObtenerTodoElStockProductosAsync(int localId);
        Task<List<StockUtensilio>> ObtenerTodoElStockUtensiliosAsync(int localId);
        Task<List<HistorialStock>> ObtenerHistorialStockAsync(int localId, DateTime fechaDesde, DateTime fechaHasta);
    }
}