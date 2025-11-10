using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Puerto92.Services;
using Puerto92.ViewModels;
using Puerto92.Models;
using System.Security.Claims;
using System.Net;
using Puerto92.Data;

namespace Puerto92.Controllers
{
    [Authorize]
    public class KardexController : BaseController
    {
        private readonly IKardexService _kardexService;
        private readonly ILogger<KardexController> _logger;
        private readonly ApplicationDbContext _context;

        public KardexController(
            ApplicationDbContext context,
            IKardexService kardexService,
            ILogger<KardexController> logger)
        {
            _context = context;
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
        // KARDEX DE SALÓN (Mozo Salón)
        // ==========================================

        // GET: Kardex/IniciarSalon?asignacionId=1
        public async Task<IActionResult> IniciarSalon(int asignacionId)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var asignacion = await _kardexService.ObtenerAsignacionActivaAsync(usuarioId);
                
                if (asignacion == null || asignacion.Id != asignacionId)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta acceder a asignación {asignacionId} sin autorización");
                    SetErrorMessage("No tienes autorización para acceder a esta asignación");
                    return RedirectToAction(nameof(MiKardex));
                }

                if (asignacion.TipoKardex != TipoKardex.MozoSalon)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta iniciar kardex de salón con asignación de tipo {asignacion.TipoKardex}");
                    SetErrorMessage($"Esta asignación es de tipo '{asignacion.TipoKardex}', no de 'Mozo Salón'");
                    return RedirectToAction(nameof(MiKardex));
                }

                var kardex = await _kardexService.IniciarKardexSalonAsync(asignacionId, usuarioId);
                
                return RedirectToAction(nameof(ConteoSalon), new { id = kardex.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar kardex de salón");
                SetErrorMessage("Error al iniciar el kardex. Por favor intente nuevamente.");
                return RedirectToAction(nameof(MiKardex));
            }
        }

        // GET: Kardex/ConteoSalon/1
        public async Task<IActionResult> ConteoSalon(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var kardex = await _kardexService.ObtenerKardexSalonAsync(id);
                
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

        // POST: Kardex/AutoguardarSalon
        [HttpPost]
        public async Task<IActionResult> AutoguardarSalon([FromBody] AutoguardadoKardexSalonRequest request)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return JsonError("Usuario no autenticado");
            }

            try
            {
                var kardex = await _kardexService.ObtenerKardexSalonAsync(request.KardexId);
                
                if (kardex.EmpleadoId != usuarioId)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta autoguardar kardex {request.KardexId} de otro usuario");
                    return JsonError("No autorizado");
                }

