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
    public class ProveedoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= INDEX =================
        public IActionResult Index()
{
    // 🔥 Detectar petición AJAX
    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
    {
        ViewData["IsAjax"] = true;
    }
    
    return View();
}

        // ================= ESTADÍSTICAS =================
        [HttpGet]
        public async Task<JsonResult> GetEstadisticas()
        {
            var total = await _context.Proveedores.CountAsync();
            var activos = await _context.Proveedores.CountAsync(p => p.Activo);
            var inactivos = total - activos;

            return Json(new
            {
                totalProveedores = total,
                proveedoresActivos = activos,
                proveedoresInactivos = inactivos
            });
        }

        // ================= LISTA DE PROVEEDORES =================
        [HttpGet]
        public async Task<JsonResult> GetProveedores()
        {
            var proveedores = await _context.Proveedores
                .Select(p => new
                {
                    p.Id,
                    p.Ruc,
                    p.Nombre,
                    p.Categoria,
                    p.Telefono,
                    p.Email,
                    p.PersonaContacto,
                    p.Direccion,
                    p.Activo
                })
                .ToListAsync();

            return Json(proveedores);
        }

        // ================= LISTA DE CATEGORÍAS =================
        [HttpGet]
        public async Task<JsonResult> GetCategorias()
        {
            var categorias = await _context.Proveedores
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Formatear para tu JS (value/text)
            var categoriasJs = categorias.Select(c => new { value = c, text = c }).ToList();

            return Json(categoriasJs);
        }

        // ================= OBTENER UN PROVEEDOR =================
        [HttpGet]
        public async Task<JsonResult> GetProveedor(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor == null)
            {
                return Json(new { success = false, message = "Proveedor no encontrado" });
            }

            return Json(proveedor);
        }

        // ================= CREAR PROVEEDOR =================
        [HttpPost]
        public async Task<JsonResult> CreateAjax([FromForm] Proveedor proveedor)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Datos inválidos" });
            }

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ================= EDITAR PROVEEDOR =================
        [HttpPost]
        public async Task<JsonResult> Edit(int id, [FromForm] Proveedor proveedorActualizado)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
            {
                return Json(new { success = false, message = "Proveedor no encontrado" });
            }

            // Actualizar campos
            proveedor.Ruc = proveedorActualizado.Ruc;
            proveedor.Nombre = proveedorActualizado.Nombre;
            proveedor.Categoria = proveedorActualizado.Categoria;
            proveedor.Telefono = proveedorActualizado.Telefono;
            proveedor.Email = proveedorActualizado.Email;
            proveedor.PersonaContacto = proveedorActualizado.PersonaContacto;
            proveedor.Direccion = proveedorActualizado.Direccion;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ================= DESACTIVAR PROVEEDOR =================
        [HttpPost]
        public async Task<JsonResult> Deactivate(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
            {
                return Json(new { success = false, message = "Proveedor no encontrado" });
            }

            proveedor.Activo = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ================= EXPORTAR CSV =================
        [HttpGet]
        public async Task<IActionResult> ExportarCSV()
        {
            var proveedores = await _context.Proveedores.ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("RUC,Nombre,Categoria,Telefono,Email,PersonaContacto,Direccion,Estado");

            foreach (var p in proveedores)
            {
                csv.AppendLine($"{p.Ruc},{p.Nombre},{p.Categoria},{p.Telefono},{p.Email},{p.PersonaContacto},{p.Direccion},{(p.Activo ? "Activo" : "Inactivo")}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"proveedores_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}