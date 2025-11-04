using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;
using Puerto92.Services;

namespace Puerto92.Controllers
{
    [Authorize(Roles = "Supervisora de Calidad")]
    public class ProveedoresController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProveedoresController> _logger;
        private readonly IAuditService _auditService;

        public ProveedoresController(
            ApplicationDbContext context,
            ILogger<ProveedoresController> logger,
            IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        // GET: Proveedores
        public async Task<IActionResult> Index(string? categoria = null)
        {
            var query = _context.Proveedores
                .Where(p => p.Activo)
                .AsQueryable();

            // Filtrar por categoría si se especifica
            if (!string.IsNullOrEmpty(categoria))
            {
                query = query.Where(p => p.Categoria == categoria);
            }

            var proveedores = await query
                .OrderBy(p => p.Nombre)
                .Select(p => new ProveedorViewModel
                {
                    Id = p.Id,
                    RUC = p.RUC,
                    Nombre = p.Nombre,
                    Categoria = p.Categoria,
                    Telefono = p.Telefono,
                    Email = p.Email,
                    PersonaContacto = p.PersonaContacto,
                    Direccion = p.Direccion,
                    Activo = p.Activo,
                    FechaCreacion = p.FechaCreacion,
                    FechaModificacion = p.FechaModificacion,
                    CreadoPor = p.CreadoPor,
                    ModificadoPor = p.ModificadoPor
                })
                .ToListAsync();

            // Pasar estadísticas en ViewBag
            ViewBag.TotalProveedores = await _context.Proveedores.CountAsync();
            ViewBag.TotalActivos = await _context.Proveedores.CountAsync(p => p.Activo);
            ViewBag.TotalInactivos = await _context.Proveedores.CountAsync(p => !p.Activo);
            ViewBag.TotalCategorias = await _context.Proveedores
                .Where(p => p.Activo)
                .Select(p => p.Categoria)
                .Distinct()
                .CountAsync();

            ViewBag.CategoriaFiltro = categoria;
            ViewBag.Categorias = CategoriaProveedor.Todas;

            return View(proveedores);
        }

        // GET: Proveedores/GetProveedor?id=1
        [HttpGet]
        public async Task<IActionResult> GetProveedor(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor == null)
            {
                return NotFound();
            }

            var data = new
            {
                id = proveedor.Id,
                ruc = proveedor.RUC,
                nombre = proveedor.Nombre,
                categoria = proveedor.Categoria,
                telefono = proveedor.Telefono,
                email = proveedor.Email ?? "",
                personaContacto = proveedor.PersonaContacto ?? "",
                direccion = proveedor.Direccion ?? "",
                activo = proveedor.Activo
            };

            return Json(data);
        }