                var resultado = await _kardexService.AutoguardarDetalleSalonAsync(request);
                
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
                _logger.LogError(ex, "Error en autoguardado de salón");
                return JsonError("Error en el guardado automático");
            }
        }

        // GET: Kardex/RecalcularSalon/1
        [HttpGet]
        public async Task<IActionResult> RecalcularSalon(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return JsonError("Usuario no autenticado");
            }

            try
            {
                var kardex = await _kardexService.ObtenerKardexSalonAsync(id);
                
                if (kardex.EmpleadoId != usuarioId)
                {
                    return JsonError("No autorizado");
                }

                kardex = await _kardexService.CalcularYActualizarSalonAsync(id);
                
                return Json(new { success = true, data = kardex });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recalcular salón");
                return JsonError("Error al recalcular");
            }
        }

        // ==========================================
        // TODO: KARDEX DE COCINA
        // ==========================================

        // GET: Kardex/IniciarCocina?asignacionId=1
        public async Task<IActionResult> IniciarCocina(int asignacionId)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var asignacion = await _kardexService.ObtenerAsignacionActivaAsync(usuarioId);
                
                if (asignacion == null || asignacion.Id != asignacionId)
                {
                    SetErrorMessage("No tienes autorización para acceder a esta asignación");
                    return RedirectToAction(nameof(MiKardex));
                }

                // Validar que sea un tipo de cocina válido
                var tiposCocinaValidos = new[] { TipoKardex.CocinaFria, TipoKardex.CocinaCaliente, TipoKardex.Parrilla };
                if (!tiposCocinaValidos.Contains(asignacion.TipoKardex))
                {
                    SetErrorMessage($"Esta asignación no es de cocina");
                    return RedirectToAction(nameof(MiKardex));
                }

                var kardex = await _kardexService.IniciarKardexCocinaAsync(asignacionId, usuarioId);
                
                return RedirectToAction(nameof(ConteoCocina), new { id = kardex.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar kardex de cocina");
                SetErrorMessage("Error al iniciar el kardex. Por favor intente nuevamente.");
                return RedirectToAction(nameof(MiKardex));
            }
        }

        // GET: Kardex/ConteoCocina/1
        public async Task<IActionResult> ConteoCocina(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var kardex = await _kardexService.ObtenerKardexCocinaAsync(id);
                
                if (kardex.EmpleadoId != usuarioId)
                {
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

        // POST: Kardex/AutoguardarCocina
        [HttpPost]
        public async Task<IActionResult> AutoguardarCocina([FromBody] AutoguardadoKardexCocinaRequest request)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return JsonError("Usuario no autenticado");
            }

            try
            {
                var kardex = await _kardexService.ObtenerKardexCocinaAsync(request.KardexId);
                
                if (kardex.EmpleadoId != usuarioId)
                {
                    return JsonError("No autorizado");
                }

                var resultado = await _kardexService.AutoguardarDetalleCocinaAsync(request);
                
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
                _logger.LogError(ex, "Error en autoguardado de cocina");
                return JsonError("Error en el guardado automático");
            }
        }

        // ==========================================
        // KARDEX DE VAJILLA (Vajillero)
        // ==========================================
        // ⭐ AGREGAR ESTOS MÉTODOS AL CONTROLADOR KardexController

        // GET: Kardex/IniciarVajilla?asignacionId=1
        public async Task<IActionResult> IniciarVajilla(int asignacionId)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var asignacion = await _kardexService.ObtenerAsignacionActivaAsync(usuarioId);

                if (asignacion == null || asignacion.Id != asignacionId)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta acceder a asignación {asignacionId} sin autorización");
                    SetErrorMessage("No tienes autorización para acceder a esta asignación");
                    return RedirectToAction(nameof(MiKardex));
                }

                if (asignacion.TipoKardex != TipoKardex.Vajilla)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta iniciar kardex de vajilla con asignación de tipo {asignacion.TipoKardex}");
                    SetErrorMessage($"Esta asignación es de tipo '{asignacion.TipoKardex}', no de 'Vajilla'");
                    return RedirectToAction(nameof(MiKardex));
                }

                var kardex = await _kardexService.IniciarKardexVajillaAsync(asignacionId, usuarioId);

                return RedirectToAction(nameof(ConteoVajilla), new { id = kardex.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar kardex de vajilla");
                SetErrorMessage("Error al iniciar el kardex. Por favor intente nuevamente.");
                return RedirectToAction(nameof(MiKardex));
            }
        }

        // GET: Kardex/ConteoVajilla/1
        public async Task<IActionResult> ConteoVajilla(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var kardex = await _kardexService.ObtenerKardexVajillaAsync(id);

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

        // POST: Kardex/AutoguardarVajilla
        [HttpPost]
        public async Task<IActionResult> AutoguardarVajilla([FromBody] AutoguardadoKardexVajillaRequest request)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return JsonError("Usuario no autenticado");
            }

            try
            {
                var kardex = await _kardexService.ObtenerKardexVajillaAsync(request.KardexId);

                if (kardex.EmpleadoId != usuarioId)
                {
                    _logger.LogWarning($"Usuario {usuarioId} intenta autoguardar kardex {request.KardexId} de otro usuario");
                    return JsonError("No autorizado");
                }

                var resultado = await _kardexService.AutoguardarDetalleVajillaAsync(request);

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
                _logger.LogError(ex, "Error en autoguardado de vajilla");
                return JsonError("Error en el guardado automático");
            }
        }

        // GET: Kardex/RecalcularVajilla/1
        [HttpGet]
        public async Task<IActionResult> RecalcularVajilla(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return JsonError("Usuario no autenticado");
            }

            try
            {
                var kardex = await _kardexService.ObtenerKardexVajillaAsync(id);

                if (kardex.EmpleadoId != usuarioId)
                {
                    return JsonError("No autorizado");
                }

                kardex = await _kardexService.CalcularYActualizarVajillaAsync(id);

                return Json(new { success = true, data = kardex });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recalcular vajilla");
                return JsonError("Error al recalcular");
            }
        }

        // ==========================================
        // PERSONAL PRESENTE
        // ==========================================

        /// <summary>
        /// GET: Mostrar pantalla de Personal Presente
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PersonalPresente(int id, string tipo)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                _logger.LogInformation($"🔍 PersonalPresente solicitado:");
                _logger.LogInformation($"   KardexId: {id}");
                _logger.LogInformation($"   Tipo: {tipo}");
                _logger.LogInformation($"   UsuarioId: {usuarioId}");

                // ⭐ DECODIFICAR EL TIPO (por si viene URL-encoded)
                var tipoDecodificado = System.Net.WebUtility.UrlDecode(tipo);
                _logger.LogInformation($"   Tipo Decodificado: {tipoDecodificado}");

                // Validar que el kardex pertenece al usuario
                if (tipoDecodificado == TipoKardex.MozoBebidas)
                {
                    var kardex = await _kardexService.ObtenerKardexBebidasAsync(id);

                    if (kardex.EmpleadoId != usuarioId)
                    {
                        _logger.LogWarning($"Usuario {usuarioId} intenta acceder a kardex {id} de otro usuario");
                        SetErrorMessage("No tienes autorización para acceder a este kardex");
                        return RedirectToAction(nameof(MiKardex));
                    }
                }
                else if (tipoDecodificado == TipoKardex.MozoSalon)
                {
                    var kardex = await _kardexService.ObtenerKardexSalonAsync(id);

                    if (kardex.EmpleadoId != usuarioId)
                    {
                        _logger.LogWarning($"Usuario {usuarioId} intenta acceder a kardex {id} de otro usuario");
                        SetErrorMessage("No tienes autorización para acceder a este kardex");
                        return RedirectToAction(nameof(MiKardex));
                    }
                }
                // ⭐ VALIDACIÓN para Cocina Fría, Caliente y Parrilla
                else if (tipoDecodificado == TipoKardex.CocinaFria ||
                        tipoDecodificado == TipoKardex.CocinaCaliente ||
                        tipoDecodificado == TipoKardex.Parrilla)
                {
                    var kardex = await _kardexService.ObtenerKardexCocinaAsync(id);

                    if (kardex.EmpleadoId != usuarioId)
                    {
                        _logger.LogWarning($"Usuario {usuarioId} intenta acceder a kardex {id} de otro usuario");
                        SetErrorMessage("No tienes autorización para acceder a este kardex");
                        return RedirectToAction(nameof(MiKardex));
                    }

                    _logger.LogInformation($"✅ Validación de usuario exitosa para Kardex de Cocina");
                }

                // ⭐ VALIDACIÓN PARA KARDEX DE VAJILLA
                else if (tipo == TipoKardex.Vajilla)
                {
                    var kardex = await _kardexService.ObtenerKardexVajillaAsync(id);
                    
                    if (kardex.EmpleadoId != usuarioId)
                    {
                        _logger.LogWarning($"Usuario {usuarioId} intenta acceder a personal presente de kardex {id} de otro usuario");
                        SetErrorMessage("No tienes autorización para acceder a este kardex");
                        return RedirectToAction(nameof(MiKardex));
                    }
                }

                // ⭐ USAR TIPO DECODIFICADO
                var viewModel = await _kardexService.ObtenerPersonalPresenteAsync(id, tipoDecodificado);
                
                _logger.LogInformation($"✅ ViewModel obtenido:");
                _logger.LogInformation($"   LocalId: {viewModel.LocalId}");
                _logger.LogInformation($"   Total Empleados: {viewModel.TotalEmpleados}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar personal presente para kardex {id}");
                _logger.LogError($"   Tipo: {tipo}");
                _logger.LogError($"   Error: {ex.Message}");
                SetErrorMessage($"Error al cargar la pantalla de personal presente: {ex.Message}");
                return RedirectToAction(nameof(MiKardex));
            }
        }

        /// <summary>
        /// POST: Guardar personal presente y completar kardex
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPersonalPresente([FromBody] PersonalPresenteRequest request)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            try
            {
                // Validar que el usuario sea el responsable del kardex
                if (request.TipoKardex == TipoKardex.MozoBebidas)
                {
                    var kardex = await _kardexService.ObtenerKardexBebidasAsync(request.KardexId);

                    if (kardex.EmpleadoId != usuarioId)
                    {
                        return Json(new { success = false, message = "No autorizado" });
                    }
                }
                // TODO: Agregar validaciones para otros tipos de kardex

                var result = await _kardexService.GuardarPersonalPresenteYCompletarAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation(
                        $"Personal presente guardado exitosamente: Kardex {request.KardexId} - {result.TotalRegistrados} empleados"
                    );

                    return Json(new
                    {
                        success = true,
                        message = result.Message,
                        totalRegistrados = result.TotalRegistrados,
                        redirectUrl = Url.Action(nameof(MiKardex))
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar personal presente");
                return Json(new { success = false, message = "Error al procesar la solicitud" });
            }
        }

        // ==========================================
        // KARDEX PENDIENTES DE REVISIÓN
        // ==========================================

        /// <summary>
        /// Vista principal de Kardex Pendientes de Revisión (solo Administrador Local)
        /// </summary>
        [Authorize(Roles = "Administrador Local")]
        [HttpGet]
        public async Task<IActionResult> PendientesDeRevision()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Obtener el local del administrador
            var usuario = await _context.Users
                .Include(u => u.Local)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null || usuario.LocalId == 0)
            {
                _logger.LogError($"❌ Usuario no encontrado o sin local asignado: {usuarioId}");
                SetErrorMessage("Error: No se encontró información del usuario");
                return RedirectToAction("Index", "Home");
            }

            var localId = usuario.LocalId;
            _logger.LogInformation($"📋 Cargando kardex pendientes para Local ID: {localId}");

            var viewModel = new KardexPendientesViewModel
            {
                KardexCocina = await ObtenerKardexCocinaPendientes(localId),
                KardexMozos = await ObtenerKardexMozosPendientes(localId),
                KardexVajilla = await ObtenerKardexVajillaPendientes(localId)
            };

            viewModel.TotalPendientesCocina = viewModel.KardexCocina.Count;
            viewModel.TotalPendientesMozos = viewModel.KardexMozos.Count;
            viewModel.TotalPendientesVajilla = viewModel.KardexVajilla.Count;

            _logger.LogInformation($"✅ Kardex pendientes cargados:");
            _logger.LogInformation($"   Cocina: {viewModel.TotalPendientesCocina}");
            _logger.LogInformation($"   Mozos: {viewModel.TotalPendientesMozos}");
            _logger.LogInformation($"   Vajilla: {viewModel.TotalPendientesVajilla}");

            return View(viewModel);
        }

        /// <summary>
        /// Obtener kardex de cocina pendientes (AGRUPADOS por día)
        /// </summary>
        private async Task<List<KardexPendienteItem>> ObtenerKardexCocinaPendientes(int localId)
        {
            // Obtener todos los kardex de cocina pendientes
            var kardexCocina = await _context.KardexCocina
                .Include(k => k.Empleado)
                .Where(k => k.LocalId == localId && 
                        k.Estado == EstadoKardex.Enviado)
                .OrderByDescending(k => k.Fecha)
                .ToListAsync();

            // Agrupar por fecha (los 3 tipos de cocina del mismo día en una fila)
            var agrupados = kardexCocina
                .GroupBy(k => k.Fecha.Date)
                .Select(g => new KardexPendienteItem
                {
                    // Usar el ID del primero como referencia
                    KardexId = g.First().Id,
                    TipoKardex = "Cocina", // Tipo genérico
                    Fecha = g.Key,
                    
                    // Consolidar los 3 responsables
                    Responsables = g.Select(k => k.Empleado?.NombreCompleto ?? "Sin nombre")
                                    .Distinct()
                                    .ToList(),
                    
                    // Sumar todo el personal presente de los 3 kardex
                    CantidadPersonalPresente = _context.PersonalPresente
                        .Count(p => g.Select(k => k.Id).Contains(p.KardexId) && 
                                p.TipoKardex.Contains("Cocina")),
                    
                    Estado = "Pendiente de Revisión",
                    
                    // Guardar los IDs de los 3 kardex agrupados
                    KardexIdsAgrupados = g.Select(k => k.Id).ToList(),
                    EsAgrupacion = true
                })
                .ToList();

            return agrupados;
        }

        /// <summary>
        /// Obtener kardex de mozos pendientes (SEPARADOS: Salón y Bebidas)
        /// </summary>
        private async Task<List<KardexPendienteItem>> ObtenerKardexMozosPendientes(int localId)
        {
            var items = new List<KardexPendienteItem>();

            // Kardex de Salón
            var kardexSalon = await _context.KardexSalon
                .Include(k => k.Empleado)
                .Where(k => k.LocalId == localId && 
                        k.Estado == EstadoKardex.Enviado)
                .OrderByDescending(k => k.Fecha)
                .ToListAsync();

            foreach (var kardex in kardexSalon)
            {
                var cantidadPersonal = await _context.PersonalPresente
                    .CountAsync(p => p.KardexId == kardex.Id && 
                                p.TipoKardex == TipoKardex.MozoSalon);

                items.Add(new KardexPendienteItem
                {
                    KardexId = kardex.Id,
                    TipoKardex = TipoKardex.MozoSalon,
                    Fecha = kardex.Fecha,
                    Responsables = new List<string> { kardex.Empleado?.NombreCompleto ?? "Sin nombre" },
                    CantidadPersonalPresente = cantidadPersonal,
                    Estado = "Pendiente de Revisión",
                    TipoDetalle = "Salón"
                });
            }

            // Kardex de Bebidas
            var kardexBebidas = await _context.KardexBebidas
                .Include(k => k.Empleado)
                .Where(k => k.LocalId == localId && 
                        k.Estado == EstadoKardex.Enviado)
                .OrderByDescending(k => k.Fecha)
                .ToListAsync();

            foreach (var kardex in kardexBebidas)
            {
                var cantidadPersonal = await _context.PersonalPresente
                    .CountAsync(p => p.KardexId == kardex.Id && 
                                p.TipoKardex == TipoKardex.MozoBebidas);

                items.Add(new KardexPendienteItem
                {
                    KardexId = kardex.Id,
                    TipoKardex = TipoKardex.MozoBebidas,
                    Fecha = kardex.Fecha,
                    Responsables = new List<string> { kardex.Empleado?.NombreCompleto ?? "Sin nombre" },
                    CantidadPersonalPresente = cantidadPersonal,
                    Estado = "Pendiente de Revisión",
                    TipoDetalle = "Bebidas"
                });
            }

            // Ordenar por fecha descendente
            return items.OrderByDescending(i => i.Fecha).ToList();
        }

        /// <summary>
        /// Obtener kardex de vajilla pendientes
        /// </summary>
        private async Task<List<KardexPendienteItem>> ObtenerKardexVajillaPendientes(int localId)
        {
            var kardexVajilla = await _context.KardexVajilla
                .Include(k => k.Empleado)
                .Where(k => k.LocalId == localId && 
                        k.Estado == EstadoKardex.Enviado)
                .OrderByDescending(k => k.Fecha)
                .ToListAsync();

            var items = new List<KardexPendienteItem>();

            foreach (var kardex in kardexVajilla)
            {
                var cantidadPersonal = await _context.PersonalPresente
                    .CountAsync(p => p.KardexId == kardex.Id && 
                                p.TipoKardex == TipoKardex.Vajilla);

                items.Add(new KardexPendienteItem
                {
                    KardexId = kardex.Id,
                    TipoKardex = TipoKardex.Vajilla,
                    Fecha = kardex.Fecha,
                    Responsables = new List<string> { kardex.Empleado?.NombreCompleto ?? "Sin nombre" },
                    CantidadPersonalPresente = cantidadPersonal,
                    Estado = "Pendiente de Revisión",
                    TipoDetalle = null
                });
            }

            return items;
        }
        
        // ==========================================
        // MÉTODO AUXILIAR PRIVADO
        // ==========================================

        /// <summary>
        /// Normaliza el tipo de kardex (decodifica HTML y trim)
        /// </summary>
        private string NormalizarTipoKardex(string? tipoKardex)
        {
            if (string.IsNullOrEmpty(tipoKardex))
                return string.Empty;
            
            // Decodificar HTML (convierte &#xF3; a ó)
            var normalizado = WebUtility.HtmlDecode(tipoKardex);
            
            // Trim
            normalizado = normalizado.Trim();
            
            _logger.LogDebug($"🔄 TipoKardex normalizado: '{tipoKardex}' → '{normalizado}'");
            
            return normalizado;
        }
    }
}