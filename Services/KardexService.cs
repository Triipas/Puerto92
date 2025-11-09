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

        public KardexService(
            ApplicationDbContext context,
            ILogger<KardexService> logger,
            UserManager<Usuario> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _notificationService = notificationService;
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
            // TODO: Agregar casos para otros tipos de kardex

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

            // Obtener empleados activos del local con los roles permitidos
            var empleados = await _context.Users
                .Where(u => u.Activo && u.LocalId == localId)
                .ToListAsync();

            var empleadosDto = new List<EmpleadoDisponibleDto>();

            foreach (var empleado in empleados)
            {
                var roles = await _userManager.GetRolesAsync(empleado);
                var tieneRolPermitido = roles.Any(r => rolesPermitidos.Contains(r));

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

            // Ordenar: responsable primero, luego por nombre
            return empleadosDto
                .OrderByDescending(e => e.EsResponsablePrincipal)
                .ThenBy(e => e.NombreCompleto)
                .ToList();
        }

        public async Task<PersonalPresenteResponse> GuardarPersonalPresenteYCompletarAsync(PersonalPresenteRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try {
                // ⭐ VALIDAR HORARIO
                var horaActual = DateTime.Now.TimeOfDay;
                var horaLimite = new TimeSpan(17, 30, 0); // 5:30 PM
                var dentroDeHorario = horaActual < horaLimite;

                // TODO: Verificar si hay habilitación manual para este kardex
                var envioHabilitadoManualmente = false;

                if (!dentroDeHorario && !envioHabilitadoManualmente)
                {
                    return new PersonalPresenteResponse
                    {
                        Success = false,
                        Message = "Fuera de horario. El envío ha sido bloqueado. El horario límite de envío es 5:30 PM. Si necesita enviar este kardex, contacte al administrador para solicitar habilitación manual."
                    };
                }

                // Validar que hay al menos un empleado presente
                if (request.EmpleadosPresentes == null || request.EmpleadosPresentes.Count == 0)
                {
                    return new PersonalPresenteResponse
                    {
                        Success = false,
                        Message = "Debe seleccionar al menos un empleado presente"
                    };
                }

                // Obtener información del kardex
                string empleadoResponsableId = "";
                string empleadoResponsableNombre = "";
                int localId = 0;
                DateTime fechaKardex = DateTime.Today;

                if (request.TipoKardex == TipoKardex.MozoBebidas)
                {
                    var kardex = await _context.KardexBebidas
                        .Include(k => k.Empleado)
                        .Include(k => k.Asignacion)
                        .FirstOrDefaultAsync(k => k.Id == request.KardexId);

                    if (kardex == null)
                    {
                        throw new Exception("Kardex no encontrado");
                    }

                    empleadoResponsableId = kardex.EmpleadoId;
                    empleadoResponsableNombre = kardex.Empleado?.NombreCompleto ?? "";
                    localId = kardex.LocalId;
                    fechaKardex = kardex.Fecha;

                    // ⭐ CAMBIAR ESTADO A "ENVIADO"
                    kardex.Estado = EstadoKardex.Enviado;
                    kardex.FechaFinalizacion = DateTime.Now;
                    kardex.FechaEnvio = DateTime.Now;
                    kardex.Observaciones = request.ObservacionesKardex;

                    // Actualizar asignación
                    if (kardex.Asignacion != null)
                    {
                        kardex.Asignacion.Estado = EstadoAsignacion.Completada;
                    }
                }
                // TODO: Agregar casos para otros tipos de kardex

                // Eliminar registros anteriores de personal presente para este kardex
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

                _logger.LogInformation(
                    $"✅ Kardex ENVIADO al administrador: Kardex {request.KardexId} ({request.TipoKardex}) - {request.EmpleadosPresentes.Count} empleados - Enviado a las {DateTime.Now:HH:mm:ss}"
                );

                // ⭐ NUEVO: Buscar y notificar al administrador local
                _logger.LogInformation($"🔍 Buscando administrador local para Local ID: {localId}");
                
                // Query simplificada y más robusta
                var usuariosLocal = await _context.Users
                    .Where(u => u.LocalId == localId && u.Activo)
                    .ToListAsync();
                
                _logger.LogInformation($"📋 Total usuarios activos en el local: {usuariosLocal.Count}");

                Usuario? administradorLocal = null;
                
                foreach (var usuario in usuariosLocal)
                {
                    var roles = await _userManager.GetRolesAsync(usuario);
                    _logger.LogInformation($"   - Usuario: {usuario.NombreCompleto} | Roles: {string.Join(", ", roles)}");
                    
                    if (roles.Contains("Administrador Local"))
                    {
                        administradorLocal = usuario;
                        _logger.LogInformation($"✅ Administrador Local encontrado: {administradorLocal.NombreCompleto} (ID: {administradorLocal.Id})");
                        break;
                    }
                }

                if (administradorLocal != null)
                {
                    _logger.LogInformation($"📤 Creando notificación para administrador: {administradorLocal.NombreCompleto}");
                    
                    await _notificationService.CrearNotificacionKardexRecibidoAsync(
                        administradorId: administradorLocal.Id,
                        tipoKardex: request.TipoKardex,
                        empleadoResponsable: empleadoResponsableNombre,
                        fecha: fechaKardex
                    );

                    _logger.LogInformation(
                        $"🔔 Notificación enviada exitosamente al administrador: {administradorLocal.NombreCompleto} - Kardex {request.TipoKardex} de {empleadoResponsableNombre}"
                    );
                }
                else
                {
                    _logger.LogWarning($"⚠️ No se encontró administrador local para el local ID {localId}");
                    _logger.LogWarning($"⚠️ Usuarios revisados: {usuariosLocal.Count}");
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

                return new PersonalPresenteResponse
                {
                    Success = false,
                    Message = $"Error al enviar el kardex: {ex.Message}"
                };
            }
        }
    }
}