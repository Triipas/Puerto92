using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;

namespace Puerto92.Services
{
    public class KardexService : IKardexService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KardexService> _logger;

        public KardexService(
            ApplicationDbContext context,
            ILogger<KardexService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> TieneAsignacionActivaAsync(string usuarioId)
        {
            var hoy = DateTime.Today;

            return await _context.AsignacionesKardex
                .AnyAsync(a => a.EmpleadoId == usuarioId &&
                              a.Fecha.Date == hoy &&
                              (a.Estado == EstadoAsignacion.Asignada || a.Estado == EstadoAsignacion.EnProceso));
        }

        public async Task<AsignacionKardex?> ObtenerAsignacionActivaAsync(string usuarioId)
        {
            var hoy = DateTime.Today;

            return await _context.AsignacionesKardex
                .Include(a => a.Local)
                .FirstOrDefaultAsync(a => a.EmpleadoId == usuarioId &&
                                         a.Fecha.Date == hoy &&
                                         (a.Estado == EstadoAsignacion.Asignada || a.Estado == EstadoAsignacion.EnProceso));
        }

        public async Task<MiKardexViewModel> ObtenerMiKardexAsync(string usuarioId)
        {
            var asignacion = await ObtenerAsignacionActivaAsync(usuarioId);

            var viewModel = new MiKardexViewModel
            {
                TieneAsignacionActiva = asignacion != null,
                AsignacionActiva = asignacion
            };

            if (asignacion == null)
            {
                viewModel.MensajeInformativo = "No tienes ninguna asignación de kardex para hoy.";
                viewModel.PuedeIniciarRegistro = false;
                return viewModel;
            }

            viewModel.TipoKardex = asignacion.TipoKardex;
            viewModel.FechaAsignada = asignacion.Fecha;

            // ⭐ IDENTIFICAR EL TIPO DE KARDEX Y VERIFICAR SI EXISTE BORRADOR
            switch (asignacion.TipoKardex)
            {
                case TipoKardex.MozoBebidas:
                    await VerificarBorradorBebidas(viewModel, asignacion.Id);
                    break;

                case TipoKardex.MozoSalon:
                    // TODO: Implementar cuando se cree el kardex de salón
                    viewModel.MensajeInformativo = "El kardex de Mozo Salón estará disponible próximamente.";
                    viewModel.PuedeIniciarRegistro = false;
                    break;

                case TipoKardex.CocinaFria:
                case TipoKardex.CocinaCaliente:
                case TipoKardex.Parrilla:
                    // TODO: Implementar cuando se cree el kardex de cocina
                    viewModel.MensajeInformativo = "El kardex de Cocina estará disponible próximamente.";
                    viewModel.PuedeIniciarRegistro = false;
                    break;

                case TipoKardex.Vajilla:
                    // TODO: Implementar cuando se cree el kardex de vajilla
                    viewModel.MensajeInformativo = "El kardex de Vajilla estará disponible próximamente.";
                    viewModel.PuedeIniciarRegistro = false;
                    break;

                default:
                    viewModel.MensajeInformativo = "Tipo de kardex no reconocido.";
                    viewModel.PuedeIniciarRegistro = false;
                    break;
            }

            return viewModel;
        }

        /// <summary>
        /// Verificar si existe borrador de kardex de bebidas
        /// </summary>
        private async Task VerificarBorradorBebidas(MiKardexViewModel viewModel, int asignacionId)
        {
            var kardexBorrador = await _context.KardexBebidas
                .FirstOrDefaultAsync(k => k.AsignacionId == asignacionId &&
                                         k.Estado == EstadoKardex.Borrador);

            if (kardexBorrador != null)
            {
                viewModel.ExisteKardexBorrador = true;
                viewModel.KardexBorradorId = kardexBorrador.Id;

                // Calcular porcentaje de avance
                var totalDetalles = await _context.KardexBebidasDetalles
                    .CountAsync(d => d.KardexBebidasId == kardexBorrador.Id);

                var detallesCompletos = await _context.KardexBebidasDetalles
                    .CountAsync(d => d.KardexBebidasId == kardexBorrador.Id &&
                                    d.ConteoAlmacen.HasValue &&
                                    d.ConteoRefri1.HasValue &&
                                    d.ConteoRefri2.HasValue &&
                                    d.ConteoRefri3.HasValue);

                viewModel.PorcentajeAvanceBorrador = totalDetalles > 0
                    ? (decimal)detallesCompletos / totalDetalles * 100
                    : 0;
            }

            viewModel.PuedeIniciarRegistro = true;
        }

        // Cambiar nombre del método para ser más específico
        public async Task<bool> AutoguardarDetalleBebidasAsync(AutoguardadoKardexRequest request)
        {
            // Mismo código que AutoguardarDetalleAsync
            try
            {
                var detalle = await _context.KardexBebidasDetalles
                    .FirstOrDefaultAsync(d => d.Id == request.DetalleId &&
                                             d.KardexBebidasId == request.KardexId);

                if (detalle == null)
                {
                    _logger.LogWarning($"Detalle no encontrado: {request.DetalleId}");
                    return false;
                }

                // Actualizar el campo correspondiente
                switch (request.Campo)
                {
                    case "ConteoAlmacen":
                        detalle.ConteoAlmacen = request.Valor;
                        break;
                    case "ConteoRefri1":
                        detalle.ConteoRefri1 = request.Valor;
                        break;
                    case "ConteoRefri2":
                        detalle.ConteoRefri2 = request.Valor;
                        break;
                    case "ConteoRefri3":
                        detalle.ConteoRefri3 = request.Valor;
                        break;
                    default:
                        _logger.LogWarning($"Campo no reconocido: {request.Campo}");
                        return false;
                }

                // Recalcular conteo final y ventas
                RecalcularDetalle(detalle);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Autoguardado exitoso: Detalle {request.DetalleId}, Campo {request.Campo}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en autoguardado: {ex.Message}");
                return false;
            }
        }

        public async Task<KardexBebidasViewModel> CalcularYActualizarBebidasAsync(int kardexId)
        {
            var kardex = await _context.KardexBebidas
                .Include(k => k.Detalles)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            foreach (var detalle in kardex.Detalles)
            {
                RecalcularDetalle(detalle);
            }

            await _context.SaveChangesAsync();

            return await ObtenerKardexBebidasAsync(kardexId);
        }

        public async Task<KardexBebidasViewModel> IniciarKardexBebidasAsync(int asignacionId, string usuarioId)
        {
            var asignacion = await _context.AsignacionesKardex
                .Include(a => a.Local)
                .Include(a => a.Empleado)
                .FirstOrDefaultAsync(a => a.Id == asignacionId && a.EmpleadoId == usuarioId);

            if (asignacion == null)
            {
                throw new Exception("Asignación no encontrada o no autorizada");
            }

            // Verificar si ya existe un kardex para esta asignación
            var kardexExistente = await _context.KardexBebidas
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(k => k.AsignacionId == asignacionId);

            if (kardexExistente != null)
            {
                return await MapearKardexAViewModel(kardexExistente);
            }

            // Crear nuevo kardex
            var kardex = new KardexBebidas
            {
                AsignacionId = asignacionId,
                Fecha = asignacion.Fecha,
                LocalId = asignacion.LocalId,
                EmpleadoId = usuarioId,
                Estado = EstadoKardex.Borrador,
                FechaInicio = DateTime.Now
            };

            _context.KardexBebidas.Add(kardex);
            await _context.SaveChangesAsync();

            // Obtener productos de bebidas activos
            var productosBebidas = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo && p.Categoria!.Tipo == TipoCategoria.Bebidas)
                .OrderBy(p => p.Categoria!.Orden)
                .ThenBy(p => p.Codigo)
                .ToListAsync();

            var orden = 1;
            foreach (var producto in productosBebidas)
            {
                var detalle = new KardexBebidasDetalle
                {
                    KardexBebidasId = kardex.Id,
                    ProductoId = producto.Id,
                    InventarioInicial = 0, // TODO: Obtener del sistema o cierre anterior
                    Ingresos = 0, // TODO: Obtener de compras del día
                    Orden = orden++
                };

                _context.KardexBebidasDetalles.Add(detalle);
            }

            await _context.SaveChangesAsync();

            // Marcar asignación como en proceso
            asignacion.Estado = EstadoAsignacion.EnProceso;
            asignacion.RegistroIniciado = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Kardex de bebidas iniciado: ID {kardex.Id} por usuario {usuarioId}");

            // Cargar kardex completo con detalles
            return await ObtenerKardexBebidasAsync(kardex.Id);
        }

