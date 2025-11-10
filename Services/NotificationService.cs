using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using System.Text.Json;

namespace Puerto92.Services
{
    /// <summary>
    /// Implementación del servicio de notificaciones
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Notificacion> CrearNotificacionAsync(
            string usuarioId,
            string tipo,
            string titulo,
            string mensaje,
            string? urlAccion = null,
            string? textoAccion = null,
            string icono = "bell",
            string color = "primary",
            string prioridad = "Media",
            bool mostrarPopup = true,
            string? datosAdicionales = null,
            DateTime? fechaExpiracion = null)
        {
            try
            {
                _logger.LogInformation($"🔔 Iniciando creación de notificación:");
                _logger.LogInformation($"   Usuario ID: {usuarioId}");
                _logger.LogInformation($"   Tipo: {tipo}");
                _logger.LogInformation($"   Título: {titulo}");
                
                var notificacion = new Notificacion
                {
                    UsuarioId = usuarioId,
                    Tipo = tipo,
                    Titulo = titulo,
                    Mensaje = mensaje,
                    UrlAccion = urlAccion,
                    TextoAccion = textoAccion,
                    Icono = icono,
                    Color = color,
                    Prioridad = prioridad,
                    MostrarPopup = mostrarPopup,
                    DatosAdicionales = datosAdicionales,
                    FechaExpiracion = fechaExpiracion,
                    FechaCreacion = DateTime.Now,
                    Leida = false
                };

                _context.Notificaciones.Add(notificacion);
                
                _logger.LogInformation($"💾 Guardando notificación en base de datos...");
                var cambiosGuardados = await _context.SaveChangesAsync();
                _logger.LogInformation($"✅ Cambios guardados: {cambiosGuardados}");

                _logger.LogInformation($"✅ Notificación creada exitosamente con ID: {notificacion.Id} para usuario {usuarioId}");

                return notificacion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al crear notificación para usuario {usuarioId}");
                _logger.LogError($"   Tipo: {tipo}, Título: {titulo}");
                _logger.LogError($"   Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<Notificacion> CrearNotificacionAsignacionKardexAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string? empleadosAdicionales = null,
            bool esResponsableCompartidas = false) // ⭐ NUEVO parámetro
        {
            var fechaFormateada = fecha.ToString("dd/MM/yyyy");
            var horaLimite = "5:30 PM";

            string mensaje;
            string titulo;

            if (tipoKardex.Contains("Cocina") && !string.IsNullOrEmpty(empleadosAdicionales))
            {
                titulo = $"Asignación de Kardex - {tipoKardex}";
                
                // ⭐ NUEVO: Mensaje diferenciado
                if (esResponsableCompartidas)
                {
                    mensaje = $"Has sido asignado como responsable del Kardex de {tipoKardex} para el {fechaFormateada}. " +
                            $"⭐ IMPORTANTE: También deberás llenar las categorías compartidas (Abarrotes, Verduras, Frutiver, Limpieza). " +
                            $"Junto a: {empleadosAdicionales}. " +
                            $"Recuerda enviar antes de las {horaLimite}.";
                }
                else
                {
                    mensaje = $"Has sido asignado como responsable del Kardex de {tipoKardex} para el {fechaFormateada}. " +
                            $"Solo deberás llenar tu categoría especial. " +
                            $"Junto a: {empleadosAdicionales}. " +
                            $"Recuerda enviar antes de las {horaLimite}.";
                }
            }
            else
            {
                titulo = $"Asignación de Kardex - {tipoKardex}";
                mensaje = $"Has sido asignado como responsable del Kardex de {tipoKardex} para el {fechaFormateada}. " +
                        $"Recuerda enviar antes de las {horaLimite}.";
            }

            var datosAdicionales = JsonSerializer.Serialize(new
            {
                TipoKardex = tipoKardex,
                Fecha = fechaFormateada,
                HoraLimite = horaLimite,
                EmpleadosAdicionales = empleadosAdicionales
            });

            return await CrearNotificacionAsync(
                usuarioId: usuarioId,
                tipo: TipoNotificacion.AsignacionKardex,
                titulo: titulo,
                mensaje: mensaje,
                urlAccion: "/Kardex/Registrar",
                textoAccion: "Ver Kardex",
                icono: "clipboard-list",
                color: ColorNotificacion.Primary,
                prioridad: PrioridadNotificacion.Alta,
                mostrarPopup: true,
                datosAdicionales: datosAdicionales,
                fechaExpiracion: fecha.AddDays(1)
            );
        }

        public async Task<Notificacion> CrearNotificacionReasignacionKardexAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string motivo)
        {
            var fechaFormateada = fecha.ToString("dd/MM/yyyy");

            var titulo = $"Reasignación de Kardex - {tipoKardex}";
            var mensaje = $"Has sido reasignado al Kardex de {tipoKardex} para el {fechaFormateada}. " +
                         $"Motivo: {motivo}. Recuerda enviar antes de las 5:30 PM.";

            var datosAdicionales = JsonSerializer.Serialize(new
            {
                TipoKardex = tipoKardex,
                Fecha = fechaFormateada,
                Motivo = motivo
            });

            return await CrearNotificacionAsync(
                usuarioId: usuarioId,
                tipo: TipoNotificacion.ReasignacionKardex,
                titulo: titulo,
                mensaje: mensaje,
                urlAccion: "/Kardex/Registrar",
                textoAccion: "Ver Kardex",
                icono: "arrows-rotate",
                color: ColorNotificacion.Warning,
                prioridad: PrioridadNotificacion.Alta,
                mostrarPopup: true,
                datosAdicionales: datosAdicionales,
                fechaExpiracion: fecha.AddDays(1)
            );
        }

        public async Task<Notificacion> CrearNotificacionCancelacionKardexAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string motivo)
        {
            var fechaFormateada = fecha.ToString("dd/MM/yyyy");

            var titulo = $"Asignación Cancelada - {tipoKardex}";
            var mensaje = $"Tu asignación del Kardex de {tipoKardex} para el {fechaFormateada} ha sido cancelada. " +
                         $"Motivo: {motivo}";

            var datosAdicionales = JsonSerializer.Serialize(new
            {
                TipoKardex = tipoKardex,
                Fecha = fechaFormateada,
                Motivo = motivo,
                AccionTomada = "Cancelación"
            });

            return await CrearNotificacionAsync(
                usuarioId: usuarioId,
                tipo: "CancelacionKardex",
                titulo: titulo,
                mensaje: mensaje,
                urlAccion: null,
                textoAccion: null,
                icono: "ban",
                color: ColorNotificacion.Warning,
                prioridad: PrioridadNotificacion.Media,
                mostrarPopup: true,
                datosAdicionales: datosAdicionales,
                fechaExpiracion: fecha.AddDays(1)
            );
        }

