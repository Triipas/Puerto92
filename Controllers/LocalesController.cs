using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;
using Puerto92.Services;

namespace Puerto92.Controllers
{
    [Authorize(Roles = "Admin Maestro")]
    public class LocalesController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LocalesController> _logger;
        private readonly IAuditService _auditService;

        public LocalesController(
            ApplicationDbContext context, 
            ILogger<LocalesController> logger,
            IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        // GET: Locales
        public async Task<IActionResult> Index()
        {
            var locales = await _context.Locales
                .Include(l => l.Usuarios)
                .OrderBy(l => l.Nombre)
                .Select(l => new LocalViewModel
                {
                    Id = l.Id,
                    Codigo = l.Codigo,
                    Nombre = l.Nombre,
                    Direccion = l.Direccion,
                    Distrito = l.Distrito,
                    Ciudad = l.Distrito,
                    Telefono = l.Telefono,
                    Activo = l.Activo,
                    FechaCreacion = l.FechaCreacion,
                    CantidadUsuarios = l.Usuarios.Count
                })
                .ToListAsync();

            // Automáticamente devuelve partial view si es AJAX
            return View(locales);
        }

        // POST: Locales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LocalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest)
                    return JsonError("Datos inválidos. Por favor verifica los campos.");
                
                SetErrorMessage("Datos inválidos. Por favor verifica los campos.");
                return RedirectToActionAjax(nameof(Index));
            }

