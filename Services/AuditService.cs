using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.Helpers;
using System.Security.Claims;
using System.Text.Json;

namespace Puerto92.Services
{
    /// <summary>
    /// Implementación del servicio de auditoría
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Método principal para registrar acciones de auditoría
        /// </summary>
        public async Task RegistrarAccionAsync(
            string accion,
            string? descripcion = null,
            string? usuarioAfectado = null,
            string? datosAdicionales = null,
            string modulo = "Sistema",
            string resultado = "Exitoso",
            string nivelSeveridad = "Info")
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var usuarioActual = httpContext?.User?.Identity?.Name;
                var usuarioId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var ip = IPHelper.ObtenerDireccionIPReal(httpContext);

                var auditLog = new AuditLog
                {
                    Accion = accion,
                    Descripcion = descripcion,
                    UsuarioAccion = usuarioActual ?? "Sistema",
                    UsuarioId = usuarioId,
                    UsuarioAfectado = usuarioAfectado,
                    DireccionIP = ip,
                    FechaHora = DateTime.Now,
                    Resultado = resultado,
                    DatosAdicionales = datosAdicionales,
                    Modulo = modulo,
                    NivelSeveridad = nivelSeveridad
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Log también en el sistema de logging de ASP.NET Core
                _logger.LogInformation(
                    "[AUDIT] {Accion} | Usuario: {Usuario} | IP: {IP} | Resultado: {Resultado}",
                    accion, usuarioActual ?? "Sistema", ip, resultado);
            }
            catch (Exception ex)
            {
                // Si falla el registro de auditoría, loggear el error pero no detener la aplicación
                _logger.LogError(ex, "Error al registrar auditoría: {Accion}", accion);
            }
        }

        public async Task RegistrarLoginExitosoAsync(string nombreUsuario, string ip)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.LoginExitoso,
                descripcion: $"Usuario '{nombreUsuario}' inició sesión exitosamente desde IP {ip}",
                usuarioAfectado: nombreUsuario,
                datosAdicionales: JsonSerializer.Serialize(new { IP = ip, Timestamp = DateTime.Now }),
                modulo: "Autenticación",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarLoginFallidoAsync(string nombreUsuario, string ip, string motivo)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.LoginFallido,
                descripcion: $"Intento fallido de login para usuario '{nombreUsuario}' desde IP {ip}. Motivo: {motivo}",
                usuarioAfectado: nombreUsuario,
                datosAdicionales: JsonSerializer.Serialize(new { IP = ip, Motivo = motivo, Timestamp = DateTime.Now }),
                modulo: "Autenticación",
                resultado: ResultadoAuditoria.Fallido,
                nivelSeveridad: NivelSeveridad.Warning);
        }

        public async Task RegistrarCreacionUsuarioAsync(string usuarioCreado, string rol, string local)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.CrearUsuario,
                descripcion: $"Usuario '{usuarioCreado}' creado con rol '{rol}' en local '{local}'",
                usuarioAfectado: usuarioCreado,
                datosAdicionales: JsonSerializer.Serialize(new { 
                    UsuarioCreado = usuarioCreado, 
                    Rol = rol, 
                    Local = local,
                    FechaCreacion = DateTime.Now 
                }),
                modulo: "Usuarios",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarEdicionUsuarioAsync(string usuarioEditado, string cambiosRealizados)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.EditarUsuario,
                descripcion: $"Usuario '{usuarioEditado}' editado. Cambios: {cambiosRealizados}",
                usuarioAfectado: usuarioEditado,
                datosAdicionales: JsonSerializer.Serialize(new { 
                    UsuarioEditado = usuarioEditado, 
                    Cambios = cambiosRealizados,
                    FechaEdicion = DateTime.Now 
                }),
                modulo: "Usuarios",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarEliminacionUsuarioAsync(string usuarioEliminado)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.EliminarUsuario,
                descripcion: $"Usuario '{usuarioEliminado}' eliminado del sistema",
                usuarioAfectado: usuarioEliminado,
                datosAdicionales: JsonSerializer.Serialize(new { 
                    UsuarioEliminado = usuarioEliminado,
                    FechaEliminacion = DateTime.Now 
                }),
                modulo: "Usuarios",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Warning);
        }

        public async Task RegistrarCambioRolAsync(string usuario, string rolAnterior, string rolNuevo)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.CambiarRolUsuario,
                descripcion: $"Rol de usuario '{usuario}' cambiado de '{rolAnterior}' a '{rolNuevo}'",
                usuarioAfectado: usuario,
                datosAdicionales: JsonSerializer.Serialize(new { 
                    Usuario = usuario,
                    RolAnterior = rolAnterior, 
                    RolNuevo = rolNuevo,
                    FechaCambio = DateTime.Now 
                }),
                modulo: "Usuarios",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarResetPasswordAsync(string usuarioAfectado)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.ResetPassword,
                descripcion: $"Contraseña reseteada para usuario '{usuarioAfectado}'",
                usuarioAfectado: usuarioAfectado,
                datosAdicionales: JsonSerializer.Serialize(new { 
                    UsuarioAfectado = usuarioAfectado,
                    FechaReset = DateTime.Now 
                }),
                modulo: "Autenticación",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Warning);
        }

        public async Task RegistrarCambioPasswordAsync(string usuario)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.CambioPassword,
                descripcion: $"Usuario '{usuario}' cambió su contraseña",
                usuarioAfectado: usuario,
                datosAdicionales: JsonSerializer.Serialize(new { 
                    Usuario = usuario,
                    FechaCambio = DateTime.Now 
                }),
                modulo: "Autenticación",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarCreacionLocalAsync(string codigoLocal, string nombreLocal)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.CrearLocal,
                descripcion: $"Local '{nombreLocal}' ({codigoLocal}) creado",
                datosAdicionales: JsonSerializer.Serialize(new { 
                    Codigo = codigoLocal, 
                    Nombre = nombreLocal,
                    FechaCreacion = DateTime.Now 
                }),
                modulo: "Locales",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarEdicionLocalAsync(string codigoLocal, string nombreLocal, string cambios)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.EditarLocal,
                descripcion: $"Local '{nombreLocal}' ({codigoLocal}) editado. Cambios: {cambios}",
                datosAdicionales: JsonSerializer.Serialize(new { 
                    Codigo = codigoLocal, 
                    Nombre = nombreLocal,
                    Cambios = cambios,
                    FechaEdicion = DateTime.Now 
                }),
                modulo: "Locales",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarDesactivacionLocalAsync(string codigoLocal, string nombreLocal)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.DesactivarLocal,
                descripcion: $"Local '{nombreLocal}' ({codigoLocal}) desactivado",
                datosAdicionales: JsonSerializer.Serialize(new { 
                    Codigo = codigoLocal, 
                    Nombre = nombreLocal,
                    FechaDesactivacion = DateTime.Now 
                }),
                modulo: "Locales",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Warning);
        }

        public async Task RegistrarAccesoDenegadoAsync(string recurso, string motivo)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.AccesoDenegado,
                descripcion: $"Acceso denegado a '{recurso}'. Motivo: {motivo}",
                datosAdicionales: JsonSerializer.Serialize(new { 
                    Recurso = recurso, 
                    Motivo = motivo,
                    Timestamp = DateTime.Now 
                }),
                modulo: "Seguridad",
                resultado: ResultadoAuditoria.Denegado,
                nivelSeveridad: NivelSeveridad.Warning);
        }

        public async Task RegistrarErrorSistemaAsync(string error, string detalles)
        {
            await RegistrarAccionAsync(
                accion: AccionAuditoria.ErrorSistema,
                descripcion: $"Error del sistema: {error}",
                datosAdicionales: JsonSerializer.Serialize(new { 
                    Error = error, 
                    Detalles = detalles,
                    Timestamp = DateTime.Now 
                }),
                modulo: "Sistema",
                resultado: ResultadoAuditoria.Error,
                nivelSeveridad: NivelSeveridad.Error);
        }

        public async Task RegistrarEdicionUtensilioAsync(Utensilio utensilio)
{
    var cambios = JsonSerializer.Serialize(utensilio); // o solo las propiedades que cambian
    await RegistrarAccionAsync(
        accion: "EditarUtensilio",
        descripcion: $"Se editó el utensilio: {utensilio.Nombre} ({utensilio.Codigo})",
        datosAdicionales: cambios,
        modulo: "Utensilios",
        resultado: "Exitoso",
        nivelSeveridad: "Info"
    );
}

