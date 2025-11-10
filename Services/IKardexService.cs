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
        
        // Salón (Mozo Salón)
        Task<KardexSalonViewModel> IniciarKardexSalonAsync(int asignacionId, string usuarioId);
        Task<KardexSalonViewModel> ObtenerKardexSalonAsync(int kardexId);
        Task<bool> AutoguardarDetalleSalonAsync(AutoguardadoKardexSalonRequest request);
        Task<KardexSalonViewModel> CalcularYActualizarSalonAsync(int kardexId);
        Task<bool> CompletarKardexSalonAsync(int kardexId, string descripcionFaltantes, string observaciones);
        
        // TODO: Cocina
        // Kardex Cocina Fria:
        Task<KardexCocinaViewModel> IniciarKardexCocinaAsync(int asignacionId, string usuarioId);
        Task<KardexCocinaViewModel> ObtenerKardexCocinaAsync(int kardexId);
        Task<bool> AutoguardarDetalleCocinaAsync(AutoguardadoKardexCocinaRequest request);
        Task<KardexCocinaViewModel> CalcularYActualizarCocinaAsync(int kardexId);
        
        // TODO: Vajilla
        // Task<KardexVajillaViewModel> IniciarKardexVajillaAsync(int asignacionId, string usuarioId);

        // Personal Presente
        Task<PersonalPresenteViewModel> ObtenerPersonalPresenteAsync(int kardexId, string tipoKardex);
        Task<PersonalPresenteResponse> GuardarPersonalPresenteYCompletarAsync(PersonalPresenteRequest request);
        Task<List<EmpleadoDisponibleDto>> ObtenerEmpleadosDelAreaAsync(string tipoKardex, int localId, string empleadoResponsableId);
    }
}