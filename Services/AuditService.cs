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

        public async Task RegistrarCreacionAsignacionAsync(string tipoKardex, DateTime fecha, string empleadoNombre, string localNombre)
        {
            await RegistrarAccionAsync(
                accion: "Crear Asignación Kardex",
                descripcion: $"Asignación de {tipoKardex} creada para {empleadoNombre} en {localNombre} para el {fecha:dd/MM/yyyy}",
                usuarioAfectado: empleadoNombre,
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    TipoKardex = tipoKardex,
                    Fecha = fecha.ToString("dd/MM/yyyy"),
                    Empleado = empleadoNombre,
                    Local = localNombre,
                    FechaCreacion = DateTime.Now
                }),
                modulo: "Asignaciones",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarEliminacionAsignacionAsync(string tipoKardex, DateTime fecha, string empleadoNombre, string motivo)
        {
            await RegistrarAccionAsync(
                accion: "Eliminar Asignación Kardex",
                descripcion: $"Asignación de {tipoKardex} para {empleadoNombre} del {fecha:dd/MM/yyyy} eliminada. Motivo: {motivo}",
                usuarioAfectado: empleadoNombre,
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    TipoKardex = tipoKardex,
                    Fecha = fecha.ToString("dd/MM/yyyy"),
                    Empleado = empleadoNombre,
                    Motivo = motivo,
                    FechaEliminacion = DateTime.Now
                }),
                modulo: "Asignaciones",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Warning);
        }

        public async Task RegistrarInicioKardexAsync(string tipoKardex, DateTime fecha, string empleadoNombre, int kardexId)
        {
            await RegistrarAccionAsync(
                accion: "Iniciar Kardex",
                descripcion: $"{empleadoNombre} inició el registro del Kardex de {tipoKardex} del {fecha:dd/MM/yyyy} (ID: {kardexId})",
                usuarioAfectado: empleadoNombre,
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    KardexId = kardexId,
                    TipoKardex = tipoKardex,
                    Fecha = fecha.ToString("dd/MM/yyyy"),
                    Empleado = empleadoNombre,
                    FechaInicio = DateTime.Now
                }),
                modulo: "Kardex",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarCompletadoKardexAsync(string tipoKardex, DateTime fecha, string empleadoNombre, int kardexId)
        {
            await RegistrarAccionAsync(
                accion: "Completar Kardex",
                descripcion: $"{empleadoNombre} completó el Kardex de {tipoKardex} del {fecha:dd/MM/yyyy} (ID: {kardexId})",
                usuarioAfectado: empleadoNombre,
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    KardexId = kardexId,
                    TipoKardex = tipoKardex,
                    Fecha = fecha.ToString("dd/MM/yyyy"),
                    Empleado = empleadoNombre,
                    FechaCompletado = DateTime.Now
                }),
                modulo: "Kardex",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarEnvioKardexAsync(string tipoKardex, DateTime fecha, string empleadoNombre, int kardexId, int totalPersonalPresente)
        {
            await RegistrarAccionAsync(
                accion: "Enviar Kardex al Administrador",
                descripcion: $"{empleadoNombre} envió el Kardex de {tipoKardex} del {fecha:dd/MM/yyyy} al administrador. Personal presente: {totalPersonalPresente} empleado(s). (ID: {kardexId})",
                usuarioAfectado: empleadoNombre,
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    KardexId = kardexId,
                    TipoKardex = tipoKardex,
                    Fecha = fecha.ToString("dd/MM/yyyy"),
                    Empleado = empleadoNombre,
                    TotalPersonalPresente = totalPersonalPresente,
                    FechaEnvio = DateTime.Now
                }),
                modulo: "Kardex",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarAprobacionKardexAsync(string tipoKardex, DateTime fecha, string empleadoResponsable, int kardexId, string administrador)
        {
            await RegistrarAccionAsync(
                accion: "Aprobar Kardex",
                descripcion: $"{administrador} aprobó el Kardex de {tipoKardex} de {empleadoResponsable} del {fecha:dd/MM/yyyy} (ID: {kardexId})",
                usuarioAfectado: empleadoResponsable,
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    KardexId = kardexId,
                    TipoKardex = tipoKardex,
                    Fecha = fecha.ToString("dd/MM/yyyy"),
                    EmpleadoResponsable = empleadoResponsable,
                    Administrador = administrador,
                    FechaAprobacion = DateTime.Now
                }),
                modulo: "Kardex",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Info);
        }

        public async Task RegistrarRechazoKardexAsync(string tipoKardex, DateTime fecha, string empleadoResponsable, int kardexId, string administrador, string motivo)
        {
            await RegistrarAccionAsync(
                accion: "Rechazar Kardex",
                descripcion: $"{administrador} rechazó el Kardex de {tipoKardex} de {empleadoResponsable} del {fecha:dd/MM/yyyy}. Motivo: {motivo} (ID: {kardexId})",
                usuarioAfectado: empleadoResponsable,
                datosAdicionales: JsonSerializer.Serialize(new
                {
                    KardexId = kardexId,
                    TipoKardex = tipoKardex,
                    Fecha = fecha.ToString("dd/MM/yyyy"),
                    EmpleadoResponsable = empleadoResponsable,
                    Administrador = administrador,
                    Motivo = motivo,
                    FechaRechazo = DateTime.Now
                }),
                modulo: "Kardex",
                resultado: ResultadoAuditoria.Exitoso,
                nivelSeveridad: NivelSeveridad.Warning);
        }
    }
}