            try
            {
                var ultimoLocal = await _context.Locales
                    .OrderByDescending(l => l.Id)
                    .FirstOrDefaultAsync();

                int siguienteNumero = 1;
                if (ultimoLocal != null)
                {
                    var partes = ultimoLocal.Codigo.Split('-');
                    if (partes.Length == 2 && int.TryParse(partes[1], out int numero))
                    {
                        siguienteNumero = numero + 1;
                    }
                }

                string nuevoCodigo = $"LOC-{siguienteNumero:D2}";

                while (await _context.Locales.AnyAsync(l => l.Codigo == nuevoCodigo))
                {
                    siguienteNumero++;
                    nuevoCodigo = $"LOC-{siguienteNumero:D2}";
                }

                var local = new Local
                {
                    Codigo = nuevoCodigo,
                    Nombre = model.Nombre,
                    Direccion = model.Direccion,
                    Distrito = model.Distrito,
                    Telefono = model.Telefono,
                    Activo = model.Activo,
                    FechaCreacion = DateTime.Now
                };

                _context.Locales.Add(local);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Local '{local.Nombre}' ({local.Codigo}) creado por {User.Identity!.Name}");
                
                await _auditService.RegistrarCreacionLocalAsync(
                    codigoLocal: local.Codigo,
                    nombreLocal: local.Nombre);

                SetSuccessMessage($"Local '{local.Nombre}' creado exitosamente con código {nuevoCodigo}");
                return RedirectToActionAjax(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear local");
                
                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al crear local",
                    detalles: ex.Message);

                if (IsAjaxRequest)
                    return JsonError("Error al crear el local. Por favor intenta nuevamente.");

                SetErrorMessage("Error al crear el local. Por favor intenta nuevamente.");
                return RedirectToActionAjax(nameof(Index));
            }
        }

        // GET: Locales/GetLocal?id=1 (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetLocal(int id)
        {
            var local = await _context.Locales.FindAsync(id);
            
            if (local == null)
            {
                return NotFound();
            }

            var data = new
            {
                id = local.Id,
                codigo = local.Codigo,
                nombre = local.Nombre,
                direccion = local.Direccion,
                distrito = local.Distrito,
                ciudad = local.Distrito, // Temporalmente igual a distrito
                telefono = local.Telefono,
                activo = local.Activo
            };

            return Json(data);
        }

        // GET: Locales/GetLocalEstadisticas?id=1 (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetLocalEstadisticas(int id)
        {
            var local = await _context.Locales
                .Include(l => l.Usuarios)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (local == null)
            {
                return NotFound();
            }

            var data = new
            {
                usuarios = local.Usuarios.Count,
                valorInventario = 15000.00m // TODO: Calcular valor real del inventario cuando exista esa funcionalidad
            };

            return Json(data);
        }

        // POST: Locales/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LocalViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Datos inválidos. Por favor verifica los campos.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var local = await _context.Locales.FindAsync(id);
                
                if (local == null)
                {
                    return NotFound();
                }

                // Detectar cambios para auditoría
                List<string> cambios = new List<string>();
                
                if (local.Nombre != model.Nombre)
                    cambios.Add($"Nombre: '{local.Nombre}' → '{model.Nombre}'");
                
                if (local.Direccion != model.Direccion)
                    cambios.Add($"Dirección: '{local.Direccion}' → '{model.Direccion}'");
                
                if (local.Distrito != model.Distrito)
                    cambios.Add($"Distrito: '{local.Distrito}' → '{model.Distrito}'");
                
                if (local.Telefono != model.Telefono)
                    cambios.Add($"Teléfono: '{local.Telefono}' → '{model.Telefono}'");
                
                if (local.Activo != model.Activo)
                    cambios.Add($"Estado: {(local.Activo ? "Activo" : "Inactivo")} → {(model.Activo ? "Activo" : "Inactivo")}");

                // Actualizar datos (el código NO se puede cambiar)
                local.Nombre = model.Nombre;
                local.Direccion = model.Direccion;
                local.Distrito = model.Distrito;
                local.Telefono = model.Telefono;
                local.Activo = model.Activo;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Local '{local.Nombre}' ({local.Codigo}) editado por {User.Identity!.Name}");
                
                // 🔍 REGISTRAR EDICIÓN DE LOCAL EN AUDITORÍA
                if (cambios.Any())
                {
                    await _auditService.RegistrarEdicionLocalAsync(
                        codigoLocal: local.Codigo,
                        nombreLocal: local.Nombre,
                        cambios: string.Join(", ", cambios));
                }

                TempData["Success"] = "Local actualizado exitosamente";
                
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LocalExists(model.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar local");
                
                // 🔍 REGISTRAR ERROR EN AUDITORÍA
                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al editar local",
                    detalles: ex.Message);

                TempData["Error"] = "Error al actualizar el local. Por favor intenta nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Locales/Delete/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var local = await _context.Locales
                    .Include(l => l.Usuarios)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (local == null)
                {
                    return NotFound();
                }

                // Desactivar el local (soft delete)
                local.Activo = false;

                // Opcional: Desasignar usuarios del local
                foreach (var usuario in local.Usuarios)
                {
                    // Podrías reasignarlos a un local por defecto o simplemente dejarlos
                    // usuario.LocalId = otroLocalId;
                }

                await _context.SaveChangesAsync();

                _logger.LogWarning($"Local '{local.Nombre}' ({local.Codigo}) DESACTIVADO por {User.Identity!.Name}");
                
                // 🔍 REGISTRAR DESACTIVACIÓN DE LOCAL EN AUDITORÍA
                await _auditService.RegistrarDesactivacionLocalAsync(
                    codigoLocal: local.Codigo,
                    nombreLocal: local.Nombre);

                TempData["Success"] = $"Local '{local.Nombre}' desactivado exitosamente";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar local");
                
                // 🔍 REGISTRAR ERROR EN AUDITORÍA
                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al desactivar local",
                    detalles: ex.Message);

                TempData["Error"] = "Error al desactivar el local. Por favor intenta nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Locales/ToggleStatus/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var local = await _context.Locales.FindAsync(id);
            
            if (local == null)
            {
                return NotFound();
            }

            local.Activo = !local.Activo;
            await _context.SaveChangesAsync();

            var estado = local.Activo ? "activado" : "desactivado";
            _logger.LogInformation($"Local '{local.Nombre}' {estado} por {User.Identity!.Name}");
            TempData["Success"] = $"Local {estado} exitosamente";

            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar
        private bool LocalExists(int id)
        {
            return _context.Locales.Any(e => e.Id == id);
        }
    }
}