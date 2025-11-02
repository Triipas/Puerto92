using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;

namespace Puerto92.Controllers
{
    [Authorize(Roles = "Admin Maestro")]
    public class LocalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LocalesController> _logger;

        public LocalesController(ApplicationDbContext context, ILogger<LocalesController> logger)
        {
            _context = context;
            _logger = logger;
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
                    Ciudad = l.Distrito, // Asumiendo que Ciudad es igual a Distrito por ahora
                    Telefono = l.Telefono,
                    Activo = l.Activo,
                    FechaCreacion = l.FechaCreacion,
                    CantidadUsuarios = l.Usuarios.Count
                })
                .ToListAsync();

            return View(locales);
        }

        // POST: Locales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LocalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Datos inválidos. Por favor verifica los campos.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Generar código automático
                var ultimoLocal = await _context.Locales
                    .OrderByDescending(l => l.Id)
                    .FirstOrDefaultAsync();

                int siguienteNumero = 1;
                if (ultimoLocal != null)
                {
                    // Extraer el número del último código (LOC-XX)
                    var partes = ultimoLocal.Codigo.Split('-');
                    if (partes.Length == 2 && int.TryParse(partes[1], out int numero))
                    {
                        siguienteNumero = numero + 1;
                    }
                }

                string nuevoCodigo = $"LOC-{siguienteNumero:D2}";

                // Verificar que el código no exista (por seguridad)
                while (await _context.Locales.AnyAsync(l => l.Codigo == nuevoCodigo))
                {
                    siguienteNumero++;
                    nuevoCodigo = $"LOC-{siguienteNumero:D2}";
                }

                // Crear nuevo local
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
                TempData["Success"] = $"Local '{local.Nombre}' creado exitosamente con código {nuevoCodigo}";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear local");
                TempData["Error"] = "Error al crear el local. Por favor intenta nuevamente.";
                return RedirectToAction(nameof(Index));
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

                // Actualizar datos (el código NO se puede cambiar)
                local.Nombre = model.Nombre;
                local.Direccion = model.Direccion;
                local.Distrito = model.Distrito;
                local.Telefono = model.Telefono;
                local.Activo = model.Activo;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Local '{local.Nombre}' ({local.Codigo}) editado por {User.Identity!.Name}");
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
                TempData["Success"] = $"Local '{local.Nombre}' desactivado exitosamente";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar local");
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