        public async Task<Notificacion> CrearNotificacionKardexReasignadoAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string nuevoResponsable,
            string motivo)
        {
            var fechaFormateada = fecha.ToString("dd/MM/yyyy");

            var titulo = $"Tu Kardex fue Reasignado - {tipoKardex}";
            var mensaje = $"Tu responsabilidad del Kardex de {tipoKardex} para el {fechaFormateada} " +
                         $"ha sido reasignada a {nuevoResponsable}. " +
                         $"Motivo: {motivo}";

            var datosAdicionales = JsonSerializer.Serialize(new
            {
                TipoKardex = tipoKardex,
                Fecha = fechaFormateada,
                NuevoResponsable = nuevoResponsable,
                Motivo = motivo,
                AccionTomada = "Reasignación"
            });

            return await CrearNotificacionAsync(
                usuarioId: usuarioId,
                tipo: TipoNotificacion.ReasignacionKardex,
                titulo: titulo,
                mensaje: mensaje,
                urlAccion: null,
                textoAccion: null,
                icono: "arrows-rotate",
                color: ColorNotificacion.Info,
                prioridad: PrioridadNotificacion.Media,
                mostrarPopup: true,
                datosAdicionales: datosAdicionales,
                fechaExpiracion: fecha.AddDays(1)
            );
        }

        public async Task<Notificacion> CrearNotificacionKardexRecibidoAsync(
            string administradorId,
            string tipoKardex,
            string empleadoResponsable,
            DateTime fecha)
        {
            var fechaFormateada = fecha.ToString("dd/MM/yyyy");
            var horaFormateada = DateTime.Now.ToString("hh:mm tt");

            var titulo = $"Nuevo Kardex Recibido - {tipoKardex}";
            var mensaje = $"{empleadoResponsable} ha enviado su Kardex de {tipoKardex} para el {fechaFormateada} a las {horaFormateada}. Pendiente de revisión.";

            var datosAdicionales = JsonSerializer.Serialize(new
            {
                TipoKardex = tipoKardex,
                EmpleadoResponsable = empleadoResponsable,
                Fecha = fechaFormateada,
                HoraEnvio = horaFormateada,
                EstadoPendiente = true
            });

            _logger.LogInformation($"📝 Creando notificación KardexRecibido para: {administradorId}");
            _logger.LogInformation($"   Título: {titulo}");
            _logger.LogInformation($"   Mensaje: {mensaje}");

            var notificacion = await CrearNotificacionAsync(
                usuarioId: administradorId,
                tipo: TipoNotificacion.KardexRecibido,
                titulo: titulo,
                mensaje: mensaje,
                urlAccion: "/Kardex/PendientesDeRevision",
                textoAccion: "Ver Pendientes",
                icono: "clipboard-check",
                color: ColorNotificacion.Success,
                prioridad: PrioridadNotificacion.Alta,
                mostrarPopup: true,
                datosAdicionales: datosAdicionales,
                fechaExpiracion: fecha.AddDays(7)
            );

            _logger.LogInformation($"✅ Notificación creada con ID: {notificacion.Id}");
            
            return notificacion;
        }

        public async Task<Notificacion> CrearNotificacionKardexAprobadoAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string? observaciones = null)
        {
            var fechaFormateada = fecha.ToString("dd/MM/yyyy");
            var horaFormateada = DateTime.Now.ToString("hh:mm tt");

            var titulo = $"✅ Kardex Aprobado - {tipoKardex}";
            var mensaje = $"Tu Kardex de {tipoKardex} para el {fechaFormateada} ha sido aprobado a las {horaFormateada}.";

            if (!string.IsNullOrEmpty(observaciones))
            {
                mensaje += $" Observaciones: {observaciones}";
            }

            var datosAdicionales = JsonSerializer.Serialize(new
            {
                TipoKardex = tipoKardex,
                Fecha = fechaFormateada,
                HoraAprobacion = horaFormateada,
                Observaciones = observaciones,
                Estado = "Aprobado"
            });

            _logger.LogInformation($"✅ Creando notificación de aprobación para: {usuarioId}");

            return await CrearNotificacionAsync(
                usuarioId: usuarioId,
                tipo: "KardexAprobado",
                titulo: titulo,
                mensaje: mensaje,
                urlAccion: null,
                textoAccion: null,
                icono: "circle-check",
                color: ColorNotificacion.Success,
                prioridad: PrioridadNotificacion.Alta,
                mostrarPopup: true,
                datosAdicionales: datosAdicionales,
                fechaExpiracion: fecha.AddDays(30)
            );
        }

        public async Task<Notificacion> CrearNotificacionKardexRechazadoAsync(
            string usuarioId,
            string tipoKardex,
            DateTime fecha,
            string motivo)
        {
            var fechaFormateada = fecha.ToString("dd/MM/yyyy");
            var horaFormateada = DateTime.Now.ToString("hh:mm tt");

            var titulo = $"❌ Kardex Rechazado - {tipoKardex}";
            var mensaje = $"Tu Kardex de {tipoKardex} para el {fechaFormateada} ha sido rechazado a las {horaFormateada}. " +
                         $"Motivo: {motivo}. Por favor, corrige y reenvía.";

            var datosAdicionales = JsonSerializer.Serialize(new
            {
                TipoKardex = tipoKardex,
                Fecha = fechaFormateada,
                HoraRechazo = horaFormateada,
                Motivo = motivo,
                Estado = "Rechazado"
            });

            _logger.LogInformation($"❌ Creando notificación de rechazo para: {usuarioId}");

            return await CrearNotificacionAsync(
                usuarioId: usuarioId,
                tipo: "KardexRechazado",
                titulo: titulo,
                mensaje: mensaje,
                urlAccion: "/Kardex/MiKardex",
                textoAccion: "Ver Mi Kardex",
                icono: "circle-xmark",
                color: ColorNotificacion.Danger,
                prioridad: PrioridadNotificacion.Alta,
                mostrarPopup: true,
                datosAdicionales: datosAdicionales,
                fechaExpiracion: fecha.AddDays(30)
            );
        }

        public async Task<List<Notificacion>> ObtenerNotificacionesUsuarioAsync(
            string usuarioId,
            bool soloNoLeidas = false,
            int cantidad = 10)
        {
            var query = _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId);

            if (soloNoLeidas)
            {
                query = query.Where(n => !n.Leida);
            }

            var notificaciones = await query
                .OrderByDescending(n => n.FechaCreacion)
                .Take(cantidad)
                .ToListAsync();

            return notificaciones;
        }

        public async Task<int> ContarNoLeidasAsync(string usuarioId)
        {
            return await _context.Notificaciones
                .CountAsync(n => n.UsuarioId == usuarioId && !n.Leida);
        }

        public async Task MarcarComoLeidaAsync(int notificacionId)
        {
            var notificacion = await _context.Notificaciones.FindAsync(notificacionId);

            if (notificacion != null && !notificacion.Leida)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Notificación {notificacionId} marcada como leída");
            }
        }

        public async Task MarcarTodasComoLeidasAsync(string usuarioId)
        {
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId && !n.Leida)
                .ToListAsync();

            foreach (var notificacion in notificaciones)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ {notificaciones.Count} notificación(es) marcadas como leídas para usuario {usuarioId}");
        }

        public async Task EliminarNotificacionAsync(int notificacionId)
        {
            var notificacion = await _context.Notificaciones.FindAsync(notificacionId);

            if (notificacion != null)
            {
                _context.Notificaciones.Remove(notificacion);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Notificación {notificacionId} eliminada");
            }
        }

        public async Task LimpiarNotificacionesExpiradasAsync()
        {
            var ahora = DateTime.Now;

            var notificacionesExpiradas = await _context.Notificaciones
                .Where(n => n.FechaExpiracion.HasValue && n.FechaExpiracion.Value < ahora)
                .ToListAsync();

            if (notificacionesExpiradas.Any())
            {
                _context.Notificaciones.RemoveRange(notificacionesExpiradas);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ {notificacionesExpiradas.Count} notificación(es) expiradas eliminadas");
            }
        }
    }
}