public async Task RegistrarCreacionUtensilioAsync(Utensilio utensilio)
{
    await RegistrarAccionAsync(
        accion: "Creación Utensilio",
        descripcion: $"Se creó el utensilio '{utensilio.Nombre}' (Código: {utensilio.Codigo}, Tipo: {utensilio.Tipo})",
        datosAdicionales: JsonSerializer.Serialize(utensilio),
        modulo: "Utensilios",
        resultado: "Exitoso",
        nivelSeveridad: "Info");
}

public async Task RegistrarDesactivacionUtensilioAsync(Utensilio utensilio)
{
    await RegistrarAccionAsync(
        accion: "Desactivación Utensilio",
        descripcion: $"Se desactivó el utensilio '{utensilio.Nombre}' (Código: {utensilio.Codigo}, Tipo: {utensilio.Tipo})",
        datosAdicionales: JsonSerializer.Serialize(utensilio),
        modulo: "Utensilios",
        resultado: "Exitoso",
        nivelSeveridad: "Warning");
}


public async Task RegistrarCreacionProductoAsync(Producto producto)
{
    await RegistrarAccionAsync(
        accion: "Creación Producto",
        descripcion: $"Se creó el producto '{producto.Nombre}' (Código: {producto.Codigo}, Categoría: {producto.Categoria})",
        datosAdicionales: JsonSerializer.Serialize(producto),
        modulo: "Productos",
        resultado: "Exitoso",
        nivelSeveridad: "Info");
}

// Registrar edición de producto
public async Task RegistrarEdicionProductoAsync(Producto producto)
{
    var cambios = JsonSerializer.Serialize(producto); // Puedes filtrar solo lo que cambia si quieres
    await RegistrarAccionAsync(
        accion: "Edición Producto",
        descripcion: $"Se editó el producto '{producto.Nombre}' (Código: {producto.Codigo}, Categoría: {producto.Categoria})",
        datosAdicionales: cambios,
        modulo: "Productos",
        resultado: "Exitoso",
        nivelSeveridad: "Info");
}

// Registrar desactivación de producto
public async Task RegistrarDesactivacionProductoAsync(Producto producto)
{
    await RegistrarAccionAsync(
        accion: "Desactivación Producto",
        descripcion: $"Se desactivó el producto '{producto.Nombre}' (Código: {producto.Codigo}, Categoría: {producto.Categoria})",
        datosAdicionales: JsonSerializer.Serialize(producto),
        modulo: "Productos",
        resultado: "Exitoso",
        nivelSeveridad: "Warning");
}




        /// <summary>
        /// Obtener la dirección IP del cliente
        /// </summary>
        private string ObtenerDireccionIP()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "0.0.0.0";

            // Intentar obtener la IP real si está detrás de un proxy
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                if (ips.Length > 0)
                    return ips[0].Trim();
            }

            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
            return remoteIp ?? "0.0.0.0";
        }
    }
    
}