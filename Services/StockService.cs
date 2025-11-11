using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;

namespace Puerto92.Services
{
    /// <summary>
    /// Servicio para gestión de stock e inventario
    /// </summary>
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockService> _logger;

        public StockService(
            ApplicationDbContext context,
            ILogger<StockService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // STOCK DE PRODUCTOS
        // ==========================================

        public async Task<StockProducto> ObtenerStockProductoAsync(int productoId, int localId)
        {
            var stock = await _context.StockProductos
                .Include(s => s.Producto)
                .FirstOrDefaultAsync(s => s.ProductoId == productoId && s.LocalId == localId);

            if (stock == null)
            {
                // Crear stock inicial si no existe
                stock = new StockProducto
                {
                    ProductoId = productoId,
                    LocalId = localId,
                    CantidadActual = 0,
                    FechaUltimaActualizacion = DateTime.Now
                };

                _context.StockProductos.Add(stock);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"📦 Stock inicial creado para Producto {productoId} en Local {localId}");
            }

            return stock;
        }

        public async Task<StockProducto> ActualizarStockProductoAsync(
            int productoId,
            int localId,
            decimal nuevaCantidad,
            int kardexId,
            string kardexTipo,
            string? observaciones = null)
        {
            var stock = await ObtenerStockProductoAsync(productoId, localId);
            var cantidadAnterior = stock.CantidadActual;

            stock.CantidadActual = nuevaCantidad;
            stock.FechaUltimaActualizacion = DateTime.Now;
            stock.UltimoKardexId = kardexId;
            stock.UltimoKardexTipo = kardexTipo;

            // Registrar en historial
            var historial = new HistorialStock
            {
                TipoItem = "Producto",
                ItemId = productoId,
                LocalId = localId,
                CantidadAnterior = cantidadAnterior,
                CantidadNueva = nuevaCantidad,
                Diferencia = nuevaCantidad - cantidadAnterior,
                TipoMovimiento = "Aprobacion Kardex",
                KardexId = kardexId,
                KardexTipo = kardexTipo,
                FechaHora = DateTime.Now,
                Observaciones = observaciones
            };

            _context.HistorialStock.Add(historial);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Stock actualizado: Producto {productoId} | Anterior: {cantidadAnterior} → Nuevo: {nuevaCantidad}");

            return stock;
        }

        public async Task<List<StockProducto>> ActualizarStockDesdeKardexBebidasAsync(int kardexId)
        {
            _logger.LogInformation($"🔄 Actualizando stock desde Kardex de Bebidas {kardexId}...");

            var kardex = await _context.KardexBebidas
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception($"Kardex de Bebidas {kardexId} no encontrado");
            }

            var stocksActualizados = new List<StockProducto>();

            foreach (var detalle in kardex.Detalles)
            {
                // El stock final es el conteo total
                var stockFinal = detalle.ConteoFinal;

                var stock = await ActualizarStockProductoAsync(
                    productoId: detalle.ProductoId,
                    localId: kardex.LocalId,
                    nuevaCantidad: stockFinal,
                    kardexId: kardexId,
                    kardexTipo: TipoKardex.MozoBebidas,
                    observaciones: $"Aprobación de Kardex de Bebidas - {kardex.Fecha:dd/MM/yyyy}"
                );

                stocksActualizados.Add(stock);
            }

            _logger.LogInformation($"✅ Stock actualizado desde Kardex de Bebidas: {stocksActualizados.Count} producto(s)");

