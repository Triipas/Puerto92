using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;

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
                .ToListAsync();

            return View(locales);
        }

        // GET: Locales/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Locales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Local local)
        {
            if (ModelState.IsValid)
            {
                // Verificar si el código ya existe
                var codigoExiste = await _context.Locales.AnyAsync(l => l.Codigo == local.Codigo);
                if (codigoExiste)
                {
                    ModelState.AddModelError("Codigo", "El código ya existe");
                    return View(local);
                }

                local.FechaCreacion = DateTime.Now;
                _context.Add(local);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Local '{local.Nombre}' creado por {User.Identity!.Name}");
                TempData["Success"] = "Local creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            return View(local);
        }

        // GET: Locales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var local = await _context.Locales.FindAsync(id);
            if (local == null)
            {
                return NotFound();
            }
            return View(local);
        }

        // POST: Locales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Local local)
        {
            if (id != local.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(local);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Local '{local.Nombre}' editado por {User.Identity!.Name}");
                    TempData["Success"] = "Local actualizado exitosamente";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LocalExists(local.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(local);
        }

        // POST: Locales/ToggleStatus/5
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

        private bool LocalExists(int id)
        {
            return _context.Locales.Any(e => e.Id == id);
        }
    }
}