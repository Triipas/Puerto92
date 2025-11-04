using Puerto92.Models;

namespace Puerto92.Services
{
    /// <summary>
    /// Interfaz para el servicio de auditoría del sistema
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Registrar una acción de auditoría
        /// </summary>
        Task RegistrarAccionAsync(
            string accion,
            string? descripcion = null,
            string? usuarioAfectado = null,
            string? datosAdicionales = null,
            string modulo = "Sistema",
            string resultado = "Exitoso",
            string nivelSeveridad = "Info");

        /// <summary>
        /// Registrar un login exitoso
        /// </summary>
        Task RegistrarLoginExitosoAsync(string nombreUsuario, string ip);

        /// <summary>
        /// Registrar un login fallido
        /// </summary>
        Task RegistrarLoginFallidoAsync(string nombreUsuario, string ip, string motivo);

        /// <summary>
        /// Registrar creación de usuario
        /// </summary>
        Task RegistrarCreacionUsuarioAsync(string usuarioCreado, string rol, string local);

        /// <summary>
        /// Registrar edición de usuario
        /// </summary>
        Task RegistrarEdicionUsuarioAsync(string usuarioEditado, string cambiosRealizados);

        /// <summary>
        /// Registrar eliminación de usuario
        /// </summary>
        Task RegistrarEliminacionUsuarioAsync(string usuarioEliminado);

        /// <summary>
        /// Registrar cambio de rol
        /// </summary>
        Task RegistrarCambioRolAsync(string usuario, string rolAnterior, string rolNuevo);

        /// <summary>
        /// Registrar reset de contraseña
        /// </summary>
        Task RegistrarResetPasswordAsync(string usuarioAfectado);

        /// <summary>
        /// Registrar cambio de contraseña
        /// </summary>
        Task RegistrarCambioPasswordAsync(string usuario);

        /// <summary>
        /// Registrar creación de local
        /// </summary>
        Task RegistrarCreacionLocalAsync(string codigoLocal, string nombreLocal);

        /// <summary>
        /// Registrar edición de local
        /// </summary>
        Task RegistrarEdicionLocalAsync(string codigoLocal, string nombreLocal, string cambios);

        /// <summary>
        /// Registrar desactivación de local
        /// </summary>
        Task RegistrarDesactivacionLocalAsync(string codigoLocal, string nombreLocal);

        /// <summary>
        /// Registrar acceso denegado
        /// </summary>
        Task RegistrarAccesoDenegadoAsync(string recurso, string motivo);

        /// <summary>
        /// Registrar error del sistema
        /// </summary>
        Task RegistrarErrorSistemaAsync(string error, string detalles);

        /// <summary>
        /// Registrar creación de utensilio
        /// </summary>
        Task RegistrarCreacionUtensilioAsync(string codigoUtensilio, string nombreUtensilio, string tipo);

        /// <summary>
        /// Registrar edición de utensilio
        /// </summary>
        Task RegistrarEdicionUtensilioAsync(string codigoUtensilio, string nombreUtensilio, string cambios);

        /// <summary>
        /// Registrar desactivación de utensilio
        /// </summary>
        Task RegistrarDesactivacionUtensilioAsync(string codigoUtensilio, string nombreUtensilio);

        /// <summary>
        /// Registrar carga masiva de utensilios
        /// </summary>
        Task RegistrarCargaMasivaUtensiliosAsync(int cantidad, string resultado);

        // ⭐ NUEVO: Métodos para Productos
        
        /// <summary>
        /// Registrar creación de producto
        /// </summary>
        Task RegistrarCreacionProductoAsync(string codigoProducto, string nombreProducto, string categoriaNombre);

        /// <summary>
        /// Registrar edición de producto
        /// </summary>
        Task RegistrarEdicionProductoAsync(string codigoProducto, string nombreProducto, string cambios);

        /// <summary>
        /// Registrar desactivación de producto
        /// </summary>
        Task RegistrarDesactivacionProductoAsync(string codigoProducto, string nombreProducto, string motivo);

        /// <summary>
        /// Registrar carga masiva de productos
        /// </summary>
        Task RegistrarCargaMasivaProductosAsync(int cantidad, string resultado);
    }
}