            return stocksActualizados;
        }

        public async Task<List<StockProducto>> ActualizarStockDesdeKardexCocinaAsync(int kardexId)
        {
            _logger.LogInformation($"🔄 Actualizando stock desde Kardex de Cocina {kardexId}...");

            var kardex = await _context.KardexCocina
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception($"Kardex de Cocina {kardexId} no encontrado");
            }

            var stocksActualizados = new List<StockProducto>();

            foreach (var detalle in kardex.Detalles.Where(d => d.StockFinal.HasValue))
            {
                var stock = await ActualizarStockProductoAsync(
                    productoId: detalle.ProductoId,
                    localId: kardex.LocalId,
                    nuevaCantidad: detalle.StockFinal!.Value,
                    kardexId: kardexId,
                    kardexTipo: kardex.TipoCocina,
                    observaciones: $"Aprobación de Kardex de {kardex.TipoCocina} - {kardex.Fecha:dd/MM/yyyy}"
                );

                stocksActualizados.Add(stock);
            }

            _logger.LogInformation($"✅ Stock actualizado desde Kardex de Cocina: {stocksActualizados.Count} producto(s)");

            return stocksActualizados;
        }

        // ==========================================
        // STOCK DE UTENSILIOS
        // ==========================================

        public async Task<StockUtensilio> ObtenerStockUtensilioAsync(int utensilioId, int localId)
        {
            var stock = await _context.StockUtensilios
                .Include(s => s.Utensilio)
                .FirstOrDefaultAsync(s => s.UtensilioId == utensilioId && s.LocalId == localId);

            if (stock == null)
            {
                // Crear stock inicial si no existe
                stock = new StockUtensilio
                {
                    UtensilioId = utensilioId,
                    LocalId = localId,
                    CantidadActual = 0,
                    FechaUltimaActualizacion = DateTime.Now
                };

                _context.StockUtensilios.Add(stock);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"📦 Stock inicial creado para Utensilio {utensilioId} en Local {localId}");
            }

            return stock;
        }

        public async Task<StockUtensilio> ActualizarStockUtensilioAsync(
            int utensilioId,
            int localId,
            int nuevaCantidad,
            int kardexId,
            string kardexTipo,
            string? observaciones = null)
        {
            var stock = await ObtenerStockUtensilioAsync(utensilioId, localId);
            var cantidadAnterior = stock.CantidadActual;

            stock.CantidadActual = nuevaCantidad;
            stock.FechaUltimaActualizacion = DateTime.Now;
            stock.UltimoKardexId = kardexId;
            stock.UltimoKardexTipo = kardexTipo;

            // Registrar en historial
            var historial = new HistorialStock
            {
                TipoItem = "Utensilio",
                ItemId = utensilioId,
                LocalId = localId,
                CantidadAnterior = cantidadAnterior,
                CantidadNueva = nuevaCantidad,
                Diferencia = nuevaCantidad - cantidadAnterior,
                TipoMovimiento = "Aprobacion Kardex",
                KardexId = kardexId,
                KardexTipo = kardexTipo,
                FechaHora = DateTime.Now,
                Observaciones = observaciones
            };

            _context.HistorialStock.Add(historial);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Stock actualizado: Utensilio {utensilioId} | Anterior: {cantidadAnterior} → Nuevo: {nuevaCantidad}");

            return stock;
        }

        public async Task<List<StockUtensilio>> ActualizarStockDesdeKardexSalonAsync(int kardexId)
        {
            _logger.LogInformation($"🔄 Actualizando stock desde Kardex de Salón {kardexId}...");

            var kardex = await _context.KardexSalon
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Utensilio)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception($"Kardex de Salón {kardexId} no encontrado");
            }

            var stocksActualizados = new List<StockUtensilio>();

            foreach (var detalle in kardex.Detalles.Where(d => d.UnidadesContadas.HasValue))
            {
                var stock = await ActualizarStockUtensilioAsync(
                    utensilioId: detalle.UtensilioId,
                    localId: kardex.LocalId,
                    nuevaCantidad: detalle.UnidadesContadas!.Value,
                    kardexId: kardexId,
                    kardexTipo: TipoKardex.MozoSalon,
                    observaciones: $"Aprobación de Kardex de Salón - {kardex.Fecha:dd/MM/yyyy}"
                );

                stocksActualizados.Add(stock);
            }

            _logger.LogInformation($"✅ Stock actualizado desde Kardex de Salón: {stocksActualizados.Count} utensilio(s)");

            return stocksActualizados;
        }

        public async Task<List<StockUtensilio>> ActualizarStockDesdeKardexVajillaAsync(int kardexId)
        {
            _logger.LogInformation($"🔄 Actualizando stock desde Kardex de Vajilla {kardexId}...");

            var kardex = await _context.KardexVajilla
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Utensilio)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception($"Kardex de Vajilla {kardexId} no encontrado");
            }

            var stocksActualizados = new List<StockUtensilio>();

            foreach (var detalle in kardex.Detalles.Where(d => d.UnidadesContadas.HasValue))
            {
                var stock = await ActualizarStockUtensilioAsync(
                    utensilioId: detalle.UtensilioId,
                    localId: kardex.LocalId,
                    nuevaCantidad: detalle.UnidadesContadas!.Value,
                    kardexId: kardexId,
                    kardexTipo: TipoKardex.Vajilla,
                    observaciones: $"Aprobación de Kardex de Vajilla - {kardex.Fecha:dd/MM/yyyy}"
                );

                stocksActualizados.Add(stock);
            }

            _logger.LogInformation($"✅ Stock actualizado desde Kardex de Vajilla: {stocksActualizados.Count} utensilio(s)");

            return stocksActualizados;
        }

        // ==========================================
        // CONSULTAS
        // ==========================================

        public async Task<List<StockProducto>> ObtenerTodoElStockProductosAsync(int localId)
        {
            return await _context.StockProductos
                .Include(s => s.Producto)
                    .ThenInclude(p => p.Categoria)
                .Where(s => s.LocalId == localId)
                .OrderBy(s => s.Producto!.Categoria!.Orden)
                .ThenBy(s => s.Producto!.Codigo)
                .ToListAsync();
        }

        public async Task<List<StockUtensilio>> ObtenerTodoElStockUtensiliosAsync(int localId)
        {
            return await _context.StockUtensilios
                .Include(s => s.Utensilio)
                    .ThenInclude(u => u.Categoria)
                .Where(s => s.LocalId == localId)
                .OrderBy(s => s.Utensilio!.Codigo)
                .ToListAsync();
        }

        public async Task<List<HistorialStock>> ObtenerHistorialStockAsync(int localId, DateTime fechaDesde, DateTime fechaHasta)
        {
            return await _context.HistorialStock
                .Where(h => h.LocalId == localId &&
                           h.FechaHora >= fechaDesde &&
                           h.FechaHora <= fechaHasta)
                .OrderByDescending(h => h.FechaHora)
                .ToListAsync();
        }
    }
}