using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;
using Puerto92.Services;

namespace Puerto92.Controllers
{
    [Authorize(Roles = "Administrador Local")]
    public class AsignacionesController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AsignacionesController> _logger;
        private readonly IAuditService _auditService;

        public AsignacionesController(
            ApplicationDbContext context,
            ILogger<AsignacionesController> logger,
            IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        // GET: Asignaciones
        public async Task<IActionResult> Index(string? tipo = null, int? mes = null, int? anio = null)
        {
            // Valores por defecto
            tipo ??= TipoKardex.CocinaFria;
            mes ??= DateTime.Now.Month;
            anio ??= DateTime.Now.Year;

            // Validar tipo
            if (!TipoKardex.Todos.Contains(tipo))
            {
                tipo = TipoKardex.CocinaFria;
            }

            // Obtener el local del usuario actual
            var usuario = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Calcular primer y último día del mes
            var primerDia = new DateTime(anio.Value, mes.Value, 1);
            var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

            // Obtener asignaciones del mes
            var asignaciones = await _context.AsignacionesKardex
                .Include(a => a.Empleado)
                .Where(a => a.LocalId == usuario.LocalId &&
                           a.Fecha >= primerDia &&
                           a.Fecha <= ultimoDia &&
                           a.TipoKardex == tipo)
                .OrderBy(a => a.Fecha)
                .Select(a => new AsignacionViewModel
                {
                    Id = a.Id,
                    TipoKardex = a.TipoKardex,
                    Fecha = a.Fecha,
                    EmpleadoId = a.EmpleadoId,
                    EmpleadoNombre = a.Empleado!.NombreCompleto,
                    EmpleadoRol = "", // Se llenará después
                    Estado = a.Estado,
                    EsReasignacion = a.EsReasignacion,
                    EmpleadoOriginal = a.EmpleadoOriginal,
                    RegistroIniciado = a.RegistroIniciado,
                    NotificacionEnviada = a.NotificacionEnviada
                })
                .ToListAsync();

            // Obtener estadísticas
            var estadisticas = await ObtenerEstadisticas(usuario.LocalId, mes.Value, anio.Value);

            // Crear ViewModel
            var viewModel = new CalendarioAsignacionesViewModel
            {
                Mes = mes.Value,
                Anio = anio.Value,
                TipoKardexActual = tipo,
                Asignaciones = asignaciones,
                Estadisticas = estadisticas,
                PrimerDiaMes = primerDia,
                UltimoDiaMes = ultimoDia,
                TotalDiasMes = ultimoDia.Day,
                DiaSemanaInicio = (int)primerDia.DayOfWeek
            };

            ViewBag.TiposTodos = TipoKardex.Todos;

            return View(viewModel);
        }

        // GET: Asignaciones/GetEmpleadosDisponibles
        [HttpGet]
        public async Task<IActionResult> GetEmpleadosDisponibles(string tipoKardex, DateTime fecha)
        {
            var usuario = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            if (usuario == null)
            {
                return JsonError("Usuario no encontrado");
            }

            // Obtener roles permitidos para el tipo de kardex
            var rolesPermitidos = TipoKardex.ObtenerRolesPermitidos(tipoKardex);

            // Obtener empleados del local con los roles permitidos
            var empleadosQuery = _context.Users
                .Where(u => u.LocalId == usuario.LocalId && u.Activo);

            var empleados = new List<EmpleadoDisponibleViewModel>();

            foreach (var emp in await empleadosQuery.ToListAsync())
            {
                var roles = await _context.UserRoles
                    .Where(ur => ur.UserId == emp.Id)
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync();

                // Verificar si tiene alguno de los roles permitidos
                if (roles.Any(r => rolesPermitidos.Contains(r ?? "")))
                {
                    // Verificar si ya tiene asignación ese día
                    var tieneAsignacion = await _context.AsignacionesKardex
                        .AnyAsync(a => a.EmpleadoId == emp.Id &&
                                      a.Fecha.Date == fecha.Date &&
                                      a.LocalId == usuario.LocalId);

                    empleados.Add(new EmpleadoDisponibleViewModel
                    {
                        Id = emp.Id,
                        NombreCompleto = emp.NombreCompleto,
                        UserName = emp.UserName!,
                        Rol = roles.FirstOrDefault() ?? "",
                        Disponible = !tieneAsignacion,
                        MotivoNoDisponible = tieneAsignacion ? "Ya tiene asignación este día" : null
                    });
                }
            }

            return Json(empleados.OrderBy(e => e.NombreCompleto));
        }

        // POST: Asignaciones/Asignar
        [HttpPost]
        public async Task<IActionResult> Asignar([FromBody] AsignacionViewModel model)
        {
            try
            {
                var usuario = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                if (usuario == null)
                {
                    return JsonError("Usuario no encontrado");
                }

                // Validar que el empleado no tenga asignación ese día
                var existeAsignacion = await _context.AsignacionesKardex
                    .AnyAsync(a => a.EmpleadoId == model.EmpleadoId &&
                                  a.Fecha.Date == model.Fecha.Date &&
                                  a.LocalId == usuario.LocalId);

                if (existeAsignacion)
                {
                    return JsonError("El empleado ya está asignado a otro kardex este día");
                }

                // Crear asignación
                var asignacion = new AsignacionKardex
                {
                    TipoKardex = model.TipoKardex,
                    Fecha = model.Fecha.Date,
                    EmpleadoId = model.EmpleadoId,
                    LocalId = usuario.LocalId,
                    Estado = EstadoAsignacion.Pendiente,
                    FechaCreacion = DateTime.Now,
                    CreadoPor = User.Identity!.Name
                };

                _context.AsignacionesKardex.Add(asignacion);
                await _context.SaveChangesAsync();

                // Obtener nombre del empleado para el log
                var empleado = await _context.Users.FindAsync(model.EmpleadoId);

                _logger.LogInformation($"Asignación creada: {model.TipoKardex} - {empleado?.NombreCompleto} - {model.Fecha:dd/MM/yyyy}");

                return JsonSuccess("Asignación agregada correctamente", new { asignacionId = asignacion.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear asignación");
                return JsonError("Error al crear la asignación");
            }
        }

        // POST: Asignaciones/GuardarAsignaciones
        [HttpPost]
        public async Task<IActionResult> GuardarAsignaciones([FromBody] List<int> asignacionesIds)
        {
            try
            {
                var usuario = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                if (usuario == null)
                {
                    return JsonError("Usuario no encontrado");
                }

                // Obtener asignaciones pendientes
                var asignaciones = await _context.AsignacionesKardex
                    .Include(a => a.Empleado)
                    .Where(a => asignacionesIds.Contains(a.Id) && a.Estado == EstadoAsignacion.Pendiente)
                    .ToListAsync();

                if (!asignaciones.Any())
                {
                    return JsonError("No hay asignaciones pendientes para guardar");
                }

                // Cambiar estado y enviar notificaciones
                foreach (var asignacion in asignaciones)
                {
                    asignacion.Estado = EstadoAsignacion.Asignada;
                    asignacion.FechaNotificacion = DateTime.Now;
                    asignacion.NotificacionEnviada = true;

                    // TODO: Implementar envío real de notificaciones (correo, push, etc.)
                    _logger.LogInformation($"Notificación enviada a {asignacion.Empleado?.NombreCompleto} para kardex {asignacion.TipoKardex} del {asignacion.Fecha:dd/MM/yyyy}");
                }

                await _context.SaveChangesAsync();

                // Registrar en auditoría
                await _auditService.RegistrarAccionAsync(
                    accion: "Guardar Asignaciones Kardex",
                    descripcion: $"Se guardaron {asignaciones.Count} asignación(es) y se notificó a los empleados",
                    modulo: "Asignaciones",
                    resultado: "Exitoso",
                    nivelSeveridad: "Info");

                return JsonSuccess($"{asignaciones.Count} asignación(es) confirmada(s) y notificada(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar asignaciones");
                return JsonError("Error al guardar las asignaciones");
            }
        }

        // POST: Asignaciones/Reasignar
        [HttpPost]
        public async Task<IActionResult> Reasignar([FromBody] ReasignacionViewModel model)
        {
            try
            {
                var usuario = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                if (usuario == null)
                {
                    return JsonError("Usuario no encontrado");
                }

                // Obtener asignación original
                var asignacionOriginal = await _context.AsignacionesKardex
                    .Include(a => a.Empleado)
                    .FirstOrDefaultAsync(a => a.Id == model.AsignacionId);

                if (asignacionOriginal == null)
                {
                    return JsonError("Asignación no encontrada");
                }

                // Validar que el nuevo empleado no tenga asignación ese día
                var existeAsignacion = await _context.AsignacionesKardex
                    .AnyAsync(a => a.EmpleadoId == model.NuevoEmpleadoId &&
                                  a.Fecha.Date == asignacionOriginal.Fecha.Date &&
                                  a.LocalId == usuario.LocalId &&
                                  a.Id != model.AsignacionId);

                if (existeAsignacion)
                {
                    return JsonError("El empleado ya está asignado a otro kardex este día");
                }

                // Guardar datos del empleado original
                var empleadoOriginalNombre = asignacionOriginal.Empleado?.NombreCompleto ?? "Desconocido";

                // Actualizar asignación
                asignacionOriginal.EmpleadoOriginal = empleadoOriginalNombre;
                asignacionOriginal.EmpleadoId = model.NuevoEmpleadoId;
                asignacionOriginal.EsReasignacion = true;
                asignacionOriginal.MotivoReasignacion = model.Motivo;
                asignacionOriginal.FechaReasignacion = DateTime.Now;
                asignacionOriginal.ReasignadoPor = User.Identity!.Name;
                asignacionOriginal.Estado = EstadoAsignacion.Asignada;
                asignacionOriginal.FechaNotificacion = DateTime.Now;
                asignacionOriginal.NotificacionEnviada = true;

                await _context.SaveChangesAsync();

                // Obtener nombre del nuevo empleado
                var nuevoEmpleado = await _context.Users.FindAsync(model.NuevoEmpleadoId);

                _logger.LogInformation($"Reasignación: {asignacionOriginal.TipoKardex} - De {empleadoOriginalNombre} a {nuevoEmpleado?.NombreCompleto} - {asignacionOriginal.Fecha:dd/MM/yyyy}");

                // Registrar en auditoría
                await _auditService.RegistrarAccionAsync(
                    accion: "Reasignar Kardex",
                    descripcion: $"Kardex {asignacionOriginal.TipoKardex} reasignado de {empleadoOriginalNombre} a {nuevoEmpleado?.NombreCompleto}. Motivo: {model.Motivo ?? "No especificado"}",
                    modulo: "Asignaciones",
                    resultado: "Exitoso",
                    nivelSeveridad: "Warning");

                return JsonSuccess("Reasignación realizada exitosamente", new { 
                    empleadoNuevo = nuevoEmpleado?.NombreCompleto 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reasignar");
                return JsonError("Error al realizar la reasignación");
            }
        }

        // DELETE: Asignaciones/EliminarAsignacion
        [HttpPost]
        public async Task<IActionResult> EliminarAsignacion(int id)
        {
            try
            {
                var asignacion = await _context.AsignacionesKardex
                    .Include(a => a.Empleado)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (asignacion == null)
                {
                    return JsonError("Asignación no encontrada");
                }

                // Solo permitir eliminar asignaciones pendientes
                if (asignacion.Estado != EstadoAsignacion.Pendiente)
                {
                    return JsonError("Solo se pueden eliminar asignaciones pendientes");
                }

                _context.AsignacionesKardex.Remove(asignacion);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Asignación eliminada: {asignacion.TipoKardex} - {asignacion.Empleado?.NombreCompleto} - {asignacion.Fecha:dd/MM/yyyy}");

                return JsonSuccess("Asignación eliminada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar asignación");
                return JsonError("Error al eliminar la asignación");
            }
        }

        // Método auxiliar para obtener estadísticas
        private async Task<Dictionary<string, int>> ObtenerEstadisticas(int localId, int mes, int anio)
        {
            var primerDia = new DateTime(anio, mes, 1);
            var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

            var asignaciones = await _context.AsignacionesKardex
                .Where(a => a.LocalId == localId &&
                           a.Fecha >= primerDia &&
                           a.Fecha <= ultimoDia)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                ["Pendientes"] = asignaciones.Count(a => a.Estado == EstadoAsignacion.Pendiente),
                ["Asignadas"] = asignaciones.Count(a => a.Estado == EstadoAsignacion.Asignada),
                ["EnProceso"] = asignaciones.Count(a => a.Estado == EstadoAsignacion.EnProceso),
                ["Completadas"] = asignaciones.Count(a => a.Estado == EstadoAsignacion.Completada),
                ["Reasignaciones"] = asignaciones.Count(a => a.EsReasignacion)
            };
        }
    }
}