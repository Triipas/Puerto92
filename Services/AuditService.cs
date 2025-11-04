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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
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
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    Error = error,
                    Detalles = detalles,
                    Timestamp = DateTime.Now
                }),
                modulo: "Sistema",
                resultado: ResultadoAuditoria.Error,
                nivelSeveridad: NivelSeveridad.Error);
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
        // Agregar estos métodos a AuditService.cs

        public async Task RegistrarCreacionUtensilioAsync(string codigoUtensilio, string nombreUtensilio, string tipo)
        {
            await RegistrarAccionAsync(
                accion: "Crear Utensilio",
                descripcion: $"Utensilio '{nombreUtensilio}' ({codigoUtensilio}) de tipo '{tipo}' creado",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    Codigo = codigoUtensilio,
                    Nombre = nombreUtensilio,
                    Tipo = tipo,
                    FechaCreacion = DateTime.Now
                }),
                modulo: "Catálogo de Utensilios",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarEdicionUtensilioAsync(string codigoUtensilio, string nombreUtensilio, string cambios)
        {
            await RegistrarAccionAsync(
                accion: "Editar Utensilio",
                descripcion: $"Utensilio '{nombreUtensilio}' ({codigoUtensilio}) editado. Cambios: {cambios}",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    Codigo = codigoUtensilio,
                    Nombre = nombreUtensilio,
                    Cambios = cambios,
                    FechaEdicion = DateTime.Now
                }),
                modulo: "Catálogo de Utensilios",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarDesactivacionUtensilioAsync(string codigoUtensilio, string nombreUtensilio)
        {
            await RegistrarAccionAsync(
                accion: "Desactivar Utensilio",
                descripcion: $"Utensilio '{nombreUtensilio}' ({codigoUtensilio}) desactivado del catálogo",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    Codigo = codigoUtensilio,
                    Nombre = nombreUtensilio,
                    FechaDesactivacion = DateTime.Now
                }),
                modulo: "Catálogo de Utensilios",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Warning);
        }

        public async Task RegistrarCargaMasivaUtensiliosAsync(int cantidad, string resultado)
        {
            await RegistrarAccionAsync(
                accion: "Carga Masiva de Utensilios",
                descripcion: $"Carga masiva completada: {cantidad} utensilios. {resultado}",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    Cantidad = cantidad,
                    Resultado = resultado,
                    FechaCarga = DateTime.Now
                }),
                modulo: "Catálogo de Utensilios",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        // ⭐ AGREGAR ESTOS MÉTODOS AL FINAL DE LA CLASE AuditService

        /// <summary>
        /// Registrar creación de producto
        /// </summary>
        public async Task RegistrarCreacionProductoAsync(string codigoProducto, string nombreProducto, string categoriaNombre)
        {
            await RegistrarAccionAsync(
                accion: "Crear Producto",
                descripcion: $"Producto '{nombreProducto}' ({codigoProducto}) de categoría '{categoriaNombre}' creado",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    Codigo = codigoProducto,
                    Nombre = nombreProducto,
                    Categoria = categoriaNombre,
                    FechaCreacion = DateTime.Now
                }),
                modulo: "Catálogo de Productos",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        /// <summary>
        /// Registrar edición de producto
        /// </summary>
        public async Task RegistrarEdicionProductoAsync(string codigoProducto, string nombreProducto, string cambios)
        {
            await RegistrarAccionAsync(
                accion: "Editar Producto",
                descripcion: $"Producto '{nombreProducto}' ({codigoProducto}) editado. Cambios: {cambios}",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    Codigo = codigoProducto,
                    Nombre = nombreProducto,
                    Cambios = cambios,
                    FechaEdicion = DateTime.Now
                }),
                modulo: "Catálogo de Productos",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        /// <summary>
        /// Registrar desactivación de producto
        /// </summary>
        public async Task RegistrarDesactivacionProductoAsync(string codigoProducto, string nombreProducto, string motivo)
        {
            await RegistrarAccionAsync(
                accion: "Desactivar Producto",
                descripcion: $"Producto '{nombreProducto}' ({codigoProducto}) desactivado del catálogo. Motivo: {motivo}",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    Codigo = codigoProducto,
                    Nombre = nombreProducto,
                    Motivo = motivo,
                    FechaDesactivacion = DateTime.Now
                }),
                modulo: "Catálogo de Productos",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Warning);
        }

        /// <summary>
        /// Registrar carga masiva de productos
        /// </summary>
        public async Task RegistrarCargaMasivaProductosAsync(int cantidad, string resultado)
        {
            await RegistrarAccionAsync(
                accion: "Carga Masiva de Productos",
                descripcion: $"Carga masiva completada: {cantidad} productos. {resultado}",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    Cantidad = cantidad,
                    Resultado = resultado,
                    FechaCarga = DateTime.Now
                }),
                modulo: "Catálogo de Productos",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        /// <summary>
        /// Registrar creación de proveedor
        /// </summary>
        public async Task RegistrarCreacionProveedorAsync(string rucProveedor, string nombreProveedor, string categoria)
        {
            await RegistrarAccionAsync(
                accion: "Crear Proveedor",
                descripcion: $"Proveedor '{nombreProveedor}' (RUC: {rucProveedor}) de categoría '{categoria}' creado",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    RUC = rucProveedor,
                    Nombre = nombreProveedor,
                    Categoria = categoria,
                    FechaCreacion = DateTime.Now
                }),
                modulo: "Catálogo de Proveedores",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        /// <summary>
        /// Registrar edición de proveedor
        /// </summary>
        public async Task RegistrarEdicionProveedorAsync(string rucProveedor, string nombreProveedor, string cambios)
        {
            await RegistrarAccionAsync(
                accion: "Editar Proveedor",
                descripcion: $"Proveedor '{nombreProveedor}' (RUC: {rucProveedor}) editado. Cambios: {cambios}",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    RUC = rucProveedor,
                    Nombre = nombreProveedor,
                    Cambios = cambios,
                    FechaEdicion = DateTime.Now
                }),
                modulo: "Catálogo de Proveedores",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        /// <summary>
        /// Registrar desactivación de proveedor
        /// </summary>
        public async Task RegistrarDesactivacionProveedorAsync(string rucProveedor, string nombreProveedor)
        {
            await RegistrarAccionAsync(
                accion: "Desactivar Proveedor",
                descripcion: $"Proveedor '{nombreProveedor}' (RUC: {rucProveedor}) desactivado del catálogo",
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    RUC = rucProveedor,
                    Nombre = nombreProveedor,
                    FechaDesactivacion = DateTime.Now
                }),
                modulo: "Catálogo de Proveedores",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Warning);
        }
    }
}