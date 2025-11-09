using Puerto92.Models;
using Puerto92.ViewModels;

namespace Puerto92.Services
{
    /// <summary>
    /// Interfaz para servicios de kardex
    /// </summary>
    public interface IKardexService
    {
        // Común - Verificación de asignaciones
        Task<bool> TieneAsignacionActivaAsync(string usuarioId);
        Task<AsignacionKardex?> ObtenerAsignacionActivaAsync(string usuarioId);
        Task<MiKardexViewModel> ObtenerMiKardexAsync(string usuarioId);
        
        // Bebidas (Mozo Bebidas)
        Task<KardexBebidasViewModel> IniciarKardexBebidasAsync(int asignacionId, string usuarioId);
        Task<KardexBebidasViewModel> ObtenerKardexBebidasAsync(int kardexId);
        Task<bool> AutoguardarDetalleBebidasAsync(AutoguardadoKardexRequest request);
        Task<KardexBebidasViewModel> CalcularYActualizarBebidasAsync(int kardexId);
        Task<bool> CompletarKardexBebidasAsync(int kardexId, string observaciones);
        
        // TODO: Salón (Mozo Salón)
        // Task<KardexSalonViewModel> IniciarKardexSalonAsync(int asignacionId, string usuarioId);
        // Task<KardexSalonViewModel> ObtenerKardexSalonAsync(int kardexId);
        
        // TODO: Cocina
        // Task<KardexCocinaViewModel> IniciarKardexCocinaAsync(int asignacionId, string usuarioId);
        
        // TODO: Vajilla
        // Task<KardexVajillaViewModel> IniciarKardexVajillaAsync(int asignacionId, string usuarioId);
    }
}