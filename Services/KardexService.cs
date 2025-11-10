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
                    // ✅ ACTUALIZADO: Ya está implementado
                    await VerificarBorradorSalon(viewModel, asignacion.Id);
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
            // ⭐ NUEVO: Agregar caso para Mozo Salón
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
            // TODO: Agregar casos para otros tipos de kardex (Cocina, Vajilla)

            // ⭐ NUEVO: Verificar horario
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
    }
}