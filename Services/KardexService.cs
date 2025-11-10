using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<Usuario> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public KardexService(
            ApplicationDbContext context,
            ILogger<KardexService> logger,
            UserManager<Usuario> userManager,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _notificationService = notificationService;
            _auditService = auditService;
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
                    await VerificarBorradorSalon(viewModel, asignacion.Id);
                    break;

                case TipoKardex.CocinaFria:
                case TipoKardex.CocinaCaliente:
                case TipoKardex.Parrilla:
                    await VerificarBorradorCocina(viewModel, asignacion.Id);
                    break;

                case TipoKardex.Vajilla:
                    await VerificarBorradorVajilla(viewModel, asignacion.Id);
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

        /// <summary>
        /// ⭐ NUEVO: Verificar si existe borrador de kardex de salón
        /// </summary>
        private async Task VerificarBorradorSalon(MiKardexViewModel viewModel, int asignacionId)
        {
            var kardexBorrador = await _context.KardexSalon
                .FirstOrDefaultAsync(k => k.AsignacionId == asignacionId &&
                                        k.Estado == EstadoKardex.Borrador);

            if (kardexBorrador != null)
            {
                viewModel.ExisteKardexBorrador = true;
                viewModel.KardexBorradorId = kardexBorrador.Id;

                // Calcular porcentaje de avance
                var totalDetalles = await _context.KardexSalonDetalle
                    .CountAsync(d => d.KardexSalonId == kardexBorrador.Id);

                var detallesCompletos = await _context.KardexSalonDetalle
                    .CountAsync(d => d.KardexSalonId == kardexBorrador.Id &&
                                    d.UnidadesContadas.HasValue);

                viewModel.PorcentajeAvanceBorrador = totalDetalles > 0
                    ? (decimal)detallesCompletos / totalDetalles * 100
                    : 0;
            }

            viewModel.PuedeIniciarRegistro = true;
        }

        private async Task VerificarBorradorCocina(MiKardexViewModel viewModel, int asignacionId)
        {
            var kardexBorrador = await _context.KardexCocina
                .FirstOrDefaultAsync(k => k.AsignacionId == asignacionId &&
                                        k.Estado == EstadoKardex.Borrador);

            if (kardexBorrador != null)
            {
                viewModel.ExisteKardexBorrador = true;
                viewModel.KardexBorradorId = kardexBorrador.Id;

                // Calcular porcentaje de avance
                var totalDetalles = await _context.Set<KardexCocinaDetalle>()
                    .CountAsync(d => d.KardexCocinaId == kardexBorrador.Id);

                var detallesCompletos = await _context.Set<KardexCocinaDetalle>()
                    .CountAsync(d => d.KardexCocinaId == kardexBorrador.Id &&
                                    d.StockFinal.HasValue);

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

            // ⭐ NUEVO: Registrar en auditoría
            await _auditService.RegistrarInicioKardexAsync(
                tipoKardex: TipoKardex.MozoBebidas,
                fecha: asignacion.Fecha,
                empleadoNombre: asignacion.Empleado?.NombreCompleto ?? "Desconocido",
                kardexId: kardex.Id
            );

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

        public async Task<PersonalPresenteViewModel> ObtenerPersonalPresenteAsync(int kardexId, string tipoKardex)
        {
            var viewModel = new PersonalPresenteViewModel
            {
                KardexId = kardexId,
                TipoKardex = tipoKardex
            };

            // Obtener información del kardex según el tipo
            if (tipoKardex == TipoKardex.MozoBebidas)
            {
                var kardex = await _context.KardexBebidas
                    .Include(k => k.Empleado)
                    .Include(k => k.Local)
                    .FirstOrDefaultAsync(k => k.Id == kardexId);

                if (kardex == null)
                {
                    throw new Exception("Kardex no encontrado");
                }

                viewModel.Fecha = kardex.Fecha;
                viewModel.LocalId = kardex.LocalId;
                viewModel.EmpleadoResponsableId = kardex.EmpleadoId;
                viewModel.EmpleadoResponsableNombre = kardex.Empleado?.NombreCompleto ?? "";
            }
            else if (tipoKardex == TipoKardex.MozoSalon)
            {
                var kardex = await _context.KardexSalon
                    .Include(k => k.Empleado)
                    .Include(k => k.Local)
                    .FirstOrDefaultAsync(k => k.Id == kardexId);

                if (kardex == null)
                {
                    throw new Exception("Kardex no encontrado");
                }

                viewModel.Fecha = kardex.Fecha;
                viewModel.LocalId = kardex.LocalId;
                viewModel.EmpleadoResponsableId = kardex.EmpleadoId;
                viewModel.EmpleadoResponsableNombre = kardex.Empleado?.NombreCompleto ?? "";
            }
            // ⭐ NUEVO: Soporte para Cocina Fría, Caliente y Parrilla
            else if (tipoKardex == TipoKardex.CocinaFria ||
                    tipoKardex == TipoKardex.CocinaCaliente ||
                    tipoKardex == TipoKardex.Parrilla)
            {
                var kardex = await _context.KardexCocina
                    .Include(k => k.Empleado)
                    .Include(k => k.Local)
                    .FirstOrDefaultAsync(k => k.Id == kardexId);

                if (kardex == null)
                {
                    _logger.LogError($"❌ Kardex de Cocina no encontrado: {kardexId}");
                    throw new Exception("Kardex no encontrado");
                }

                _logger.LogInformation($"✅ Kardex de Cocina encontrado: ID {kardex.Id}, LocalId: {kardex.LocalId}");

                // ⭐ VALIDACIÓN CRÍTICA
                if (kardex.LocalId == 0)
                {
                    _logger.LogError($"❌ ERROR: Kardex {kardexId} tiene LocalId = 0");
                    throw new Exception("Error: El kardex no tiene un Local válido. Contacte al administrador.");
                }

                viewModel.Fecha = kardex.Fecha;
                viewModel.LocalId = kardex.LocalId;
                viewModel.EmpleadoResponsableId = kardex.EmpleadoId;
                viewModel.EmpleadoResponsableNombre = kardex.Empleado?.NombreCompleto ?? "";

                _logger.LogInformation($"📋 ViewModel configurado:");
                _logger.LogInformation($"   LocalId: {viewModel.LocalId}");
                _logger.LogInformation($"   Empleado: {viewModel.EmpleadoResponsableNombre}");
                _logger.LogInformation($"   TipoKardex: {tipoKardex}");
            }

            else if (tipoKardex == TipoKardex.Vajilla)
            {
                var kardex = await _context.KardexVajilla
                    .Include(k => k.Empleado)
                    .Include(k => k.Local)
                    .FirstOrDefaultAsync(k => k.Id == kardexId);

                if (kardex == null)
                {
                    throw new Exception("Kardex no encontrado");
                }

                viewModel.Fecha = kardex.Fecha;
                viewModel.LocalId = kardex.LocalId;
                viewModel.EmpleadoResponsableId = kardex.EmpleadoId;
                viewModel.EmpleadoResponsableNombre = kardex.Empleado?.NombreCompleto ?? "";
            }
            else
            {
                _logger.LogError($"❌ Tipo de kardex no soportado: {tipoKardex}");
                throw new Exception($"Tipo de kardex no soportado: {tipoKardex}");
            }

            // ⭐ VERIFICACIÓN FINAL
            if (viewModel.LocalId == 0)
            {
                _logger.LogError($"❌ ERROR FINAL: ViewModel tiene LocalId = 0 después de configuración");
                _logger.LogError($"   TipoKardex: {tipoKardex}");
                _logger.LogError($"   KardexId: {kardexId}");
                throw new Exception("Error al configurar Personal Presente: LocalId inválido");
            }

            // ⭐ VERIFICACIÓN DE HORARIO
            viewModel.HoraActual = DateTime.Now;
            viewModel.HoraLimiteEnvio = new TimeSpan(17, 30, 0); // 5:30 PM
            viewModel.DentroDeHorario = DateTime.Now.TimeOfDay < viewModel.HoraLimiteEnvio;

            // ⭐ NUEVO: Verificar si hay habilitación manual (TODO: implementar lógica de habilitación)
            viewModel.EnvioHabilitadoManualmente = false;

            // Obtener empleados del área
            viewModel.EmpleadosDisponibles = await ObtenerEmpleadosDelAreaAsync(
                tipoKardex,
                viewModel.LocalId,
                viewModel.EmpleadoResponsableId
            );

            viewModel.TotalEmpleados = viewModel.EmpleadosDisponibles.Count;
            viewModel.TotalSeleccionados = viewModel.EmpleadosDisponibles.Count(e => e.Seleccionado);

            _logger.LogInformation($"✅ Personal Presente configurado: {viewModel.TotalEmpleados} empleado(s) disponible(s)");

            return viewModel;
        }

        public async Task<List<EmpleadoDisponibleDto>> ObtenerEmpleadosDelAreaAsync(
            string tipoKardex,
            int localId,
            string empleadoResponsableId)
        {
            // Determinar roles permitidos según el tipo de kardex
            var rolesPermitidos = TipoKardex.ObtenerRolesPermitidos(tipoKardex);

            _logger.LogInformation($"🔍 Buscando empleados para {tipoKardex} en Local {localId}");
            _logger.LogInformation($"   Roles permitidos: {string.Join(", ", rolesPermitidos)}");

            // Obtener empleados activos del local con los roles permitidos
            var empleados = await _context.Users
                .Where(u => u.Activo && u.LocalId == localId)
                .ToListAsync();

            _logger.LogInformation($"   Total empleados activos en el local: {empleados.Count}");

            var empleadosDto = new List<EmpleadoDisponibleDto>();

            foreach (var empleado in empleados)
            {
                var roles = await _userManager.GetRolesAsync(empleado);
                var tieneRolPermitido = roles.Any(r => rolesPermitidos.Contains(r));

                _logger.LogDebug($"   - {empleado.NombreCompleto}: Roles={string.Join(",", roles)}, Permitido={tieneRolPermitido}");

                if (tieneRolPermitido)
                {
                    var dto = new EmpleadoDisponibleDto
                    {
                        Id = empleado.Id,
                        NombreCompleto = empleado.NombreCompleto,
                        UserName = empleado.UserName ?? "",
                        Rol = roles.FirstOrDefault() ?? "",
                        EsResponsablePrincipal = empleado.Id == empleadoResponsableId,
                        Seleccionado = empleado.Id == empleadoResponsableId // Pre-seleccionar al responsable
                    };

                    empleadosDto.Add(dto);
                }
            }

            _logger.LogInformation($"✅ {empleadosDto.Count} empleados encontrados con roles permitidos");

            // Ordenar: responsable primero, luego por nombre
            return empleadosDto
                .OrderByDescending(e => e.EsResponsablePrincipal)
                .ThenBy(e => e.NombreCompleto)
                .ToList();
        }

        public async Task<PersonalPresenteResponse> GuardarPersonalPresenteYCompletarAsync(PersonalPresenteRequest request)
        {
            _logger.LogInformation($"📤 Iniciando GuardarPersonalPresenteYCompletar:");
            _logger.LogInformation($"   KardexId: {request.KardexId}");
            _logger.LogInformation($"   TipoKardex: {request.TipoKardex}");
            _logger.LogInformation($"   EmpleadosPresentes: {request.EmpleadosPresentes.Count}");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ⭐ VALIDAR HORARIO
                var horaActual = DateTime.Now.TimeOfDay;
                var horaLimite = new TimeSpan(17, 30, 0); // 5:30 PM
                var dentroDeHorario = horaActual < horaLimite;
                var envioHabilitadoManualmente = false; // TODO: Implementar lógica de habilitación

                if (!dentroDeHorario && !envioHabilitadoManualmente)
                {
                    _logger.LogWarning($"⏰ Intento de envío fuera de horario: {DateTime.Now:HH:mm:ss}");
                    return new PersonalPresenteResponse
                    {
                        Success = false,
                        Message = "Fuera de horario. El envío ha sido bloqueado. El horario límite de envío es 5:30 PM."
                    };
                }

                // Validar empleados presentes
                if (request.EmpleadosPresentes == null || request.EmpleadosPresentes.Count == 0)
                {
                    return new PersonalPresenteResponse
                    {
                        Success = false,
                        Message = "Debe seleccionar al menos un empleado presente"
                    };
                }

                // ⭐ INICIALIZAR VARIABLES CON VALORES POR DEFECTO
                string empleadoResponsableId = "";
                string empleadoResponsableNombre = "";
                int localId = 0;
                DateTime fechaKardex = DateTime.Today;

                // ⭐ MANEJO MEJORADO SEGÚN TIPO DE KARDEX
                if (request.TipoKardex == TipoKardex.MozoBebidas)
                {
                    _logger.LogInformation($"🍹 Procesando Kardex de Bebidas ID: {request.KardexId}");

                    var kardex = await _context.KardexBebidas
                        .Include(k => k.Empleado)
                        .Include(k => k.Asignacion)
                        .Include(k => k.Local) // ⭐ INCLUIR Local para debug
                        .FirstOrDefaultAsync(k => k.Id == request.KardexId);

                    if (kardex == null)
                    {
                        _logger.LogError($"❌ Kardex de Bebidas no encontrado: {request.KardexId}");
                        throw new Exception("Kardex no encontrado");
                    }

                    empleadoResponsableId = kardex.EmpleadoId;
                    empleadoResponsableNombre = kardex.Empleado?.NombreCompleto ?? "";
                    localId = kardex.LocalId;
                    fechaKardex = kardex.Fecha;

                    _logger.LogInformation($"📋 Datos del Kardex de Bebidas:");
                    _logger.LogInformation($"   LocalId: {localId}");
                    _logger.LogInformation($"   Local: {kardex.Local?.Nombre ?? "NULL"}");
                    _logger.LogInformation($"   EmpleadoId: {empleadoResponsableId}");
                    _logger.LogInformation($"   Empleado: {empleadoResponsableNombre}");

                    // ⭐ VALIDACIÓN CRÍTICA
                    if (localId == 0)
                    {
                        _logger.LogError($"❌ ERROR: Kardex de Bebidas {request.KardexId} tiene LocalId = 0");
                        throw new Exception("Error: El kardex no tiene un Local válido. Contacte al administrador.");
                    }

                    // Cambiar estado a "Enviado"
                    kardex.Estado = EstadoKardex.Enviado;
                    kardex.FechaFinalizacion = DateTime.Now;
                    kardex.FechaEnvio = DateTime.Now;
                    kardex.Observaciones = request.ObservacionesKardex;

                    if (kardex.Asignacion != null)
                    {
                        kardex.Asignacion.Estado = EstadoAsignacion.Completada;
                    }
                }
                // ⭐ KARDEX DE SALÓN
                else if (request.TipoKardex == TipoKardex.MozoSalon)
                {
                    _logger.LogInformation($"🍽️ Procesando Kardex de Salón ID: {request.KardexId}");

                    var kardex = await _context.KardexSalon
                        .Include(k => k.Empleado)
                        .Include(k => k.Asignacion)
                        .Include(k => k.Local) // ⭐ INCLUIR Local para debug
                        .FirstOrDefaultAsync(k => k.Id == request.KardexId);

                    if (kardex == null)
                    {
                        _logger.LogError($"❌ Kardex de Salón no encontrado: {request.KardexId}");
                        throw new Exception("Kardex no encontrado");
                    }

                    empleadoResponsableId = kardex.EmpleadoId;
                    empleadoResponsableNombre = kardex.Empleado?.NombreCompleto ?? "";
                    localId = kardex.LocalId;
                    fechaKardex = kardex.Fecha;

                    _logger.LogInformation($"📋 Datos del Kardex de Salón:");
                    _logger.LogInformation($"   LocalId: {localId}");
                    _logger.LogInformation($"   Local: {kardex.Local?.Nombre ?? "NULL"}");
                    _logger.LogInformation($"   EmpleadoId: {empleadoResponsableId}");
                    _logger.LogInformation($"   Empleado: {empleadoResponsableNombre}");
                    _logger.LogInformation($"   Fecha: {fechaKardex}");

                    // ⭐ VALIDACIÓN CRÍTICA
                    if (localId == 0)
                    {
                        _logger.LogError($"❌ ERROR CRÍTICO: Kardex de Salón {request.KardexId} tiene LocalId = 0");
                        _logger.LogError($"   Esto indica que el kardex se creó sin un Local válido");
                        _logger.LogError($"   Asignación ID: {kardex.AsignacionId}");
                        throw new Exception("Error: El kardex no tiene un Local válido asignado. Contacte al administrador del sistema.");
                    }

                    // Cambiar estado a "Enviado"
                    kardex.Estado = EstadoKardex.Enviado;
                    kardex.FechaFinalizacion = DateTime.Now;
                    kardex.FechaEnvio = DateTime.Now;
                    kardex.DescripcionFaltantes = request.DescripcionFaltantes;
                    kardex.Observaciones = request.ObservacionesKardex;

                    if (kardex.Asignacion != null)
                    {
                        kardex.Asignacion.Estado = EstadoAsignacion.Completada;
                    }

                    _logger.LogInformation($"📋 Kardex de Salón actualizado a estado Enviado");
                    _logger.LogInformation($"   Descripción de Faltantes: {(!string.IsNullOrEmpty(kardex.DescripcionFaltantes) ? "Sí" : "No")}");
                }
                else if (request.TipoKardex == TipoKardex.CocinaFria ||
                        request.TipoKardex == TipoKardex.CocinaCaliente ||
                        request.TipoKardex == TipoKardex.Parrilla)
                {
                    _logger.LogInformation($"🍳 Procesando Kardex de Cocina: {request.TipoKardex} - ID: {request.KardexId}");

                    var kardex = await _context.KardexCocina
                        .Include(k => k.Empleado)
                        .Include(k => k.Asignacion)
                        .Include(k => k.Local) // ⭐ INCLUIR Local para debug
                        .FirstOrDefaultAsync(k => k.Id == request.KardexId);

                    if (kardex == null)
                    {
                        _logger.LogError($"❌ Kardex de Cocina no encontrado: {request.KardexId}");
                        throw new Exception("Kardex no encontrado");
                    }

                    empleadoResponsableId = kardex.EmpleadoId;
                    empleadoResponsableNombre = kardex.Empleado?.NombreCompleto ?? "";
                    localId = kardex.LocalId;
                    fechaKardex = kardex.Fecha;

                    _logger.LogInformation($"📋 Datos del Kardex de Cocina:");
                    _logger.LogInformation($"   LocalId: {localId}");
                    _logger.LogInformation($"   Local: {kardex.Local?.Nombre ?? "NULL"}");
                    _logger.LogInformation($"   EmpleadoId: {empleadoResponsableId}");
                    _logger.LogInformation($"   Empleado: {empleadoResponsableNombre}");
                    _logger.LogInformation($"   TipoCocina: {kardex.TipoCocina}");
                    _logger.LogInformation($"   Fecha: {fechaKardex}");

                    // ⭐ VALIDACIÓN CRÍTICA
                    if (localId == 0)
                    {
                        _logger.LogError($"❌ ERROR CRÍTICO: Kardex de Cocina {request.KardexId} tiene LocalId = 0");
                        _logger.LogError($"   Esto indica que el kardex se creó sin un Local válido");
                        _logger.LogError($"   Asignación ID: {kardex.AsignacionId}");
                        throw new Exception("Error: El kardex no tiene un Local válido asignado. Contacte al administrador del sistema.");
                    }

                    // Cambiar estado a "Enviado"
                    kardex.Estado = EstadoKardex.Enviado;
                    kardex.FechaFinalizacion = DateTime.Now;
                    kardex.FechaEnvio = DateTime.Now;
                    kardex.Observaciones = request.ObservacionesKardex;

                    if (kardex.Asignacion != null)
                    {
                        kardex.Asignacion.Estado = EstadoAsignacion.Completada;
                    }

                    _logger.LogInformation($"📋 Kardex de Cocina actualizado a estado Enviado");
                }

                else if (request.TipoKardex == TipoKardex.Vajilla)
                {
                    _logger.LogInformation($"🍽️ Procesando Kardex de Vajilla ID: {request.KardexId}");

                    var kardex = await _context.KardexVajilla
                        .Include(k => k.Empleado)
                        .Include(k => k.Asignacion)
                        .Include(k => k.Local)
                        .FirstOrDefaultAsync(k => k.Id == request.KardexId);

                    if (kardex == null)
                    {
                        _logger.LogError($"❌ Kardex de Vajilla no encontrado: {request.KardexId}");
                        throw new Exception("Kardex no encontrado");
                    }

                    empleadoResponsableId = kardex.EmpleadoId;
                    empleadoResponsableNombre = kardex.Empleado?.NombreCompleto ?? "";
                    localId = kardex.LocalId;
                    fechaKardex = kardex.Fecha;

                    _logger.LogInformation($"📋 Datos del Kardex de Vajilla:");
                    _logger.LogInformation($"   LocalId: {localId}");
                    _logger.LogInformation($"   Local: {kardex.Local?.Nombre ?? "NULL"}");
                    _logger.LogInformation($"   EmpleadoId: {empleadoResponsableId}");
                    _logger.LogInformation($"   Empleado: {empleadoResponsableNombre}");
                    _logger.LogInformation($"   Fecha: {fechaKardex}");

                    // ⭐ VALIDACIÓN CRÍTICA
                    if (localId == 0)
                    {
                        _logger.LogError($"❌ ERROR CRÍTICO: Kardex de Vajilla {request.KardexId} tiene LocalId = 0");
                        throw new Exception("Error: El kardex no tiene un Local válido asignado. Contacte al administrador del sistema.");
                    }

                    // Cambiar estado a "Enviado"
                    kardex.Estado = EstadoKardex.Enviado;
                    kardex.FechaFinalizacion = DateTime.Now;
                    kardex.FechaEnvio = DateTime.Now;
                    kardex.DescripcionFaltantes = request.DescripcionFaltantes;
                    kardex.Observaciones = request.ObservacionesKardex;

                    // ⭐ PARSEAR cantidades de faltantes desde la descripción si existe
                    if (!string.IsNullOrEmpty(request.DescripcionFaltantes))
                    {
                        var lines = request.DescripcionFaltantes.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Cantidad Rotos:"))
                            {
                                var valor = line.Replace("Cantidad Rotos:", "").Trim();
                                if (int.TryParse(valor, out int rotos))
                                {
                                    kardex.CantidadRotos = rotos;
                                }
                            }
                            else if (line.StartsWith("Cantidad Extraviados:"))
                            {
                                var valor = line.Replace("Cantidad Extraviados:", "").Trim();
                                if (int.TryParse(valor, out int extraviados))
                                {
                                    kardex.CantidadExtraviados = extraviados;
                                }
                            }
                        }
                    }

                    if (kardex.Asignacion != null)
                    {
                        kardex.Asignacion.Estado = EstadoAsignacion.Completada;
                    }

                    _logger.LogInformation($"📋 Kardex de Vajilla actualizado a estado Enviado");
                    _logger.LogInformation($"   Descripción de Faltantes: {(!string.IsNullOrEmpty(kardex.DescripcionFaltantes) ? "Sí" : "No")}");
                    _logger.LogInformation($"   Cantidad Rotos: {kardex.CantidadRotos ?? 0}");
                    _logger.LogInformation($"   Cantidad Extraviados: {kardex.CantidadExtraviados ?? 0}");
                }

                else
                {
                    _logger.LogError($"❌ Tipo de kardex no soportado: {request.TipoKardex}");
                    throw new Exception($"Tipo de kardex no soportado: {request.TipoKardex}");
                }

                // Eliminar registros anteriores de personal presente
                var registrosAnteriores = await _context.Set<PersonalPresente>()
                    .Where(p => p.KardexId == request.KardexId && p.TipoKardex == request.TipoKardex)
                    .ToListAsync();

                _context.Set<PersonalPresente>().RemoveRange(registrosAnteriores);

                // Guardar personal presente
                foreach (var empleadoId in request.EmpleadosPresentes)
                {
                    var personalPresente = new PersonalPresente
                    {
                        KardexId = request.KardexId,
                        TipoKardex = request.TipoKardex,
                        EmpleadoId = empleadoId,
                        EsResponsablePrincipal = empleadoId == empleadoResponsableId,
                        FechaRegistro = DateTime.Now
                    };

                    _context.Set<PersonalPresente>().Add(personalPresente);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"✅ Kardex enviado exitosamente");
                _logger.LogInformation($"   Kardex ID: {request.KardexId}");
                _logger.LogInformation($"   Tipo: {request.TipoKardex}");
                _logger.LogInformation($"   Personal: {request.EmpleadosPresentes.Count} empleado(s)");
                _logger.LogInformation($"   Hora de envío: {DateTime.Now:HH:mm:ss}");

                await _auditService.RegistrarEnvioKardexAsync(
                    tipoKardex: request.TipoKardex,
                    fecha: fechaKardex,
                    empleadoNombre: empleadoResponsableNombre,
                    kardexId: request.KardexId,
                    totalPersonalPresente: request.EmpleadosPresentes.Count
                );

                // ⭐ BUSCAR Y NOTIFICAR AL ADMINISTRADOR LOCAL
                _logger.LogInformation($"🔍 Buscando Administrador Local para Local ID: {localId}");

                var usuariosLocal = await _context.Users
                    .Where(u => u.LocalId == localId && u.Activo)
                    .ToListAsync();

                _logger.LogInformation($"📋 Usuarios activos en el local {localId}: {usuariosLocal.Count}");

                Usuario? administradorLocal = null;

                foreach (var usuario in usuariosLocal)
                {
                    var roles = await _userManager.GetRolesAsync(usuario);
                    _logger.LogInformation($"   👤 Usuario: {usuario.NombreCompleto} ({usuario.UserName})");
                    _logger.LogInformation($"      Roles: {string.Join(", ", roles)}");
                    _logger.LogInformation($"      LocalId: {usuario.LocalId}");

                    if (roles.Contains("Administrador Local"))
                    {
                        administradorLocal = usuario;
                        _logger.LogInformation($"   ✅ Administrador Local encontrado: {administradorLocal.NombreCompleto}");
                        break;
                    }
                }

                if (administradorLocal != null)
                {
                    _logger.LogInformation($"📤 Enviando notificación al administrador: {administradorLocal.NombreCompleto}");

                    await _notificationService.CrearNotificacionKardexRecibidoAsync(
                        administradorId: administradorLocal.Id,
                        tipoKardex: request.TipoKardex,
                        empleadoResponsable: empleadoResponsableNombre,
                        fecha: fechaKardex
                    );

                    _logger.LogInformation($"✅ Notificación enviada exitosamente");
                }
                else
                {
                    _logger.LogWarning($"⚠️ No se encontró Administrador Local para el local ID {localId}");
                    _logger.LogWarning($"⚠️ Total de usuarios revisados: {usuariosLocal.Count}");
                    _logger.LogWarning($"⚠️ El kardex se guardó correctamente, pero no se envió notificación");
                }

                return new PersonalPresenteResponse
                {
                    Success = true,
                    Message = "Kardex enviado exitosamente",
                    TotalRegistrados = request.EmpleadosPresentes.Count
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error al enviar kardex al administrador");
                _logger.LogError($"   KardexId: {request.KardexId}");
                _logger.LogError($"   TipoKardex: {request.TipoKardex}");

                return new PersonalPresenteResponse
                {
                    Success = false,
                    Message = $"Error al enviar el kardex: {ex.Message}"
                };
            }
        }

        // ==========================================
        // KARDEX DE SALÓN (Mozo Salón)
        // ==========================================

        public async Task<KardexSalonViewModel> IniciarKardexSalonAsync(int asignacionId, string usuarioId)
        {
            _logger.LogInformation($"🍽️ Iniciando Kardex de Salón - AsignacionId: {asignacionId}, UsuarioId: {usuarioId}");

            var asignacion = await _context.AsignacionesKardex
                .Include(a => a.Local)
                .Include(a => a.Empleado)
                .FirstOrDefaultAsync(a => a.Id == asignacionId && a.EmpleadoId == usuarioId);

            if (asignacion == null)
            {
                _logger.LogError($"❌ Asignación no encontrada: {asignacionId}");
                throw new Exception("Asignación no encontrada o no autorizada");
            }

            // ⭐ VALIDACIÓN CRÍTICA: Verificar LocalId
            if (asignacion.LocalId == 0)
            {
                _logger.LogError($"❌ ERROR CRÍTICO: La asignación {asignacionId} tiene LocalId = 0");
                _logger.LogError($"   Empleado: {asignacion.EmpleadoId}");
                _logger.LogError($"   Fecha: {asignacion.Fecha}");
                throw new Exception("Error: La asignación no tiene un Local válido asignado. Contacte al administrador.");
            }

            _logger.LogInformation($"✅ Asignación encontrada - LocalId: {asignacion.LocalId}, Local: {asignacion.Local?.Nombre ?? "N/A"}");

            // Verificar si ya existe un kardex para esta asignación
            var kardexExistente = await _context.KardexSalon
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Utensilio)
                .FirstOrDefaultAsync(k => k.AsignacionId == asignacionId);

            if (kardexExistente != null)
            {
                _logger.LogInformation($"📋 Kardex existente encontrado: ID {kardexExistente.Id}, LocalId: {kardexExistente.LocalId}");
                return await MapearKardexSalonAViewModel(kardexExistente);
            }

            // Crear nuevo kardex
            var kardex = new KardexSalon
            {
                AsignacionId = asignacionId,
                Fecha = asignacion.Fecha,
                LocalId = asignacion.LocalId, // ✅ Usar el LocalId de la asignación
                EmpleadoId = usuarioId,
                Estado = EstadoKardex.Borrador,
                FechaInicio = DateTime.Now
            };

            _logger.LogInformation($"💾 Creando nuevo Kardex de Salón:");
            _logger.LogInformation($"   LocalId: {kardex.LocalId}");
            _logger.LogInformation($"   EmpleadoId: {kardex.EmpleadoId}");
            _logger.LogInformation($"   Fecha: {kardex.Fecha}");

            _context.KardexSalon.Add(kardex);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Kardex de Salón creado: ID {kardex.Id}, LocalId: {kardex.LocalId}");

            // ⭐ VERIFICACIÓN POST-GUARDADO
            if (kardex.LocalId == 0)
            {
                _logger.LogError($"❌ ERROR POST-GUARDADO: El kardex se guardó con LocalId = 0");
                throw new Exception("Error al guardar el kardex: LocalId inválido");
            }

            // Obtener utensilios de categoría "Mozo" activos
            var utensiliosMozo = await _context.Utensilios
                .Include(u => u.Categoria)
                .Where(u => u.Activo &&
                        u.Categoria!.Tipo == TipoCategoria.Utensilios &&
                        u.Categoria.Nombre == "Mozo" &&
                        u.Categoria.Activo)
                .OrderBy(u => u.Codigo)
                .ToListAsync();

            _logger.LogInformation($"📦 {utensiliosMozo.Count} utensilios encontrados para el kardex");

            var orden = 1;
            foreach (var utensilio in utensiliosMozo)
            {
                var detalle = new KardexSalonDetalle
                {
                    KardexSalonId = kardex.Id,
                    UtensilioId = utensilio.Id,
                    InventarioInicial = 0, // TODO: Obtener del sistema o configuración
                    Orden = orden++
                };

                _context.KardexSalonDetalle.Add(detalle);
            }

            await _context.SaveChangesAsync();

            // Marcar asignación como en proceso
            asignacion.Estado = EstadoAsignacion.EnProceso;
            asignacion.RegistroIniciado = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Kardex de salón iniciado completamente: ID {kardex.Id}");

            await _auditService.RegistrarInicioKardexAsync(
                tipoKardex: TipoKardex.MozoSalon,
                fecha: asignacion.Fecha,
                empleadoNombre: asignacion.Empleado?.NombreCompleto ?? "Desconocido",
                kardexId: kardex.Id
            );

            return await ObtenerKardexSalonAsync(kardex.Id);
        }

        public async Task<KardexSalonViewModel> ObtenerKardexSalonAsync(int kardexId)
        {
            var kardex = await _context.KardexSalon
                .Include(k => k.Asignacion)
                .Include(k => k.Empleado)
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Utensilio)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            return await MapearKardexSalonAViewModel(kardex);
        }

        public async Task<bool> AutoguardarDetalleSalonAsync(AutoguardadoKardexSalonRequest request)
        {
            try
            {
                var detalle = await _context.KardexSalonDetalle
                    .FirstOrDefaultAsync(d => d.Id == request.DetalleId &&
                                            d.KardexSalonId == request.KardexId);

                if (detalle == null)
                {
                    _logger.LogWarning($"Detalle no encontrado: {request.DetalleId}");
                    return false;
                }

                // Actualizar unidades contadas
                detalle.UnidadesContadas = request.Valor;

                // Recalcular diferencia
                RecalcularDetalleSalon(detalle);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Autoguardado exitoso: Detalle {request.DetalleId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en autoguardado: {ex.Message}");
                return false;
            }
        }

        public async Task<KardexSalonViewModel> CalcularYActualizarSalonAsync(int kardexId)
        {
            var kardex = await _context.KardexSalon
                .Include(k => k.Detalles)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            foreach (var detalle in kardex.Detalles)
            {
                RecalcularDetalleSalon(detalle);
            }

            await _context.SaveChangesAsync();

            return await ObtenerKardexSalonAsync(kardexId);
        }

        public async Task<bool> CompletarKardexSalonAsync(int kardexId, string descripcionFaltantes, string observaciones)
        {
            var kardex = await _context.KardexSalon
                .Include(k => k.Detalles)
                .Include(k => k.Asignacion)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            // Validar que todos los campos estén completos
            var detallesIncompletos = kardex.Detalles.Where(d => !d.UnidadesContadas.HasValue).ToList();

            if (detallesIncompletos.Any())
            {
                throw new Exception($"Hay {detallesIncompletos.Count} utensilio(s) sin registrar");
            }

            kardex.Estado = EstadoKardex.Completado;
            kardex.FechaFinalizacion = DateTime.Now;
            kardex.DescripcionFaltantes = descripcionFaltantes;
            kardex.Observaciones = observaciones;

            if (kardex.Asignacion != null)
            {
                kardex.Asignacion.Estado = EstadoAsignacion.Completada;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Kardex de salón completado: ID {kardexId}");

            return true;
        }

        // Método auxiliar privado
        private void RecalcularDetalleSalon(KardexSalonDetalle detalle)
        {
            if (detalle.UnidadesContadas.HasValue)
            {
                detalle.Diferencia = detalle.UnidadesContadas.Value - detalle.InventarioInicial;
                detalle.TieneFaltantes = detalle.Diferencia < 0;
            }
            else
            {
                detalle.Diferencia = 0;
                detalle.TieneFaltantes = false;
            }
        }

        private async Task<KardexSalonViewModel> MapearKardexSalonAViewModel(KardexSalon kardex)
        {
            var detalles = kardex.Detalles
                .OrderBy(d => d.Orden)
                .Select(d => new KardexSalonDetalleViewModel
                {
                    Id = d.Id,
                    UtensilioId = d.UtensilioId,
                    Codigo = d.Utensilio?.Codigo ?? "",
                    Descripcion = d.Utensilio?.Nombre ?? "",
                    Unidad = d.Utensilio?.Unidad ?? "",
                    InventarioInicial = d.InventarioInicial,
                    UnidadesContadas = d.UnidadesContadas,
                    Diferencia = d.Diferencia,
                    TieneFaltantes = d.TieneFaltantes,
                    Observaciones = d.Observaciones,
                    Orden = d.Orden,
                    EstaCompleto = d.UnidadesContadas.HasValue
                })
                .ToList();

            var totalUtensilios = detalles.Count;
            var utensiliosCompletos = detalles.Count(d => d.EstaCompleto);
            var utensiliosConFaltantes = detalles.Count(d => d.TieneFaltantes);

            return new KardexSalonViewModel
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
                DescripcionFaltantes = kardex.DescripcionFaltantes,
                Observaciones = kardex.Observaciones,
                Detalles = detalles,
                TotalUtensilios = totalUtensilios,
                UtensiliosCompletos = utensiliosCompletos,
                UtensiliosConFaltantes = utensiliosConFaltantes,
                PorcentajeAvance = totalUtensilios > 0
                    ? (decimal)utensiliosCompletos / totalUtensilios * 100
                    : 0,
                RequiereDescripcionFaltantes = utensiliosConFaltantes > 0
            };
        }
        // ==========================================
        // KARDEX DE COCINA (Fría, Caliente, Parrilla)
        // ==========================================

        public async Task<KardexCocinaViewModel> IniciarKardexCocinaAsync(int asignacionId, string usuarioId)
        {
            _logger.LogInformation($"🍳 Iniciando Kardex de Cocina - AsignacionId: {asignacionId}, UsuarioId: {usuarioId}");

            var asignacion = await _context.AsignacionesKardex
                .Include(a => a.Local)
                .Include(a => a.Empleado)
                .FirstOrDefaultAsync(a => a.Id == asignacionId && a.EmpleadoId == usuarioId);

            if (asignacion == null)
            {
                _logger.LogError($"❌ Asignación no encontrada: {asignacionId}");
                throw new Exception("Asignación no encontrada o no autorizada");
            }

            // Validar LocalId
            if (asignacion.LocalId == 0)
            {
                _logger.LogError($"❌ ERROR CRÍTICO: La asignación {asignacionId} tiene LocalId = 0");
                throw new Exception("Error: La asignación no tiene un Local válido asignado. Contacte al administrador.");
            }

            _logger.LogInformation($"✅ Asignación encontrada - LocalId: {asignacion.LocalId}, Tipo: {asignacion.TipoKardex}");

            // Verificar si ya existe un kardex para esta asignación
            var kardexExistente = await _context.KardexCocina
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(k => k.AsignacionId == asignacionId);

            if (kardexExistente != null)
            {
                _logger.LogInformation($"📋 Kardex existente encontrado: ID {kardexExistente.Id}");
                return await MapearKardexCocinaAViewModel(kardexExistente);
            }

            // Crear nuevo kardex
            var kardex = new KardexCocina
            {
                AsignacionId = asignacionId,
                Fecha = asignacion.Fecha,
                LocalId = asignacion.LocalId,
                EmpleadoId = usuarioId,
                TipoCocina = asignacion.TipoKardex,
                Estado = EstadoKardex.Borrador,
                FechaInicio = DateTime.Now
            };

            _logger.LogInformation($"💾 Creando nuevo Kardex de Cocina:");
            _logger.LogInformation($"   LocalId: {kardex.LocalId}");
            _logger.LogInformation($"   TipoCocina: {kardex.TipoCocina}");

            _context.KardexCocina.Add(kardex);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Kardex de Cocina creado: ID {kardex.Id}");

            // Verificación post-guardado
            if (kardex.LocalId == 0)
            {
                _logger.LogError($"❌ ERROR POST-GUARDADO: El kardex se guardó con LocalId = 0");
                throw new Exception("Error al guardar el kardex: LocalId inválido");
            }

            // Obtener categoría especial según tipo de cocina
            var categoriaEspecial = TipoCocinaKardex.ObtenerCategoriaEspecial(asignacion.TipoKardex);

            // Obtener productos de cocina activos
            var productosCocina = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo &&
                        p.Categoria!.Tipo == TipoCategoria.Cocina &&
                        p.Categoria.Activo &&
                        (
                            // Productos generales (sin tipo especial)
                            p.Categoria.TipoCocinaEspecial == null ||
                            // Productos de la categoría especial de este tipo de cocina
                            p.Categoria.Nombre == categoriaEspecial
                        ))
                .OrderBy(p => p.Categoria!.Orden)
                .ThenBy(p => p.Codigo)
                .ToListAsync();

            _logger.LogInformation($"📦 {productosCocina.Count} productos encontrados para el kardex");

            var orden = 1;
            foreach (var producto in productosCocina)
            {
                var detalle = new KardexCocinaDetalle
                {
                    KardexCocinaId = kardex.Id,
                    ProductoId = producto.Id,
                    UnidadMedida = producto.Unidad, // Usar unidad por defecto del producto
                    CantidadAPedir = 0, // TODO: Calcular según lógica de negocio
                    Ingresos = 0, // TODO: Obtener de compras del día
                    Orden = orden++
                };

                _context.Set<KardexCocinaDetalle>().Add(detalle);
            }

            await _context.SaveChangesAsync();

            // Marcar asignación como en proceso
            asignacion.Estado = EstadoAsignacion.EnProceso;
            asignacion.RegistroIniciado = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Kardex de cocina iniciado completamente: ID {kardex.Id}");

            await _auditService.RegistrarInicioKardexAsync(
                tipoKardex: asignacion.TipoKardex,
                fecha: asignacion.Fecha,
                empleadoNombre: asignacion.Empleado?.NombreCompleto ?? "Desconocido",
                kardexId: kardex.Id
            );

            return await ObtenerKardexCocinaAsync(kardex.Id);
        }

        public async Task<KardexCocinaViewModel> ObtenerKardexCocinaAsync(int kardexId)
        {
            var kardex = await _context.KardexCocina
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

            return await MapearKardexCocinaAViewModel(kardex);
        }

        public async Task<bool> AutoguardarDetalleCocinaAsync(AutoguardadoKardexCocinaRequest request)
        {
            try
            {
                var detalle = await _context.Set<KardexCocinaDetalle>()
                    .FirstOrDefaultAsync(d => d.Id == request.DetalleId &&
                                            d.KardexCocinaId == request.KardexId);

                if (detalle == null)
                {
                    _logger.LogWarning($"Detalle no encontrado: {request.DetalleId}");
                    return false;
                }

                // Actualizar el campo correspondiente
                switch (request.Campo)
                {
                    case "StockFinal":
                        detalle.StockFinal = request.ValorNumerico;
                        break;
                    case "UnidadMedida":
                        if (!string.IsNullOrEmpty(request.ValorTexto) &&
                            UnidadMedidaCocina.Todas.Contains(request.ValorTexto))
                        {
                            detalle.UnidadMedida = request.ValorTexto;
                        }
                        break;
                    default:
                        _logger.LogWarning($"Campo no reconocido: {request.Campo}");
                        return false;
                }

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

        public async Task<KardexCocinaViewModel> CalcularYActualizarCocinaAsync(int kardexId)
        {
            var kardex = await _context.KardexCocina
                .Include(k => k.Detalles)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            // Aquí se pueden agregar cálculos adicionales si es necesario
            await _context.SaveChangesAsync();

            return await ObtenerKardexCocinaAsync(kardexId);
        }

        private async Task<KardexCocinaViewModel> MapearKardexCocinaAViewModel(KardexCocina kardex)
        {
            // Agrupar productos por categoría
            var categorias = new List<KardexCocinaCategoriaViewModel>();

            var categoriaEspecial = TipoCocinaKardex.ObtenerCategoriaEspecial(kardex.TipoCocina);

            var productosAgrupados = kardex.Detalles
                .OrderBy(d => d.Orden)
                .GroupBy(d => d.Producto?.Categoria?.Nombre ?? "Sin Categoría");

            foreach (var grupo in productosAgrupados)
            {
                var esEspecial = grupo.Key == categoriaEspecial;

                var categoria = new KardexCocinaCategoriaViewModel
                {
                    NombreCategoria = grupo.Key,
                    EsEspecial = esEspecial,
                    Expandida = true, // Todas expandidas por defecto
                    Productos = grupo.Select(d => new KardexCocinaDetalleViewModel
                    {
                        Id = d.Id,
                        ProductoId = d.ProductoId,
                        Categoria = d.Producto?.Categoria?.Nombre ?? "",
                        Codigo = d.Producto?.Codigo ?? "",
                        NombreProducto = d.Producto?.Nombre ?? "",
                        UnidadMedida = d.UnidadMedida,
                        CantidadAPedir = d.CantidadAPedir,
                        Ingresos = d.Ingresos,
                        StockFinal = d.StockFinal,
                        Observaciones = d.Observaciones,
                        Orden = d.Orden
                    }).ToList()
                };

                categorias.Add(categoria);
            }

            var totalProductos = categorias.Sum(c => c.TotalProductos);
            var productosCompletos = categorias.Sum(c => c.ProductosCompletos);

            return new KardexCocinaViewModel
            {
                Id = kardex.Id,
                AsignacionId = kardex.AsignacionId,
                Fecha = kardex.Fecha,
                LocalId = kardex.LocalId,
                EmpleadoId = kardex.EmpleadoId,
                EmpleadoNombre = kardex.Empleado?.NombreCompleto ?? "",
                TipoCocina = kardex.TipoCocina,
                Estado = kardex.Estado,
                FechaInicio = kardex.FechaInicio,
                FechaFinalizacion = kardex.FechaFinalizacion,
                FechaEnvio = kardex.FechaEnvio,
                Observaciones = kardex.Observaciones,
                Categorias = categorias,
                TotalProductos = totalProductos,
                ProductosCompletos = productosCompletos,
                PorcentajeAvance = totalProductos > 0
                    ? (decimal)productosCompletos / totalProductos * 100
                    : 0
            };
        }

        // ==========================================
        // KARDEX DE VAJILLA (Vajillero)
        // ==========================================
        // ⭐ AGREGAR ESTOS MÉTODOS AL FINAL DE LA CLASE KardexService

        public async Task<KardexVajillaViewModel> IniciarKardexVajillaAsync(int asignacionId, string usuarioId)
        {
            _logger.LogInformation($"🍽️ Iniciando Kardex de Vajilla - AsignacionId: {asignacionId}, UsuarioId: {usuarioId}");

            var asignacion = await _context.AsignacionesKardex
                .Include(a => a.Local)
                .Include(a => a.Empleado)
                .FirstOrDefaultAsync(a => a.Id == asignacionId && a.EmpleadoId == usuarioId);

            if (asignacion == null)
            {
                _logger.LogError($"❌ Asignación no encontrada: {asignacionId}");
                throw new Exception("Asignación no encontrada o no autorizada");
            }

            // ⭐ VALIDACIÓN CRÍTICA: Verificar LocalId
            if (asignacion.LocalId == 0)
            {
                _logger.LogError($"❌ ERROR CRÍTICO: La asignación {asignacionId} tiene LocalId = 0");
                throw new Exception("Error: La asignación no tiene un Local válido asignado. Contacte al administrador.");
            }

            _logger.LogInformation($"✅ Asignación encontrada - LocalId: {asignacion.LocalId}, Local: {asignacion.Local?.Nombre ?? "N/A"}");

            // Verificar si ya existe un kardex para esta asignación
            var kardexExistente = await _context.KardexVajilla
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Utensilio)
                        .ThenInclude(u => u.Categoria)
                .FirstOrDefaultAsync(k => k.AsignacionId == asignacionId);

            if (kardexExistente != null)
            {
                _logger.LogInformation($"📋 Kardex existente encontrado: ID {kardexExistente.Id}, LocalId: {kardexExistente.LocalId}");
                return await MapearKardexVajillaAViewModel(kardexExistente);
            }

            // Crear nuevo kardex
            var kardex = new KardexVajilla
            {
                AsignacionId = asignacionId,
                Fecha = asignacion.Fecha,
                LocalId = asignacion.LocalId,
                EmpleadoId = usuarioId,
                Estado = EstadoKardex.Borrador,
                FechaInicio = DateTime.Now
            };

            _logger.LogInformation($"💾 Creando nuevo Kardex de Vajilla:");
            _logger.LogInformation($"   LocalId: {kardex.LocalId}");
            _logger.LogInformation($"   EmpleadoId: {kardex.EmpleadoId}");
            _logger.LogInformation($"   Fecha: {kardex.Fecha}");

            _context.KardexVajilla.Add(kardex);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Kardex de Vajilla creado: ID {kardex.Id}, LocalId: {kardex.LocalId}");

            // ⭐ VERIFICACIÓN POST-GUARDADO
            if (kardex.LocalId == 0)
            {
                _logger.LogError($"❌ ERROR POST-GUARDADO: El kardex se guardó con LocalId = 0");
                throw new Exception("Error al guardar el kardex: LocalId inválido");
            }

            // Obtener utensilios de categorías: Utensilios Cocina, Menajería Cocina, Equipos
            // Obtener utensilios de categoría "Mozo" activos
            var utensiliosVajilla = await _context.Utensilios
                .Include(u => u.Categoria)
                .Where(u => u.Activo &&
                        u.Categoria!.Tipo == TipoCategoria.Utensilios &&
                        u.Categoria.Nombre == "Cocina" &&
                        u.Categoria.Activo)
                .OrderBy(u => u.Codigo)
                .ToListAsync();

            _logger.LogInformation($"📦 {utensiliosVajilla.Count} utensilios encontrados para el kardex de vajilla");

            var orden = 1;
            foreach (var utensilio in utensiliosVajilla)
            {
                var detalle = new KardexVajillaDetalle
                {
                    KardexVajillaId = kardex.Id,
                    UtensilioId = utensilio.Id,
                    InventarioInicial = 0, // TODO: Obtener del sistema o configuración
                    Orden = orden++
                };

                _context.KardexVajillaDetalle.Add(detalle);
            }

            await _context.SaveChangesAsync();

            // Marcar asignación como en proceso
            asignacion.Estado = EstadoAsignacion.EnProceso;
            asignacion.RegistroIniciado = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Kardex de vajilla iniciado completamente: ID {kardex.Id}");

            await _auditService.RegistrarInicioKardexAsync(
                tipoKardex: TipoKardex.Vajilla,
                fecha: asignacion.Fecha,
                empleadoNombre: asignacion.Empleado?.NombreCompleto ?? "Desconocido",
                kardexId: kardex.Id
            );

            return await ObtenerKardexVajillaAsync(kardex.Id);
        }

        public async Task<KardexVajillaViewModel> ObtenerKardexVajillaAsync(int kardexId)
        {
            var kardex = await _context.KardexVajilla
                .Include(k => k.Asignacion)
                .Include(k => k.Empleado)
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Utensilio)
                        .ThenInclude(u => u.Categoria)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            return await MapearKardexVajillaAViewModel(kardex);
        }

        public async Task<bool> AutoguardarDetalleVajillaAsync(AutoguardadoKardexVajillaRequest request)
        {
            try
            {
                var detalle = await _context.KardexVajillaDetalle
                    .FirstOrDefaultAsync(d => d.Id == request.DetalleId &&
                                            d.KardexVajillaId == request.KardexId);

                if (detalle == null)
                {
                    _logger.LogWarning($"Detalle no encontrado: {request.DetalleId}");
                    return false;
                }

                // Actualizar unidades contadas
                detalle.UnidadesContadas = request.Valor;

                // Recalcular diferencia
                RecalcularDetalleVajilla(detalle);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Autoguardado exitoso: Detalle {request.DetalleId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en autoguardado: {ex.Message}");
                return false;
            }
        }

        public async Task<KardexVajillaViewModel> CalcularYActualizarVajillaAsync(int kardexId)
        {
            var kardex = await _context.KardexVajilla
                .Include(k => k.Detalles)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            foreach (var detalle in kardex.Detalles)
            {
                RecalcularDetalleVajilla(detalle);
            }

            await _context.SaveChangesAsync();

            return await ObtenerKardexVajillaAsync(kardexId);
        }

        // Método auxiliar privado
        private void RecalcularDetalleVajilla(KardexVajillaDetalle detalle)
        {
            if (detalle.UnidadesContadas.HasValue)
            {
                detalle.Diferencia = detalle.UnidadesContadas.Value - detalle.InventarioInicial;
                detalle.TieneFaltantes = detalle.Diferencia < 0;
            }
            else
            {
                detalle.Diferencia = 0;
                detalle.TieneFaltantes = false;
            }
        }

        private async Task<KardexVajillaViewModel> MapearKardexVajillaAViewModel(KardexVajilla kardex)
        {
            var detalles = kardex.Detalles
                .OrderBy(d => d.Orden)
                .Select(d => new KardexVajillaDetalleViewModel
                {
                    Id = d.Id,
                    UtensilioId = d.UtensilioId,
                    Categoria = d.Utensilio?.Categoria?.Nombre ?? "",
                    Codigo = d.Utensilio?.Codigo ?? "",
                    Descripcion = d.Utensilio?.Nombre ?? "",
                    Unidad = d.Utensilio?.Unidad ?? "",
                    InventarioInicial = d.InventarioInicial,
                    UnidadesContadas = d.UnidadesContadas,
                    Diferencia = d.Diferencia,
                    TieneFaltantes = d.TieneFaltantes,
                    Observaciones = d.Observaciones,
                    Orden = d.Orden,
                    EstaCompleto = d.UnidadesContadas.HasValue
                })
                .ToList();

            var totalUtensilios = detalles.Count;
            var utensiliosCompletos = detalles.Count(d => d.EstaCompleto);
            var utensiliosConFaltantes = detalles.Count(d => d.TieneFaltantes);

            return new KardexVajillaViewModel
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
                DescripcionFaltantes = kardex.DescripcionFaltantes,
                CantidadRotos = kardex.CantidadRotos,
                CantidadExtraviados = kardex.CantidadExtraviados,
                Observaciones = kardex.Observaciones,
                Detalles = detalles,
                TotalUtensilios = totalUtensilios,
                UtensiliosCompletos = utensiliosCompletos,
                UtensiliosConFaltantes = utensiliosConFaltantes,
                PorcentajeAvance = totalUtensilios > 0
                    ? (decimal)utensiliosCompletos / totalUtensilios * 100
                    : 0,
                RequiereDescripcionFaltantes = utensiliosConFaltantes > 0
            };
        }

        // ⭐ TAMBIÉN AGREGAR EN VerificarBorradorVajilla (dentro de ObtenerMiKardexAsync)
        private async Task VerificarBorradorVajilla(MiKardexViewModel viewModel, int asignacionId)
        {
            var kardexBorrador = await _context.KardexVajilla
                .FirstOrDefaultAsync(k => k.AsignacionId == asignacionId &&
                                        k.Estado == EstadoKardex.Borrador);

            if (kardexBorrador != null)
            {
                viewModel.ExisteKardexBorrador = true;
                viewModel.KardexBorradorId = kardexBorrador.Id;

                // Calcular porcentaje de avance
                var totalDetalles = await _context.KardexVajillaDetalle
                    .CountAsync(d => d.KardexVajillaId == kardexBorrador.Id);

                var detallesCompletos = await _context.KardexVajillaDetalle
                    .CountAsync(d => d.KardexVajillaId == kardexBorrador.Id &&
                                    d.UnidadesContadas.HasValue);

                viewModel.PorcentajeAvanceBorrador = totalDetalles > 0
                    ? (decimal)detallesCompletos / totalDetalles * 100
                    : 0;
            }

            viewModel.PuedeIniciarRegistro = true;
        }

        // ==========================================
        // MÉTODOS DE REVISIÓN DE KARDEX
        // ==========================================

        /// <summary>
        /// Obtener kardex de cocina consolidado (3 cocineros)
        /// </summary>
        public async Task<KardexCocinaConsolidadoViewModel> ObtenerKardexCocinaConsolidadoAsync(List<int> kardexIds)
        {
            _logger.LogInformation($"🔍 Obteniendo kardex consolidado de cocina: {string.Join(", ", kardexIds)}");

            // Obtener los 3 kardex de cocina
            var kardexList = await _context.KardexCocina
                .Include(k => k.Empleado)
                .Include(k => k.Local)
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Categoria)
                .Where(k => kardexIds.Contains(k.Id))
                .OrderBy(k => k.TipoCocina)
                .ToListAsync();

            if (!kardexList.Any())
            {
                throw new Exception("No se encontraron kardex de cocina");
            }

            var primerKardex = kardexList.First();

            var viewModel = new KardexCocinaConsolidadoViewModel
            {
                Fecha = primerKardex.Fecha,
                LocalId = primerKardex.LocalId,
                LocalNombre = primerKardex.Local?.Nombre ?? "",
                KardexCocinaFria = kardexList.FirstOrDefault(k => k.TipoCocina == TipoKardex.CocinaFria)?.ToIndividualDto(),
                KardexCocinaCaliente = kardexList.FirstOrDefault(k => k.TipoCocina == TipoKardex.CocinaCaliente)?.ToIndividualDto(),
                KardexParrilla = kardexList.FirstOrDefault(k => k.TipoCocina == TipoKardex.Parrilla)?.ToIndividualDto()
            };

            // Consolidar productos por categoría
            var categoriasDict = new Dictionary<string, CategoriaCocinaConsolidadaViewModel>();

            // Obtener todas las categorías únicas
            var todasLasCategorias = kardexList
                .SelectMany(k => k.Detalles)
                .Select(d => d.Producto?.Categoria?.Nombre ?? "Sin Categoría")
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var nombreCategoria in todasLasCategorias)
            {
                var categoria = new CategoriaCocinaConsolidadaViewModel
                {
                    NombreCategoria = nombreCategoria,
                    EsEspecial = EsCategoriaEspecial(nombreCategoria),
                    TipoCocinaResponsable = ObtenerTipoCocinaResponsable(nombreCategoria),
                    Expandida = true
                };

                // Obtener productos de esta categoría
                var productosDeCategoria = kardexList
                    .SelectMany(k => k.Detalles)
                    .Where(d => (d.Producto?.Categoria?.Nombre ?? "Sin Categoría") == nombreCategoria)
                    .GroupBy(d => d.ProductoId)
                    .Select(g => new
                    {
                        ProductoId = g.Key,
                        Producto = g.First().Producto,
                        Detalles = g.ToList()
                    })
                    .OrderBy(p => p.Producto?.Codigo)
                    .ToList();

                foreach (var productoGrupo in productosDeCategoria)
                {
                    var producto = productoGrupo.Producto;
                    if (producto == null) continue;

                    var productoVm = new ProductoCocinaConsolidadoViewModel
                    {
                        ProductoId = productoGrupo.ProductoId,
                        Codigo = producto.Codigo,
                        NombreProducto = producto.Nombre,
                        UnidadMedida = productoGrupo.Detalles.First().UnidadMedida,
                        CantidadAPedir = productoGrupo.Detalles.First().CantidadAPedir,
                        Ingresos = productoGrupo.Detalles.First().Ingresos,
                        Orden = productoGrupo.Detalles.First().Orden
                    };

                    if (categoria.EsEspecial)
                    {
                        // Producto específico: solo 1 cocinero responsable
                        var detalleResponsable = productoGrupo.Detalles.FirstOrDefault();
                        productoVm.StockFinalEspecifico = detalleResponsable?.StockFinal;

                        // Calcular diferencia
                        var stockEsperado = productoVm.CantidadAPedir + productoVm.Ingresos;
                        productoVm.Diferencia = (productoVm.StockFinalEspecifico ?? 0) - stockEsperado;

                        if (stockEsperado > 0)
                        {
                            productoVm.DiferenciaPorcentual = Math.Abs((productoVm.Diferencia / stockEsperado) * 100);
                            productoVm.TieneDiferenciaSignificativa = productoVm.DiferenciaPorcentual > 10;
                        }
                    }
                    else
                    {
                        // Producto compartido: conteo de los 3 cocineros
                        foreach (var detalle in productoGrupo.Detalles)
                        {
                            var kardexOrigen = kardexList.First(k => k.Detalles.Contains(detalle));

                            if (kardexOrigen.TipoCocina == TipoKardex.CocinaFria)
                                productoVm.StockFinalCocinaFria = detalle.StockFinal;
                            else if (kardexOrigen.TipoCocina == TipoKardex.CocinaCaliente)
                                productoVm.StockFinalCocinaCaliente = detalle.StockFinal;
                            else if (kardexOrigen.TipoCocina == TipoKardex.Parrilla)
                                productoVm.StockFinalParrilla = detalle.StockFinal;
                        }

                        // Calcular promedio
                        var conteos = new List<decimal>();
                        if (productoVm.StockFinalCocinaFria.HasValue) conteos.Add(productoVm.StockFinalCocinaFria.Value);
                        if (productoVm.StockFinalCocinaCaliente.HasValue) conteos.Add(productoVm.StockFinalCocinaCaliente.Value);
                        if (productoVm.StockFinalParrilla.HasValue) conteos.Add(productoVm.StockFinalParrilla.Value);

                        if (conteos.Any())
                        {
                            productoVm.StockFinalPromedio = conteos.Average();

                            // Calcular diferencia basada en el promedio
                            var stockEsperado = productoVm.CantidadAPedir + productoVm.Ingresos;
                            productoVm.Diferencia = productoVm.StockFinalPromedio.Value - stockEsperado;

                            if (stockEsperado > 0)
                            {
                                productoVm.DiferenciaPorcentual = Math.Abs((productoVm.Diferencia / stockEsperado) * 100);
                                productoVm.TieneDiferenciaSignificativa = productoVm.DiferenciaPorcentual > 10;
                            }
                        }
                    }

                    categoria.Productos.Add(productoVm);
                }

                categoriasDict[nombreCategoria] = categoria;
            }

            viewModel.CategoriasConsolidadas = categoriasDict.Values.ToList();
            viewModel.TotalProductos = viewModel.CategoriasConsolidadas.Sum(c => c.TotalProductos);
            viewModel.ProductosConDiferencia = viewModel.CategoriasConsolidadas.Sum(c => c.ProductosConDiferencia);

            if (viewModel.TotalProductos > 0)
            {
                viewModel.PorcentajeProductosConDiferencia =
                    (decimal)viewModel.ProductosConDiferencia / viewModel.TotalProductos * 100;
            }

            // Obtener personal presente consolidado
            var personalPresente = await _context.PersonalPresente
                .Include(p => p.Empleado)
                .Where(p => kardexIds.Contains(p.KardexId) &&
                            (p.TipoKardex == TipoKardex.CocinaFria ||
                            p.TipoKardex == TipoKardex.CocinaCaliente ||
                            p.TipoKardex == TipoKardex.Parrilla))
                .ToListAsync();

            viewModel.PersonalPresenteTotal = personalPresente
                .GroupBy(p => p.EmpleadoId)
                .Select(g => g.First())
                .Select(p => new EmpleadoPresenteDto
                {
                    EmpleadoId = p.EmpleadoId,
                    NombreCompleto = p.Empleado?.NombreCompleto ?? "",
                    Rol = _userManager.GetRolesAsync(p.Empleado).Result.FirstOrDefault() ?? "",
                    EsResponsablePrincipal = p.EsResponsablePrincipal,
                    FechaRegistro = p.FechaRegistro
                })
                .OrderByDescending(e => e.EsResponsablePrincipal)
                .ThenBy(e => e.NombreCompleto)
                .ToList();

            _logger.LogInformation($"✅ Kardex consolidado obtenido: {viewModel.TotalProductos} productos");

            return viewModel;
        }

        /// <summary>
        /// Obtener kardex de salón para revisión
        /// </summary>
        public async Task<KardexSalonRevisionViewModel> ObtenerKardexSalonParaRevisionAsync(int kardexId)
        {
            _logger.LogInformation($"🔍 Obteniendo kardex de salón para revisión: {kardexId}");

            var kardex = await _context.KardexSalon
                .Include(k => k.Empleado)
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Utensilio)
                        .ThenInclude(u => u.Categoria)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            var viewModel = new KardexSalonRevisionViewModel
            {
                Id = kardex.Id,
                Fecha = kardex.Fecha,
                LocalId = kardex.LocalId,
                EmpleadoId = kardex.EmpleadoId,
                EmpleadoNombre = kardex.Empleado?.NombreCompleto ?? "",
                Estado = kardex.Estado,
                FechaEnvio = kardex.FechaEnvio,
                DescripcionFaltantes = kardex.DescripcionFaltantes,
                Observaciones = kardex.Observaciones
            };

            // Mapear detalles
            viewModel.Detalles = kardex.Detalles
                .OrderBy(d => d.Orden)
                .Select(d => new KardexSalonDetalleViewModel
                {
                    Id = d.Id,
                    UtensilioId = d.UtensilioId,
                    Codigo = d.Utensilio?.Codigo ?? "",
                    Descripcion = d.Utensilio?.Nombre ?? "",
                    Unidad = d.Utensilio?.Unidad ?? "",
                    InventarioInicial = d.InventarioInicial,
                    UnidadesContadas = d.UnidadesContadas,
                    Diferencia = d.Diferencia,
                    TieneFaltantes = d.TieneFaltantes,
                    Observaciones = d.Observaciones,
                    Orden = d.Orden,
                    EstaCompleto = d.UnidadesContadas.HasValue
                })
                .ToList();

            // Obtener personal presente
            var personalPresente = await _context.PersonalPresente
                .Include(p => p.Empleado)
                .Where(p => p.KardexId == kardexId && p.TipoKardex == TipoKardex.MozoSalon)
                .ToListAsync();

            viewModel.PersonalPresente = personalPresente
                .Select(p => new EmpleadoPresenteDto
                {
                    EmpleadoId = p.EmpleadoId,
                    NombreCompleto = p.Empleado?.NombreCompleto ?? "",
                    Rol = _userManager.GetRolesAsync(p.Empleado).Result.FirstOrDefault() ?? "",
                    EsResponsablePrincipal = p.EsResponsablePrincipal,
                    FechaRegistro = p.FechaRegistro
                })
                .OrderByDescending(e => e.EsResponsablePrincipal)
                .ThenBy(e => e.NombreCompleto)
                .ToList();

            // Estadísticas
            viewModel.TotalUtensilios = viewModel.Detalles.Count;
            viewModel.UtensiliosConFaltantes = viewModel.Detalles.Count(d => d.TieneFaltantes);
            viewModel.TotalFaltantes = viewModel.Detalles.Where(d => d.TieneFaltantes).Sum(d => Math.Abs(d.Diferencia));

            _logger.LogInformation($"✅ Kardex de salón obtenido para revisión");

            return viewModel;
        }

        /// <summary>
        /// Obtener kardex de bebidas para revisión
        /// </summary>
        public async Task<KardexBebidasRevisionViewModel> ObtenerKardexBebidasParaRevisionAsync(int kardexId)
        {
            _logger.LogInformation($"🔍 Obteniendo kardex de bebidas para revisión: {kardexId}");

            var kardex = await _context.KardexBebidas
                .Include(k => k.Empleado)
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            var viewModel = new KardexBebidasRevisionViewModel
            {
                Id = kardex.Id,
                Fecha = kardex.Fecha,
                LocalId = kardex.LocalId,
                EmpleadoId = kardex.EmpleadoId,
                EmpleadoNombre = kardex.Empleado?.NombreCompleto ?? "",
                Estado = kardex.Estado,
                FechaEnvio = kardex.FechaEnvio,
                Observaciones = kardex.Observaciones
            };

            // Mapear detalles
            viewModel.Detalles = kardex.Detalles
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

            // Obtener personal presente
            var personalPresente = await _context.PersonalPresente
                .Include(p => p.Empleado)
                .Where(p => p.KardexId == kardexId && p.TipoKardex == TipoKardex.MozoBebidas)
                .ToListAsync();

            viewModel.PersonalPresente = personalPresente
                .Select(p => new EmpleadoPresenteDto
                {
                    EmpleadoId = p.EmpleadoId,
                    NombreCompleto = p.Empleado?.NombreCompleto ?? "",
                    Rol = _userManager.GetRolesAsync(p.Empleado).Result.FirstOrDefault() ?? "",
                    EsResponsablePrincipal = p.EsResponsablePrincipal,
                    FechaRegistro = p.FechaRegistro
                })
                .OrderByDescending(e => e.EsResponsablePrincipal)
                .ThenBy(e => e.NombreCompleto)
                .ToList();

            // Estadísticas
            viewModel.TotalProductos = viewModel.Detalles.Count;
            viewModel.ProductosConDiferencia = viewModel.Detalles.Count(d => d.TieneDiferenciaSignificativa);

            _logger.LogInformation($"✅ Kardex de bebidas obtenido para revisión");

            return viewModel;
        }

        /// <summary>
        /// Obtener kardex de vajilla para revisión
        /// </summary>
        public async Task<KardexVajillaRevisionViewModel> ObtenerKardexVajillaParaRevisionAsync(int kardexId)
        {
            _logger.LogInformation($"🔍 Obteniendo kardex de vajilla para revisión: {kardexId}");

            var kardex = await _context.KardexVajilla
                .Include(k => k.Empleado)
                .Include(k => k.Detalles)
                    .ThenInclude(d => d.Utensilio)
                        .ThenInclude(u => u.Categoria)
                .FirstOrDefaultAsync(k => k.Id == kardexId);

            if (kardex == null)
            {
                throw new Exception("Kardex no encontrado");
            }

            var viewModel = new KardexVajillaRevisionViewModel
            {
                Id = kardex.Id,
                Fecha = kardex.Fecha,
                LocalId = kardex.LocalId,
                EmpleadoId = kardex.EmpleadoId,
                EmpleadoNombre = kardex.Empleado?.NombreCompleto ?? "",
                Estado = kardex.Estado,
                FechaEnvio = kardex.FechaEnvio,
                DescripcionFaltantes = kardex.DescripcionFaltantes,
                CantidadRotos = kardex.CantidadRotos,
                CantidadExtraviados = kardex.CantidadExtraviados,
                Observaciones = kardex.Observaciones
            };

            // Mapear detalles
            viewModel.Detalles = kardex.Detalles
                .OrderBy(d => d.Orden)
                .Select(d => new KardexVajillaDetalleViewModel
                {
                    Id = d.Id,
                    UtensilioId = d.UtensilioId,
                    Categoria = d.Utensilio?.Categoria?.Nombre ?? "",
                    Codigo = d.Utensilio?.Codigo ?? "",
                    Descripcion = d.Utensilio?.Nombre ?? "",
                    Unidad = d.Utensilio?.Unidad ?? "",
                    InventarioInicial = d.InventarioInicial,
                    UnidadesContadas = d.UnidadesContadas,
                    Diferencia = d.Diferencia,
                    TieneFaltantes = d.TieneFaltantes,
                    Observaciones = d.Observaciones,
                    Orden = d.Orden,
                    EstaCompleto = d.UnidadesContadas.HasValue
                })
                .ToList();

            // Obtener personal presente
            var personalPresente = await _context.PersonalPresente
                .Include(p => p.Empleado)
                .Where(p => p.KardexId == kardexId && p.TipoKardex == TipoKardex.Vajilla)
                .ToListAsync();

            viewModel.PersonalPresente = personalPresente
                .Select(p => new EmpleadoPresenteDto
                {
                    EmpleadoId = p.EmpleadoId,
                    NombreCompleto = p.Empleado?.NombreCompleto ?? "",
                    Rol = _userManager.GetRolesAsync(p.Empleado).Result.FirstOrDefault() ?? "",
                    EsResponsablePrincipal = p.EsResponsablePrincipal,
                    FechaRegistro = p.FechaRegistro
                })
                .OrderByDescending(e => e.EsResponsablePrincipal)
                .ThenBy(e => e.NombreCompleto)
                .ToList();

            // Estadísticas
            viewModel.TotalUtensilios = viewModel.Detalles.Count;
            viewModel.UtensiliosConFaltantes = viewModel.Detalles.Count(d => d.TieneFaltantes);
            viewModel.TotalFaltantes = viewModel.Detalles.Where(d => d.TieneFaltantes).Sum(d => Math.Abs(d.Diferencia));

            _logger.LogInformation($"✅ Kardex de vajilla obtenido para revisión");

            return viewModel;
        }

        // ==========================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ==========================================

        /// <summary>
        /// Determinar si una categoría es específica de un tipo de cocina
        /// </summary>
        private bool EsCategoriaEspecial(string nombreCategoria)
        {
            var categoriasEspeciales = new[]
            {
                "FRÍOS Y CEVICHES",
                "CALIENTES Y ARROCES",
                "CROCANTES"
            };

            return categoriasEspeciales.Contains(nombreCategoria, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Obtener el tipo de cocina responsable de una categoría específica
        /// </summary>
        private string? ObtenerTipoCocinaResponsable(string nombreCategoria)
        {
            return nombreCategoria.ToUpper() switch
            {
                "FRÍOS Y CEVICHES" => "Cocina Fría",
                "CALIENTES Y ARROCES" => "Cocina Caliente",
                "CROCANTES" => "Parrilla",
                _ => null
            };
        }
    }
}
/// <summary>
/// Métodos de extensión para mapeo de kardex
/// </summary>
public static class KardexExtensions
{
    public static KardexCocinaIndividualDto? ToIndividualDto(this KardexCocina? kardex)
    {
        if (kardex == null) return null;
        
        return new KardexCocinaIndividualDto
        {
            Id = kardex.Id,
            TipoCocina = kardex.TipoCocina,
            EmpleadoId = kardex.EmpleadoId,
            EmpleadoNombre = kardex.Empleado?.NombreCompleto ?? "",
            Estado = kardex.Estado,
            FechaEnvio = kardex.FechaEnvio,
            Observaciones = kardex.Observaciones
        };
    }
}