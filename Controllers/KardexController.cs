using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Puerto92.Services;
using Puerto92.ViewModels;
using Puerto92.Models;
using System.Security.Claims;

namespace Puerto92.Controllers
{
    [Authorize]
    public class KardexController : BaseController
    {
        private readonly IKardexService _kardexService;
        private readonly ILogger<KardexController> _logger;

        public KardexController(
            IKardexService kardexService,
            ILogger<KardexController> logger)
        {
            _kardexService = kardexService;
            _logger = logger;
        }

        // GET: Kardex/MiKardex
        public async Task<IActionResult> MiKardex()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = await _kardexService.ObtenerMiKardexAsync(usuarioId);

            return View(viewModel);
        }

        // ==========================================
        // KARDEX DE BEBIDAS (Mozo Bebidas)
        // ==========================================

        // GET: Kardex/IniciarBebidas?asignacionId=1
        public async Task<IActionResult> IniciarBebidas(int asignacionId)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // ⭐ Validar que la asignación sea de tipo "Mozo Bebidas"
                var asignacion = await _kardexService.ObtenerAsignacionActivaAsync(usuarioId);
                
                if (asignacion == null || asignacion.Id != asignacionId)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta acceder a asignación {asignacionId} sin autorización");
                    SetErrorMessage("No tienes autorización para acceder a esta asignación");
                    return RedirectToAction(nameof(MiKardex));
                }

                if (asignacion.TipoKardex != TipoKardex.MozoBebidas)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta iniciar kardex de bebidas con asignación de tipo {asignacion.TipoKardex}");
                    SetErrorMessage($"Esta asignación es de tipo '{asignacion.TipoKardex}', no de 'Mozo Bebidas'");
                    return RedirectToAction(nameof(MiKardex));
                }

                var kardex = await _kardexService.IniciarKardexBebidasAsync(asignacionId, usuarioId);
                
                return RedirectToAction(nameof(ConteoUbicacionBebidas), new { id = kardex.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar kardex de bebidas");
                SetErrorMessage("Error al iniciar el kardex. Por favor intente nuevamente.");
                return RedirectToAction(nameof(MiKardex));
            }
        }

        // GET: Kardex/ConteoUbicacionBebidas/1
        public async Task<IActionResult> ConteoUbicacionBebidas(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var kardex = await _kardexService.ObtenerKardexBebidasAsync(id);
                
                // ⭐ Validar que el kardex pertenece al usuario actual
                if (kardex.EmpleadoId != usuarioId)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta acceder a kardex {id} de otro usuario");
                    SetErrorMessage("No tienes autorización para acceder a este kardex");
                    return RedirectToAction(nameof(MiKardex));
                }
                
                return View(kardex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar kardex {id}");
                SetErrorMessage("Error al cargar el kardex");
                return RedirectToAction(nameof(MiKardex));
            }
        }

        // POST: Kardex/AutoguardarBebidas
        [HttpPost]
        public async Task<IActionResult> AutoguardarBebidas([FromBody] AutoguardadoKardexRequest request)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return JsonError("Usuario no autenticado");
            }

            try
            {
                // ⭐ Validar que el kardex pertenece al usuario
                var kardex = await _kardexService.ObtenerKardexBebidasAsync(request.KardexId);
                
                if (kardex.EmpleadoId != usuarioId)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta autoguardar kardex {request.KardexId} de otro usuario");
                    return JsonError("No autorizado");
                }

                var resultado = await _kardexService.AutoguardarDetalleBebidasAsync(request);
                
                if (resultado)
                {
                    return JsonSuccess("Guardado automático exitoso");
                }
                else
                {
                    return JsonError("Error en el guardado automático");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en autoguardado de bebidas");
                return JsonError("Error en el guardado automático");
            }
        }

        // POST: Kardex/CompletarBebidas
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletarBebidas(int id, string observaciones)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // ⭐ Validar que el kardex pertenece al usuario
                var kardex = await _kardexService.ObtenerKardexBebidasAsync(id);
                
                if (kardex.EmpleadoId != usuarioId)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta completar kardex {id} de otro usuario");
                    SetErrorMessage("No tienes autorización para completar este kardex");
                    return RedirectToAction(nameof(MiKardex));
                }

                await _kardexService.CompletarKardexBebidasAsync(id, observaciones);
                
                SetSuccessMessage("Kardex de bebidas completado exitosamente");
                return RedirectToAction(nameof(MiKardex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al completar kardex de bebidas");
                SetErrorMessage(ex.Message);
                return RedirectToAction(nameof(ConteoUbicacionBebidas), new { id });
            }
        }

        // GET: Kardex/RecalcularBebidas/1
        [HttpGet]
        public async Task<IActionResult> RecalcularBebidas(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return JsonError("Usuario no autenticado");
            }

            try
            {
                // ⭐ Validar que el kardex pertenece al usuario
                var kardex = await _kardexService.ObtenerKardexBebidasAsync(id);
                
                if (kardex.EmpleadoId != usuarioId)
                {
                    return JsonError("No autorizado");
                }

                kardex = await _kardexService.CalcularYActualizarBebidasAsync(id);
                
                return Json(new { success = true, data = kardex });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recalcular bebidas");
                return JsonError("Error al recalcular");
            }
        }

        // ==========================================
        // TODO: KARDEX DE SALÓN (Mozo Salón)
        // ==========================================

        // GET: Kardex/IniciarSalon?asignacionId=1
        public async Task<IActionResult> IniciarSalon(int asignacionId)
        {
            // TODO: Implementar cuando se cree el kardex de salón
            SetErrorMessage("El kardex de Mozo Salón estará disponible próximamente");
            return RedirectToAction(nameof(MiKardex));
        }

        // ==========================================
        // TODO: KARDEX DE COCINA
        // ==========================================

        // GET: Kardex/IniciarCocina?asignacionId=1
        public async Task<IActionResult> IniciarCocina(int asignacionId)
        {
            // TODO: Implementar cuando se cree el kardex de cocina
            SetErrorMessage("El kardex de Cocina estará disponible próximamente");
            return RedirectToAction(nameof(MiKardex));
        }

        // ==========================================
        // TODO: KARDEX DE VAJILLA
        // ==========================================

        // GET: Kardex/IniciarVajilla?asignacionId=1
        public async Task<IActionResult> IniciarVajilla(int asignacionId)
        {
            // TODO: Implementar cuando se cree el kardex de vajilla
            SetErrorMessage("El kardex de Vajilla estará disponible próximamente");
            return RedirectToAction(nameof(MiKardex));
        }
    }
}