        public async Task<KardexBebidasViewModel> ObtenerKardexBebidasAsync(int kardexId)
        {
            var kardex = await _context.KardexBebidas
                .Include(k => k.Asignacion)
                .Include(k => k.Empleado)
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            return await MapearKardexAViewModel(kardex);
        }

        public async Task<bool> AutoguardarDetalleAsync(AutoguardadoKardexRequest request)
        {
            try
            {
                var detalle = await _context.KardexBebidasDetalles
                    .FirstOrDefaultAsync(d => d.Id == request.DetalleId &&
                                             d.KardexBebidasId == request.KardexId);

                if (detalle == null)
                {
                    _logger.LogWarning($"Detalle no encontrado: {request.DetalleId}");
                    return false;
                }

                // Actualizar el campo correspondiente
                switch (request.Campo)
                {
                    case "ConteoAlmacen":
                        detalle.ConteoAlmacen = request.Valor;
                        break;
                    case "ConteoRefri1":
                        detalle.ConteoRefri1 = request.Valor;
                        break;
                    case "ConteoRefri2":
                        detalle.ConteoRefri2 = request.Valor;
                        break;
                    case "ConteoRefri3":
                        detalle.ConteoRefri3 = request.Valor;
                        break;
                    case "Observaciones":
                        // Para observaciones se maneja diferente
                        break;
                    default:
                        _logger.LogWarning($"Campo no reconocido: {request.Campo}");
                        return false;
                }

                // Recalcular conteo final y ventas
                RecalcularDetalle(detalle);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Autoguardado exitoso: Detalle {request.DetalleId}, Campo {request.Campo}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en autoguardado: {ex.Message}");
                return false;
            }
        }