        // GET: Proveedores/BuscarPorRUC?ruc=20123456789
        [HttpGet]
        [AllowAnonymous] // Permitir acceso a administradores locales
        public async Task<IActionResult> BuscarPorRUC(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc) || ruc.Length != 11)
            {
                return Json(new { success = false, message = "RUC inválido" });
            }

            var proveedor = await _context.Proveedores
                .Where(p => p.RUC == ruc && p.Activo)
                .Select(p => new ProveedorAutocompletarViewModel
                {
                    Id = p.Id,
                    RUC = p.RUC,
                    Nombre = p.Nombre,
                    Categoria = p.Categoria,
                    Telefono = p.Telefono,
                    PersonaContacto = p.PersonaContacto
                })
                .FirstOrDefaultAsync();

            if (proveedor == null)
            {
                return Json(new { success = false, message = "RUC no encontrado" });
            }

            return Json(new { success = true, data = proveedor });
        }

        // POST: Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProveedorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest)
                    return JsonError("Datos inválidos. Por favor verifica los campos.");

                SetErrorMessage("Datos inválidos. Por favor verifica los campos.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Validar que el RUC no exista
                if (await _context.Proveedores.AnyAsync(p => p.RUC == model.RUC))
                {
                    if (IsAjaxRequest)
                        return JsonError("El RUC ya está registrado en el sistema.");

                    SetErrorMessage("El RUC ya está registrado en el sistema.");
                    return RedirectToAction(nameof(Index));
                }

                var proveedor = new Proveedor
                {
                    RUC = model.RUC.Trim(),
                    Nombre = model.Nombre.Trim(),
                    Categoria = model.Categoria.Trim(),
                    Telefono = model.Telefono.Trim(),
                    Email = model.Email?.Trim(),
                    PersonaContacto = model.PersonaContacto?.Trim(),
                    Direccion = model.Direccion?.Trim(),
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    CreadoPor = User.Identity!.Name
                };

                _context.Proveedores.Add(proveedor);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Proveedor '{proveedor.Nombre}' (RUC: {proveedor.RUC}) creado por {User.Identity!.Name}");

                await _auditService.RegistrarCreacionProveedorAsync(
                    rucProveedor: proveedor.RUC,
                    nombreProveedor: proveedor.Nombre,
                    categoria: proveedor.Categoria);

                SetSuccessMessage($"Proveedor '{proveedor.Nombre}' creado exitosamente.");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear proveedor");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al crear proveedor",
                    detalles: ex.Message);

                if (IsAjaxRequest)
                    return JsonError("Error al crear el proveedor. Por favor intenta nuevamente.");

                SetErrorMessage("Error al crear el proveedor. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Proveedores/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProveedorViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                SetErrorMessage("Datos inválidos. Por favor verifica los campos.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var proveedor = await _context.Proveedores.FindAsync(id);

                if (proveedor == null)
                {
                    return NotFound();
                }

                // Detectar cambios para auditoría
                List<string> cambios = new List<string>();

                if (proveedor.Nombre != model.Nombre)
                    cambios.Add($"Nombre: '{proveedor.Nombre}' → '{model.Nombre}'");

                if (proveedor.Categoria != model.Categoria)
                    cambios.Add($"Categoría: '{proveedor.Categoria}' → '{model.Categoria}'");

                if (proveedor.Telefono != model.Telefono)
                    cambios.Add($"Teléfono: '{proveedor.Telefono}' → '{model.Telefono}'");

                if (proveedor.Email != model.Email)
                    cambios.Add($"Email: '{proveedor.Email}' → '{model.Email}'");

                if (proveedor.PersonaContacto != model.PersonaContacto)
                    cambios.Add($"Contacto: '{proveedor.PersonaContacto}' → '{model.PersonaContacto}'");

                if (proveedor.Direccion != model.Direccion)
                    cambios.Add($"Dirección modificada");

                // Actualizar datos (RUC NO se modifica)
                proveedor.Nombre = model.Nombre.Trim();
                proveedor.Categoria = model.Categoria.Trim();
                proveedor.Telefono = model.Telefono.Trim();
                proveedor.Email = model.Email?.Trim();
                proveedor.PersonaContacto = model.PersonaContacto?.Trim();
                proveedor.Direccion = model.Direccion?.Trim();
                proveedor.FechaModificacion = DateTime.Now;
                proveedor.ModificadoPor = User.Identity!.Name;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Proveedor '{proveedor.Nombre}' (RUC: {proveedor.RUC}) editado por {User.Identity!.Name}");

                if (cambios.Any())
                {
                    await _auditService.RegistrarEdicionProveedorAsync(
                        rucProveedor: proveedor.RUC,
                        nombreProveedor: proveedor.Nombre,
                        cambios: string.Join(", ", cambios));
                }

                SetSuccessMessage("Proveedor actualizado exitosamente.");

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProveedorExists(model.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar proveedor");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al editar proveedor",
                    detalles: ex.Message);

                SetErrorMessage("Error al actualizar el proveedor. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Proveedores/Desactivar/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                var proveedor = await _context.Proveedores.FindAsync(id);

                if (proveedor == null)
                {
                    return NotFound();
                }

                // Desactivar el proveedor (soft delete)
                proveedor.Activo = false;
                proveedor.FechaModificacion = DateTime.Now;
                proveedor.ModificadoPor = User.Identity!.Name;

                await _context.SaveChangesAsync();

                _logger.LogWarning($"Proveedor '{proveedor.Nombre}' (RUC: {proveedor.RUC}) DESACTIVADO por {User.Identity!.Name}");

                await _auditService.RegistrarDesactivacionProveedorAsync(
                    rucProveedor: proveedor.RUC,
                    nombreProveedor: proveedor.Nombre);

                SetSuccessMessage($"Proveedor '{proveedor.Nombre}' desactivado exitosamente.");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar proveedor");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al desactivar proveedor",
                    detalles: ex.Message);

                SetErrorMessage("Error al desactivar el proveedor. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // Método auxiliar
        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.Id == id);
        }
    }
}