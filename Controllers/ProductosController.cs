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
    public class ProductosController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductosController> _logger;
        private readonly IAuditService _auditService;

        public ProductosController(
            ApplicationDbContext context,
            ILogger<ProductosController> logger,
            IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        // GET: Productos
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos
                .Select(p => new ProductoViewModel
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Codigo = p.Codigo,
                    Categoria = p.Categoria, // Solo string
                    Activo = p.Activo,
                    PrecioCompra = p.PrecioCompra,
                    PrecioVenta = p.PrecioVenta,
                    Descripcion = p.Descripcion,
                    UnidadMedida = p.UnidadMedida
                })
                .ToListAsync();

            return View(productos);
        }

        // GET: /Productos/GetProducto?id=xxx (Para AJAX)
        [HttpGet]
        public async Task<IActionResult> GetProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            var data = new
            {
                id = producto.Id,
                nombre = producto.Nombre,
                codigo = producto.Codigo,
                categoria = producto.Categoria, // string
                activo = producto.Activo,
                precioCompra = producto.PrecioCompra,
                precioVenta = producto.PrecioVenta,
                descripcion = producto.Descripcion,
                unidadMedida = producto.UnidadMedida
            };

            return Json(data);
        }



// GET: /Productos/GetCategorias (Para AJAX)
// ==========================================
[HttpGet]
public IActionResult GetCategorias()
{
    var categorias = new[]
    {
        new { value = "Cervezas", text = "Cervezas" },
        new { value = "Licores", text = "Licores" },
        new { value = "Snacks", text = "Snacks" }
    };
    return Json(categorias);
}



        // POST: Productos/CreateAjax
        [HttpPost]
        public async Task<IActionResult> CreateAjax(ProductoViewModel model)
        {
            if (!ModelState.IsValid) return JsonError("Datos inválidos. Verifica los campos.");

            // Validar duplicado
            if (await _context.Productos.AnyAsync(p => p.Nombre == model.Nombre))
                return JsonError("El nombre del producto ya existe.");

            // Generar código automático
            model.Codigo = await GenerarCodigoProducto();

            var producto = new Producto
            {
                Nombre = model.Nombre,
                Codigo = model.Codigo,
                Categoria = model.Categoria, // solo string
                Activo = true,
                PrecioCompra = model.PrecioCompra,
                PrecioVenta = model.PrecioVenta,
                UnidadMedida = model.UnidadMedida,
                Descripcion = model.Descripcion,
                FechaCreacion = DateTime.Now
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Producto {producto.Nombre} creado por {User.Identity!.Name}");
            await _auditService.RegistrarCreacionProductoAsync(producto);

            return JsonSuccess("Producto creado exitosamente", new
            {
                producto.Id,
                producto.Nombre,
                producto.Codigo,
                producto.Categoria,
                producto.PrecioCompra,
                producto.PrecioVenta
            });
        }

        // POST: Productos/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductoViewModel model)
        {
            if (id != model.Id) return JsonError("Producto no encontrado.");
            if (!ModelState.IsValid) return JsonError("Datos inválidos. Verifica los campos.");

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return JsonError("Producto no encontrado.");

            var cambios = new List<string>();

            if (producto.Nombre != model.Nombre)
            {
                cambios.Add($"Nombre: '{producto.Nombre}' → '{model.Nombre}'");
                producto.Nombre = model.Nombre;
            }

            if (producto.PrecioCompra != model.PrecioCompra)
            {
                cambios.Add($"PrecioCompra: '{producto.PrecioCompra}' → '{model.PrecioCompra}'");
                producto.PrecioCompra = model.PrecioCompra;
            }

            if (producto.PrecioVenta != model.PrecioVenta)
            {
                cambios.Add($"PrecioVenta: '{producto.PrecioVenta}' → '{model.PrecioVenta}'");
                producto.PrecioVenta = model.PrecioVenta;
            }

            if (producto.Categoria != model.Categoria)
            {
                cambios.Add($"Categoría: '{producto.Categoria}' → '{model.Categoria}'");
                producto.Categoria = model.Categoria;
            }

            if (producto.Descripcion != model.Descripcion)
            {
                cambios.Add($"Descripción: '{producto.Descripcion}' → '{model.Descripcion}'");
                producto.Descripcion = model.Descripcion;
            }

            if (producto.UnidadMedida != model.UnidadMedida)
            {
                cambios.Add($"UnidadMedida: '{producto.UnidadMedida}' → '{model.UnidadMedida}'");
                producto.UnidadMedida = model.UnidadMedida;
            }

            producto.Activo = model.Activo;

            await _context.SaveChangesAsync();

            if (cambios.Any())
                await _auditService.RegistrarEdicionProductoAsync(producto);

            _logger.LogInformation($"Producto {producto.Nombre} editado por {User.Identity!.Name}");

            return JsonSuccess("Producto actualizado exitosamente");
        }


        // POST: Productos/Deactivate/5
        [HttpPost]
        public async Task<IActionResult> Deactivate(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return JsonError("Producto no encontrado.");

            producto.Activo = !producto.Activo;
            await _context.SaveChangesAsync();

            var estado = producto.Activo ? "activado" : "desactivado";

            _logger.LogInformation($"Producto {producto.Nombre} {estado} por {User.Identity!.Name}");

            // Registrar auditoría SOLO si se desactiva
            if (!producto.Activo)
                await _auditService.RegistrarDesactivacionProductoAsync(producto);

            return JsonSuccess($"Producto {estado} exitosamente");
        }



// GET: /Productos/GetEstadisticas
[HttpGet]
public async Task<IActionResult> GetEstadisticas()
{
    var totalProductos = await _context.Productos.CountAsync();
    var productosActivos = await _context.Productos.CountAsync(p => p.Activo);
    var productosInactivos = await _context.Productos.CountAsync(p => !p.Activo);
    
    // Contar categorías únicas
    var totalCategorias = await _context.Productos
        .Where(p => !string.IsNullOrEmpty(p.Categoria))
        .Select(p => p.Categoria)
        .Distinct()
        .CountAsync();

    return Json(new
    {
        totalProductos,
        productosActivos,
        productosInactivos,
        totalCategorias
    });
}

// GET: /Productos/GetProductos
[HttpGet]
public async Task<IActionResult> GetProductos()
{
    var productos = await _context.Productos
        .Select(p => new
        {
            p.Id,
            p.Codigo,
            p.Nombre,
            p.Descripcion,
            p.Categoria,
            p.UnidadMedida,
            p.PrecioCompra,
            p.PrecioVenta,
            p.Activo
        })
        .ToListAsync();

    return Json(productos);
}





        private async Task<string> GenerarCodigoProducto()
        {
            int count = await _context.Productos.CountAsync() + 1;
            return $"PROD-{count:0000}";
        }
    }
}