        public async Task<KardexBebidasViewModel> CalcularYActualizarAsync(int kardexId)
        {
            var kardex = await _context.KardexBebidas
                .Include(k => k.Detalles)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            foreach (var detalle in kardex.Detalles)
            {
                RecalcularDetalle(detalle);
            }

            await _context.SaveChangesAsync();

            return await ObtenerKardexBebidasAsync(kardexId);
        }

        public async Task<bool> CompletarKardexBebidasAsync(int kardexId, string observaciones)
        {
            var kardex = await _context.KardexBebidas
                .Include(k => k.Detalles)
                .Include(k => k.Asignacion)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            // Validar que todos los campos estén completos
            var detallesIncompletos = kardex.Detalles.Where(d =>
                !d.ConteoAlmacen.HasValue ||
                !d.ConteoRefri1.HasValue ||
                !d.ConteoRefri2.HasValue ||
                !d.ConteoRefri3.HasValue
            ).ToList();

            if (detallesIncompletos.Any())
            {
                throw new Exception($"Hay {detallesIncompletos.Count} producto(s) con campos incompletos");
            }

            kardex.Estado = EstadoKardex.Completado;
            kardex.FechaFinalizacion = DateTime.Now;
            kardex.Observaciones = observaciones;

            if (kardex.Asignacion != null)
            {
                kardex.Asignacion.Estado = EstadoAsignacion.Completada;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Kardex de bebidas completado: ID {kardexId}");

            return true;
        }

        // Métodos auxiliares privados

        private void RecalcularDetalle(KardexBebidasDetalle detalle)
        {
            // Calcular conteo final
            detalle.ConteoFinal = (detalle.ConteoAlmacen ?? 0) +
                                 (detalle.ConteoRefri1 ?? 0) +
                                 (detalle.ConteoRefri2 ?? 0) +
                                 (detalle.ConteoRefri3 ?? 0);

            // Calcular ventas
            var stockEsperado = detalle.InventarioInicial + detalle.Ingresos;
            detalle.Ventas = stockEsperado - detalle.ConteoFinal;

            // Calcular diferencia porcentual
            if (stockEsperado > 0)
            {
                detalle.DiferenciaPorcentual = Math.Abs((detalle.Ventas / stockEsperado) * 100);
                detalle.TieneDiferenciaSignificativa = detalle.DiferenciaPorcentual > 10;
            }
            else
            {
                detalle.DiferenciaPorcentual = null;
                detalle.TieneDiferenciaSignificativa = false;
            }
        }

        private async Task<KardexBebidasViewModel> MapearKardexAViewModel(KardexBebidas kardex)
        {
            var detalles = kardex.Detalles
                .OrderBy(d => d.Orden)
                .Select(d => new KardexBebidasDetalleViewModel
                {
                    Id = d.Id,
                    ProductoId = d.ProductoId,
                    Categoria = d.Producto?.Categoria?.Nombre ?? "",
                    Codigo = d.Producto?.Codigo ?? "",
                    Descripcion = d.Producto?.Nombre ?? "",
                    Unidad = d.Producto?.Unidad ?? "",
                    InventarioInicial = d.InventarioInicial,
                    Ingresos = d.Ingresos,
                    ConteoAlmacen = d.ConteoAlmacen,
                    ConteoRefri1 = d.ConteoRefri1,
                    ConteoRefri2 = d.ConteoRefri2,
                    ConteoRefri3 = d.ConteoRefri3,
                    ConteoFinal = d.ConteoFinal,
                    Ventas = d.Ventas,
                    DiferenciaPorcentual = d.DiferenciaPorcentual,
                    TieneDiferenciaSignificativa = d.TieneDiferenciaSignificativa,
                    Observaciones = d.Observaciones,
                    Orden = d.Orden,
                    EstaCompleto = d.ConteoAlmacen.HasValue &&
                                  d.ConteoRefri1.HasValue &&
                                  d.ConteoRefri2.HasValue &&
                                  d.ConteoRefri3.HasValue
                })
                .ToList();

            var totalProductos = detalles.Count;
            var productosCompletos = detalles.Count(d => d.EstaCompleto);
            var productosConDiferencia = detalles.Count(d => d.TieneDiferenciaSignificativa);

            return new KardexBebidasViewModel
            {
                Id = kardex.Id,
                AsignacionId = kardex.AsignacionId,
                Fecha = kardex.Fecha,
                LocalId = kardex.LocalId,
                EmpleadoId = kardex.EmpleadoId,
                EmpleadoNombre = kardex.Empleado?.NombreCompleto ?? "",
                Estado = kardex.Estado,
                FechaInicio = kardex.FechaInicio,
                FechaFinalizacion = kardex.FechaFinalizacion,
                FechaEnvio = kardex.FechaEnvio,
                Observaciones = kardex.Observaciones,
                Detalles = detalles,
                TotalProductos = totalProductos,
                ProductosCompletos = productosCompletos,
                ProductosConDiferencia = productosConDiferencia,
                PorcentajeAvance = totalProductos > 0
                    ? (decimal)productosCompletos / totalProductos * 100
                    : 0
            };
        }
    }
}