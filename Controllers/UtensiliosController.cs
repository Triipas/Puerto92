using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;
using Puerto92.Services;

namespace Puerto92.Controllers
{
    [Authorize(Roles = "Contador")]
    public class UtensiliosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public UtensiliosController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: /Utensilios
       public IActionResult Index()
{
    // 🔥 Detectar petición AJAX
    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
    {
        ViewData["IsAjax"] = true;
    }
    
    return View();
}


        // GET: /Utensilios/GetEstadisticas
        [HttpGet]
        public async Task<IActionResult> GetEstadisticas()
        {
            var totalActivos = await _context.Utensilios.CountAsync(u => u.Activo);
            var utensiliosCocina = await _context.Utensilios.CountAsync(u => u.Activo && u.Tipo == "Cocina");
            var utensiliosMozos = await _context.Utensilios.CountAsync(u => u.Activo && u.Tipo == "Mozos");
            var vajilla = await _context.Utensilios.CountAsync(u => u.Activo && u.Tipo == "Vajilla");

            return Json(new
            {
                totalActivos,
                utensiliosCocina,
                utensiliosMozos,
                vajilla
            });
        }

        // GET: /Utensilios/GetUtensilios
        [HttpGet]
        public async Task<IActionResult> GetUtensilios()
        {
            var utensilios = await _context.Utensilios
                .Select(u => new
                {
                    u.Id,
                    u.Codigo,
                    u.Nombre,
                    u.Descripcion,
                    u.Tipo,
                    u.Unidad,
                    u.Precio,
                    u.Activo
                })
                .ToListAsync();

            return Json(utensilios);
        }

        // GET: /Utensilios/GetUtensilio?id=xxx
        [HttpGet]
        public async Task<IActionResult> GetUtensilio(string id)
        {
            if (!int.TryParse(id, out var idInt))
                return Json(new { success = false, message = "ID inválido" });

            var utensilio = await _context.Utensilios.FindAsync(idInt);
            if (utensilio == null)
                return Json(new { success = false, message = "Utensilio no encontrado" });

            return Json(utensilio);
        }

        // POST: /Utensilios/CreateAjax
   [HttpPost]
public async Task<IActionResult> CreateAjax(UtensilioViewModel model)
{
    if (!ModelState.IsValid)
        return Json(new { success = false, message = "Datos inválidos" });

    var utensilio = new Utensilio
    {
        Codigo = model.Codigo,
        Nombre = model.Nombre,
        Tipo = model.Tipo,
        Unidad = model.Unidad,
        Precio = model.Precio,
        Descripcion = model.Descripcion,
        Activo = true
    };

    _context.Utensilios.Add(utensilio);

    try
    {
        // Guardar primero el utensilio
        await _context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Ocurrió un error al guardar el utensilio" });
    }

    // Intentar registrar auditoría, pero no bloquear la creación si falla
    try
    {
        await _auditService.RegistrarCreacionUtensilioAsync(utensilio);
    }
    catch (Exception ex)
    {
        // Solo loguear el error, no devolver fallo al usuario
        Console.WriteLine("Error al registrar auditoría: " + ex.Message);
    }

    return Json(new { success = true, message = "Utensilio creado correctamente" });
}

        // POST: /Utensilios/Edit/{id}
        [HttpPost]
        public async Task<IActionResult> Edit(string id, UtensilioViewModel model)
        {
            if (!int.TryParse(id, out var idInt) || idInt != model.Id)
                return Json(new { success = false, message = "ID inválido" });

            var utensilio = await _context.Utensilios.FindAsync(idInt);
            if (utensilio == null)
                return Json(new { success = false, message = "Utensilio no encontrado" });

            utensilio.Nombre = model.Nombre;
            utensilio.Tipo = model.Tipo;
            utensilio.Unidad = model.Unidad;
            utensilio.Precio = model.Precio;
            utensilio.Descripcion = model.Descripcion;

            _context.Utensilios.Update(utensilio);

            try
            {
                await _context.SaveChangesAsync();
                await _auditService.RegistrarEdicionUtensilioAsync(utensilio);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = true });
        }

        // POST: /Utensilios/Deactivate/{id}
        [HttpPost]
        public async Task<IActionResult> Deactivate(string id)
        {
            if (!int.TryParse(id, out var idInt))
                return Json(new { success = false, message = "ID inválido" });

            var utensilio = await _context.Utensilios.FindAsync(idInt);
            if (utensilio == null)
                return Json(new { success = false, message = "Utensilio no encontrado" });

            utensilio.Activo = false;
            _context.Utensilios.Update(utensilio);

            try
            {
                await _context.SaveChangesAsync();
                await _auditService.RegistrarDesactivacionUtensilioAsync(utensilio);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = true });
        }

        // POST: /Utensilios/BulkUpload
        [HttpPost]
        public async Task<IActionResult> BulkUpload()
        {
            return Json(new { success = true, cantidadProcesada = 0 });
        }

        // GET: /Utensilios/ExportarCSV
        [HttpGet]
        public async Task<IActionResult> ExportarCSV()
        {
            var utensilios = await _context.Utensilios.ToListAsync();
            var csv = "Codigo,Nombre,Tipo,Unidad,Precio,Descripcion\n";

            foreach (var u in utensilios)
            {
                csv += $"{u.Codigo},{u.Nombre},{u.Tipo},{u.Unidad},{u.Precio},{u.Descripcion}\n";
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"utensilios_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}