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
    public class CategoriasController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriasController> _logger;
        private readonly IAuditService _auditService;

        public CategoriasController(
            ApplicationDbContext context,
            ILogger<CategoriasController> logger,
            IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        // GET: Categorias
        public async Task<IActionResult> Index(string? tipo = null)
        {
            // Si no se especifica tipo, mostrar Bebidas por defecto
            tipo ??= TipoCategoria.Bebidas;

            // Validar que el tipo sea válido
            if (!TipoCategoria.EsValido(tipo))
            {
                tipo = TipoCategoria.Bebidas;
            }

            ViewBag.TipoActual = tipo;
            ViewBag.TiposTodos = TipoCategoria.Todos;

            // Obtener categorías del tipo seleccionado
            var categorias = await _context.Categorias
                .Where(c => c.Tipo == tipo)
                .OrderBy(c => c.Orden)
                .Select(c => new CategoriaViewModel
                {
                    Id = c.Id,
                    Tipo = c.Tipo,
                    Nombre = c.Nombre,
                    Orden = c.Orden,
                    Activo = c.Activo,
                    FechaCreacion = c.FechaCreacion,
                    CreadoPor = c.CreadoPor,
                    CantidadProductos = 0 // TODO: Calcular cuando exista tabla de productos
                })
                .ToListAsync();

            // Obtener estadísticas por tipo
            var estadisticas = new Dictionary<string, int>();
            foreach (var t in TipoCategoria.Todos)
            {
                var count = await _context.Categorias.CountAsync(c => c.Tipo == t && c.Activo);
                estadisticas[t] = count;
            }
            ViewBag.Estadisticas = estadisticas;

            return View(categorias);
        }

        // POST: Categorias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoriaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest)
                    return JsonError("Datos inválidos. Por favor verifica los campos.");

                SetErrorMessage("Datos inválidos. Por favor verifica los campos.");
                return RedirectToAction(nameof(Index), new { tipo = model.Tipo });
            }

            try
            {
                // Validar que el tipo sea válido
                if (!TipoCategoria.EsValido(model.Tipo))
                {
                    if (IsAjaxRequest)
                        return JsonError("Tipo de categoría inválido.");

                    SetErrorMessage("Tipo de categoría inválido.");
                    return RedirectToAction(nameof(Index));
                }

                // Verificar que el nombre sea único dentro del tipo
                var existe = await _context.Categorias
                    .AnyAsync(c => c.Tipo == model.Tipo && 
                                   c.Nombre.ToLower() == model.Nombre.ToLower());

                if (existe)
                {
                    if (IsAjaxRequest)
                        return JsonError($"Ya existe una categoría con el nombre '{model.Nombre}' en {model.Tipo}.");

                    SetErrorMessage($"Ya existe una categoría con el nombre '{model.Nombre}' en {model.Tipo}.");
                    return RedirectToAction(nameof(Index), new { tipo = model.Tipo });
                }

                // Si no se especificó orden, asignar el siguiente disponible
                if (model.Orden == 0)
                {
                    var maxOrden = await _context.Categorias
                        .Where(c => c.Tipo == model.Tipo)
                        .MaxAsync(c => (int?)c.Orden) ?? 0;
                    model.Orden = maxOrden + 1;
                }

                var categoria = new Categoria
                {
                    Tipo = model.Tipo,
                    Nombre = model.Nombre.Trim(),
                    Orden = model.Orden,
                    Activo = model.Activo,
                    FechaCreacion = DateTime.Now,
                    CreadoPor = User.Identity!.Name
                };

                _context.Categorias.Add(categoria);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Categoría '{categoria.Nombre}' ({categoria.Tipo}) creada por {User.Identity!.Name}");

                await _auditService.RegistrarAccionAsync(
                    accion: "Crear Categoría",
                    descripcion: $"Categoría '{categoria.Nombre}' creada en tipo '{categoria.Tipo}' con orden {categoria.Orden}",
                    datosAdicionales: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        CategoriaId = categoria.Id,
                        Tipo = categoria.Tipo,
                        Nombre = categoria.Nombre,
                        Orden = categoria.Orden
                    }),
                    modulo: "Categorías",
                    resultado: "Exitoso",
                    nivelSeveridad: "Info");

                SetSuccessMessage($"Categoría '{categoria.Nombre}' creada exitosamente.");
                return RedirectToAction(nameof(Index), new { tipo = model.Tipo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al crear categoría",
                    detalles: ex.Message);

                if (IsAjaxRequest)
                    return JsonError("Error al crear la categoría. Por favor intenta nuevamente.");

                SetErrorMessage("Error al crear la categoría. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index), new { tipo = model.Tipo });
            }
        }

        // GET: Categorias/GetCategoria?id=1 (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);

            if (categoria == null)
            {
                return NotFound();
            }

            var data = new
            {
                id = categoria.Id,
                tipo = categoria.Tipo,
                nombre = categoria.Nombre,
                orden = categoria.Orden,
                activo = categoria.Activo
            };

            return Json(data);
        }

        // POST: Categorias/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoriaViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                SetErrorMessage("Datos inválidos. Por favor verifica los campos.");
                return RedirectToAction(nameof(Index), new { tipo = model.Tipo });
            }

            try
            {
                var categoria = await _context.Categorias.FindAsync(id);

                if (categoria == null)
                {
                    return NotFound();
                }

                // Verificar que el nombre sea único (excepto para esta categoría)
                var existe = await _context.Categorias
                    .AnyAsync(c => c.Id != id &&
                                   c.Tipo == model.Tipo &&
                                   c.Nombre.ToLower() == model.Nombre.ToLower());

                if (existe)
                {
                    SetErrorMessage($"Ya existe otra categoría con el nombre '{model.Nombre}' en {model.Tipo}.");
                    return RedirectToAction(nameof(Index), new { tipo = model.Tipo });
                }

                // Detectar cambios para auditoría
                List<string> cambios = new List<string>();

                if (categoria.Nombre != model.Nombre)
                    cambios.Add($"Nombre: '{categoria.Nombre}' → '{model.Nombre}'");

                if (categoria.Orden != model.Orden)
                    cambios.Add($"Orden: {categoria.Orden} → {model.Orden}");

                if (categoria.Activo != model.Activo)
                    cambios.Add($"Estado: {(categoria.Activo ? "Activo" : "Inactivo")} → {(model.Activo ? "Activo" : "Inactivo")}");

                // Actualizar datos
                categoria.Nombre = model.Nombre.Trim();
                categoria.Orden = model.Orden;
                categoria.Activo = model.Activo;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Categoría '{categoria.Nombre}' ({categoria.Tipo}) editada por {User.Identity!.Name}");

                // Registrar en auditoría
                if (cambios.Any())
                {
                    await _auditService.RegistrarAccionAsync(
                        accion: "Editar Categoría",
                        descripcion: $"Categoría '{categoria.Nombre}' ({categoria.Tipo}) editada. Cambios: {string.Join(", ", cambios)}",
                        datosAdicionales: System.Text.Json.JsonSerializer.Serialize(new
                        {
                            CategoriaId = categoria.Id,
                            Cambios = cambios
                        }),
                        modulo: "Categorías",
                        resultado: "Exitoso",
                        nivelSeveridad: "Info");
                }

                SetSuccessMessage("Categoría actualizada exitosamente.");
                return RedirectToAction(nameof(Index), new { tipo = model.Tipo });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoriaExists(model.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar categoría");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al editar categoría",
                    detalles: ex.Message);

                SetErrorMessage("Error al actualizar la categoría. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index), new { tipo = model.Tipo });
            }
        }

        // POST: Categorias/Delete/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var categoria = await _context.Categorias.FindAsync(id);

                if (categoria == null)
                {
                    return NotFound();
                }

                // TODO: Cuando existan productos, verificar si tiene productos asignados
                int productosAsignados = 0; // await _context.Productos.CountAsync(p => p.CategoriaId == id);

                if (productosAsignados > 0)
                {
                    SetErrorMessage($"No se puede eliminar la categoría '{categoria.Nombre}'. Hay {productosAsignados} producto(s) asignados a esta categoría.");
                    return RedirectToAction(nameof(Index), new { tipo = categoria.Tipo });
                }

                var nombreCategoria = categoria.Nombre;
                var tipoCategoria = categoria.Tipo;

                _context.Categorias.Remove(categoria);
                await _context.SaveChangesAsync();

                _logger.LogWarning($"Categoría '{nombreCategoria}' ({tipoCategoria}) ELIMINADA por {User.Identity!.Name}");

                await _auditService.RegistrarAccionAsync(
                    accion: "Eliminar Categoría",
                    descripcion: $"Categoría '{nombreCategoria}' ({tipoCategoria}) eliminada del sistema",
                    datosAdicionales: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        CategoriaId = id,
                        Nombre = nombreCategoria,
                        Tipo = tipoCategoria
                    }),
                    modulo: "Categorías",
                    resultado: "Exitoso",
                    nivelSeveridad: "Warning");

                SetSuccessMessage($"Categoría '{nombreCategoria}' eliminada exitosamente.");
                return RedirectToAction(nameof(Index), new { tipo = tipoCategoria });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categoría");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al eliminar categoría",
                    detalles: ex.Message);

                SetErrorMessage("Error al eliminar la categoría. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categorias/UpdateOrder
        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] List<OrdenCategoriaDto> ordenes)
        {
            try
            {
                if (ordenes == null || !ordenes.Any())
                {
                    return JsonError("No se recibieron datos de ordenamiento.");
                }

                foreach (var orden in ordenes)
                {
                    var categoria = await _context.Categorias.FindAsync(orden.Id);
                    if (categoria != null)
                    {
                        categoria.Orden = orden.Orden;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Orden de categorías actualizado por {User.Identity!.Name}");

                await _auditService.RegistrarAccionAsync(
                    accion: "Reordenar Categorías",
                    descripcion: $"Se actualizó el orden de {ordenes.Count} categoría(s)",
                    datosAdicionales: System.Text.Json.JsonSerializer.Serialize(ordenes),
                    modulo: "Categorías",
                    resultado: "Exitoso",
                    nivelSeveridad: "Info");

                return JsonSuccess("Orden actualizado exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar orden de categorías");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al reordenar categorías",
                    detalles: ex.Message);

                return JsonError("Error al actualizar el orden. Por favor intenta nuevamente.");
            }
        }

        // Método auxiliar
        private bool CategoriaExists(int id)
        {
            return _context.Categorias.Any(e => e.Id == id);
        }
    }

    // DTO para reordenamiento
    public class OrdenCategoriaDto
    {
        public int Id { get; set; }
        public int Orden { get; set; }
    }
}