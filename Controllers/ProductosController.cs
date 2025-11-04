using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Puerto92.Data;
using Puerto92.Models;
using Puerto92.ViewModels;
using Puerto92.Services;
using System.Globalization;
using System.Text;

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
        public async Task<IActionResult> Index(int? categoriaId = null)
        {
            // Obtener TODOS los productos activos para las estadísticas
            var todosProductos = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo)
                .ToListAsync();

            // Pasar estadísticas totales en ViewBag
            ViewBag.TotalProductos = await _context.Productos.CountAsync();
            ViewBag.TotalActivos = todosProductos.Count;
            ViewBag.TotalInactivos = await _context.Productos.CountAsync(p => !p.Activo);
            ViewBag.TotalCategorias = await _context.Categorias
                .Where(c => c.Tipo == "Productos" && c.Activo)
                .CountAsync();

            var query = _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo)
                .AsQueryable();

            // Filtrar por categoría si se especifica
            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            var productos = await query
                .OrderBy(p => p.Codigo)
                .Select(p => new ProductoViewModel
                {
                    Id = p.Id,
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    CategoriaId = p.CategoriaId,
                    CategoriaNombre = p.Categoria!.Nombre,
                    Unidad = p.Unidad,
                    PrecioCompra = p.PrecioCompra,
                    PrecioVenta = p.PrecioVenta,
                    Descripcion = p.Descripcion,
                    Activo = p.Activo,
                    FechaCreacion = p.FechaCreacion,
                    FechaModificacion = p.FechaModificacion,
                    CreadoPor = p.CreadoPor,
                    ModificadoPor = p.ModificadoPor
                })
                .ToListAsync();

            // Obtener categorías de productos para el filtro
            ViewBag.Categorias = await _context.Categorias
                .Where(c => c.Tipo == "Productos" && c.Activo)
                .OrderBy(c => c.Orden)
                .ToListAsync();

            ViewBag.CategoriaFiltro = categoriaId;

            return View(productos);
        }

        // GET: Productos/GetProducto?id=1
        [HttpGet]
        public async Task<IActionResult> GetProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            var data = new
            {
                id = producto.Id,
                codigo = producto.Codigo,
                nombre = producto.Nombre,
                categoriaId = producto.CategoriaId,
                unidad = producto.Unidad,
                precioCompra = producto.PrecioCompra,
                precioVenta = producto.PrecioVenta,
                descripcion = producto.Descripcion ?? "",
                activo = producto.Activo
            };

            return Json(data);
        }

        // GET: Productos/GetCategorias
        [HttpGet]
        public async Task<IActionResult> GetCategorias()
        {
            var categorias = await _context.Categorias
                .Where(c => c.Tipo == "Productos" && c.Activo)
                .OrderBy(c => c.Orden)
                .Select(c => new
                {
                    value = c.Id,
                    text = c.Nombre
                })
                .ToListAsync();

            return Json(categorias);
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductoViewModel model)
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
                // Validar que la categoría existe y es de tipo "Productos"
                var categoria = await _context.Categorias.FindAsync(model.CategoriaId);
                if (categoria == null || categoria.Tipo != "Productos" || !categoria.Activo)
                {
                    if (IsAjaxRequest)
                        return JsonError("Categoría inválida o inactiva.");

                    SetErrorMessage("Categoría inválida o inactiva.");
                    return RedirectToAction(nameof(Index));
                }

                // Validar unidad
                if (!UnidadMedidaProducto.Unidades.Contains(model.Unidad))
                {
                    if (IsAjaxRequest)
                        return JsonError($"Unidad inválida: {model.Unidad}");

                    SetErrorMessage($"Unidad inválida: {model.Unidad}");
                    return RedirectToAction(nameof(Index));
                }

                // Validar que precio de venta >= precio de compra
                if (model.PrecioVenta < model.PrecioCompra)
                {
                    if (IsAjaxRequest)
                        return JsonError("El precio de venta debe ser mayor o igual al precio de compra.");

                    SetErrorMessage("El precio de venta debe ser mayor o igual al precio de compra.");
                    return RedirectToAction(nameof(Index));
                }

                // Generar código si no se proporciona
                string codigo = string.IsNullOrWhiteSpace(model.Codigo)
                    ? await GenerarCodigoUnico()
                    : model.Codigo.Trim();

                // Verificar que el código no exista
                if (await _context.Productos.AnyAsync(p => p.Codigo == codigo))
                {
                    codigo = await GenerarCodigoUnico();
                }

                var producto = new Producto
                {
                    Codigo = codigo,
                    Nombre = model.Nombre.Trim(),
                    CategoriaId = model.CategoriaId,
                    Unidad = model.Unidad,
                    PrecioCompra = model.PrecioCompra,
                    PrecioVenta = model.PrecioVenta,
                    Descripcion = model.Descripcion?.Trim(),
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    CreadoPor = User.Identity!.Name
                };

                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Producto '{producto.Nombre}' ({producto.Codigo}) creado por {User.Identity!.Name}");

                await _auditService.RegistrarCreacionProductoAsync(
                    codigoProducto: producto.Codigo,
                    nombreProducto: producto.Nombre,
                    categoriaNombre: categoria.Nombre);

                SetSuccessMessage($"Producto '{producto.Nombre}' creado exitosamente con código {codigo}");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al crear producto",
                    detalles: ex.Message);

                if (IsAjaxRequest)
                    return JsonError("Error al crear el producto. Por favor intenta nuevamente.");

                SetErrorMessage("Error al crear el producto. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Productos/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductoViewModel model)
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
                var producto = await _context.Productos
                    .Include(p => p.Categoria)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (producto == null)
                {
                    return NotFound();
                }

                // Validar que precio de venta >= precio de compra
                if (model.PrecioVenta < model.PrecioCompra)
                {
                    SetErrorMessage("El precio de venta debe ser mayor o igual al precio de compra.");
                    return RedirectToAction(nameof(Index));
                }

                // Detectar cambios para auditoría
                List<string> cambios = new List<string>();

                if (producto.Nombre != model.Nombre)
                    cambios.Add($"Nombre: '{producto.Nombre}' → '{model.Nombre}'");

                if (producto.PrecioCompra != model.PrecioCompra)
                    cambios.Add($"Precio Compra: S/ {producto.PrecioCompra:N2} → S/ {model.PrecioCompra:N2}");

                if (producto.PrecioVenta != model.PrecioVenta)
                    cambios.Add($"Precio Venta: S/ {producto.PrecioVenta:N2} → S/ {model.PrecioVenta:N2}");

                if (producto.Unidad != model.Unidad)
                    cambios.Add($"Unidad: '{producto.Unidad}' → '{model.Unidad}'");

                if (producto.Descripcion != model.Descripcion)
                    cambios.Add($"Descripción modificada");

                // Actualizar datos (código y categoría NO se modifican)
                producto.Nombre = model.Nombre.Trim();
                producto.Unidad = model.Unidad;
                producto.PrecioCompra = model.PrecioCompra;
                producto.PrecioVenta = model.PrecioVenta;
                producto.Descripcion = model.Descripcion?.Trim();
                producto.FechaModificacion = DateTime.Now;
                producto.ModificadoPor = User.Identity!.Name;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Producto '{producto.Nombre}' ({producto.Codigo}) editado por {User.Identity!.Name}");

                if (cambios.Any())
                {
                    await _auditService.RegistrarEdicionProductoAsync(
                        codigoProducto: producto.Codigo,
                        nombreProducto: producto.Nombre,
                        cambios: string.Join(", ", cambios));
                }

                SetSuccessMessage("Producto actualizado exitosamente. Los cambios se reflejarán en todos los locales.");

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(model.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar producto");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al editar producto",
                    detalles: ex.Message);

                SetErrorMessage("Error al actualizar el producto. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Productos/Desactivar/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id, string motivo)
        {
            try
            {
                var producto = await _context.Productos
                    .Include(p => p.Categoria)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (producto == null)
                {
                    return NotFound();
                }

                // Desactivar el producto (soft delete)
                producto.Activo = false;
                producto.FechaModificacion = DateTime.Now;
                producto.ModificadoPor = User.Identity!.Name;

                await _context.SaveChangesAsync();

                _logger.LogWarning($"Producto '{producto.Nombre}' ({producto.Codigo}) DESACTIVADO por {User.Identity!.Name}. Motivo: {motivo}");

                await _auditService.RegistrarDesactivacionProductoAsync(
                    codigoProducto: producto.Codigo,
                    nombreProducto: producto.Nombre,
                    motivo: motivo);

                SetSuccessMessage($"Producto '{producto.Nombre}' desactivado exitosamente. Ya no aparecerá en kardex futuros.");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar producto");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al desactivar producto",
                    detalles: ex.Message);

                SetErrorMessage("Error al desactivar el producto. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Productos/DescargarPlantilla
        [HttpGet]
        public IActionResult DescargarPlantilla()
        {
            try
            {
                var csv = new StringBuilder();

                // Header
                csv.AppendLine("Codigo;Nombre;Categoria;Unidad;PrecioCompra;PrecioVenta;Descripcion");

                // Instrucciones
                csv.AppendLine("# DEJA EL CODIGO VACIO (;) PARA QUE SE GENERE AUTOMATICAMENTE");
                csv.AppendLine("# Categorias: Usa el nombre EXACTO de las categorias de productos existentes");
                csv.AppendLine("# Unidades validas: Unidad, Kilogramo, Litro, Caja, Docena, Bolsa, Paquete");
                csv.AppendLine("# Precios: usa punto como decimal (12.50) y PrecioVenta >= PrecioCompra");
                csv.AppendLine("");

                // Ejemplos
                csv.AppendLine(";Arroz Costeño 1kg;Abarrotes;Kilogramo;3.50;4.20;Arroz extra");
                csv.AppendLine(";Aceite Primor 1L;Abarrotes;Litro;8.50;10.00;Aceite vegetal");
                csv.AppendLine(";Coca Cola 2L;Bebidas;Unidad;4.50;6.00;Gaseosa 2 litros");
                csv.AppendLine(";Papel Higiénico Elite;Limpieza;Paquete;12.00;15.00;Paquete x 6 rollos");

                // Agregar BOM UTF-8 para Excel
                var bomBytes = Encoding.UTF8.GetPreamble();
                var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
                var bytes = bomBytes.Concat(csvBytes).ToArray();

                return File(bytes, "text/csv", $"Plantilla_Productos_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar plantilla");
                SetErrorMessage("Error al descargar la plantilla");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Productos/CargaMasiva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargaMasiva(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                SetErrorMessage("Debe seleccionar un archivo CSV válido");
                return RedirectToAction(nameof(Index));
            }

            if (!archivo.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                SetErrorMessage("El archivo debe ser formato CSV");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _logger.LogInformation($"Iniciando carga masiva. Archivo: {archivo.FileName}, Tamaño: {archivo.Length} bytes");

                var resultado = await ProcesarCargaMasiva(archivo);

                if (resultado.Exitoso)
                {
                    await _auditService.RegistrarCargaMasivaProductosAsync(
                        cantidad: resultado.ProductosCargados,
                        resultado: resultado.Mensaje);

                    SetSuccessMessage($"✅ {resultado.ProductosCargados} producto(s) cargado(s) exitosamente");
                }
                else
                {
                    _logger.LogWarning($"Carga masiva con errores: {resultado.FilasConError} filas con problemas");

                    var mensajeError = $"❌ Se encontraron {resultado.FilasConError} fila(s) con errores. ";

                    if (resultado.Errores.Count <= 5)
                    {
                        mensajeError += "Errores: " + string.Join("; ", resultado.Errores);
                    }
                    else
                    {
                        mensajeError += "Primeros 5 errores: " + string.Join("; ", resultado.Errores.Take(5)) +
                                       $" (y {resultado.Errores.Count - 5} más...)";
                    }

                    SetErrorMessage(mensajeError);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en carga masiva");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error en carga masiva de productos",
                    detalles: ex.Message);

                SetErrorMessage($"Error al procesar el archivo: {ex.Message}. Por favor verifica el formato del CSV.");
                return RedirectToAction(nameof(Index));
            }
        }

        // MÉTODOS AUXILIARES

        private async Task<string> GenerarCodigoUnico()
        {
            var ultimoProducto = await _context.Productos
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            int siguienteNumero = 1;
            if (ultimoProducto != null)
            {
                var partes = ultimoProducto.Codigo.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out int numero))
                {
                    siguienteNumero = numero + 1;
                }
            }

            string nuevoCodigo;
            do
            {
                nuevoCodigo = $"PROD-{siguienteNumero:D3}";
                siguienteNumero++;
            }
            while (await _context.Productos.AnyAsync(p => p.Codigo == nuevoCodigo));

            return nuevoCodigo;
        }

        private async Task<CargaMasivaProductosResultado> ProcesarCargaMasiva(IFormFile archivo)
        {
            var resultado = new CargaMasivaProductosResultado();
            var productosImportados = new List<Producto>();
            var codigosGenerados = new HashSet<string>();

            // Obtener categorías de productos
            var categorias = await _context.Categorias
                .Where(c => c.Tipo == "Productos" && c.Activo)
                .ToListAsync();

            if (!categorias.Any())
            {
                resultado.Errores.Add("No hay categorías de productos activas en el sistema. Cree categorías antes de importar.");
                return resultado;
            }

            using (var reader = new StreamReader(archivo.OpenReadStream(), Encoding.UTF8))
            {
                string? headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    resultado.Errores.Add("El archivo está vacío");
                    _logger.LogWarning("Archivo CSV vacío");
                    return resultado;
                }

                char separador = headerLine.Contains(';') ? ';' : ',';
                _logger.LogInformation($"Separador detectado: '{separador}'");

                var columnas = headerLine.Split(separador).Select(c => c.Trim()).ToList();
                _logger.LogInformation($"Columnas encontradas: {string.Join(", ", columnas)}");

                if (columnas.Count < 6)
                {
                    resultado.Errores.Add($"El archivo debe tener al menos 6 columnas (encontradas: {columnas.Count}). Formato: Codigo;Nombre;Categoria;Unidad;PrecioCompra;PrecioVenta;Descripcion");
                    return resultado;
                }

                int numeroFila = 1;

                while (!reader.EndOfStream)
                {
                    numeroFila++;
                    var linea = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(linea) || linea.Trim().All(c => c == separador || char.IsWhiteSpace(c)))
                    {
                        _logger.LogDebug($"Fila {numeroFila}: Línea vacía, omitiendo");
                        continue;
                    }

                    // Ignorar líneas de comentarios
                    if (linea.TrimStart().StartsWith("#"))
                    {
                        _logger.LogDebug($"Fila {numeroFila}: Comentario, omitiendo");
                        continue;
                    }

                    var dto = ParsearLineaCSV(linea, numeroFila, separador, categorias);

                    if (!dto.EsValido)
                    {
                        resultado.FilasConError++;
                        resultado.Errores.AddRange(dto.Errores);
                        _logger.LogWarning($"Fila {numeroFila} con errores: {string.Join(", ", dto.Errores)}");
                        continue;
                    }

                    string codigo;
                    if (string.IsNullOrWhiteSpace(dto.Codigo))
                    {
                        codigo = await GenerarCodigoUnicoParaLote(codigosGenerados);
                        _logger.LogInformation($"Fila {numeroFila}: Código generado automáticamente: {codigo}");
                    }
                    else
                    {
                        codigo = dto.Codigo.Trim();

                        if (await _context.Productos.AnyAsync(p => p.Codigo == codigo) ||
                            codigosGenerados.Contains(codigo))
                        {
                            resultado.FilasConError++;
                            resultado.Errores.Add($"Fila {numeroFila}: El código '{codigo}' ya existe");
                            _logger.LogWarning($"Fila {numeroFila}: Código duplicado: {codigo}");
                            continue;
                        }
                    }

                    codigosGenerados.Add(codigo);

                    // Buscar categoría
                    var categoria = categorias.FirstOrDefault(c => 
                        c.Nombre.Equals(dto.Categoria, StringComparison.OrdinalIgnoreCase));

                    var producto = new Producto
                    {
                        Codigo = codigo,
                        Nombre = dto.Nombre.Trim(),
                        CategoriaId = categoria!.Id,
                        Unidad = dto.Unidad,
                        PrecioCompra = dto.PrecioCompra,
                        PrecioVenta = dto.PrecioVenta,
                        Descripcion = dto.Descripcion?.Trim(),
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        CreadoPor = User.Identity!.Name
                    };

                    productosImportados.Add(producto);
                    _logger.LogDebug($"Fila {numeroFila}: Producto '{producto.Nombre}' ({codigo}) listo para importar");
                }
            }

            if (resultado.FilasConError > 0)
            {
                resultado.Mensaje = $"Archivo contiene {resultado.FilasConError} fila(s) con errores. No se importó ningún producto.";
                _logger.LogWarning(resultado.Mensaje);
                return resultado;
            }

            if (productosImportados.Count == 0)
            {
                resultado.Errores.Add("No se encontraron productos válidos para importar.");
                _logger.LogWarning("No hay productos válidos para importar");
                return resultado;
            }

            _context.Productos.AddRange(productosImportados);
            await _context.SaveChangesAsync();

            resultado.Exitoso = true;
            resultado.ProductosCargados = productosImportados.Count;
            resultado.Mensaje = $"{productosImportados.Count} producto(s) importado(s) correctamente";

            _logger.LogInformation($"Carga masiva exitosa: {productosImportados.Count} productos");

            return resultado;
        }

        private async Task<string> GenerarCodigoUnicoParaLote(HashSet<string> codigosGenerados)
        {
            var ultimoProducto = await _context.Productos
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            int siguienteNumero = 1;

            if (ultimoProducto != null)
            {
                var partes = ultimoProducto.Codigo.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out int numero))
                {
                    siguienteNumero = numero + 1;
                }
            }

            string nuevoCodigo;
            do
            {
                nuevoCodigo = $"PROD-{siguienteNumero:D3}";
                siguienteNumero++;
            }
            while (await _context.Productos.AnyAsync(p => p.Codigo == nuevoCodigo) ||
                   codigosGenerados.Contains(nuevoCodigo));

            return nuevoCodigo;
        }

        private ProductoImportDto ParsearLineaCSV(
            string linea, 
            int numeroFila, 
            char separador, 
            List<Categoria> categorias)
        {
            var dto = new ProductoImportDto { NumeroFila = numeroFila };

            var campos = linea.Split(separador);

            if (campos.Length < 6)
            {
                dto.Errores.Add($"Fila {numeroFila}: Formato inválido, se esperan al menos 6 columnas");
                return dto;
            }

            try
            {
                // Código (opcional)
                dto.Codigo = campos[0].Trim();

                // Nombre (requerido)
                dto.Nombre = campos[1].Trim();
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                {
                    dto.Errores.Add($"Fila {numeroFila}: El nombre es obligatorio");
                }

                // Categoría (requerido y debe existir)
                dto.Categoria = campos[2].Trim();
                if (string.IsNullOrWhiteSpace(dto.Categoria))
                {
                    dto.Errores.Add($"Fila {numeroFila}: La categoría es obligatoria");
                }
                else
                {
                    var categoriaExiste = categorias.Any(c => 
                        c.Nombre.Equals(dto.Categoria, StringComparison.OrdinalIgnoreCase));
                    
                    if (!categoriaExiste)
                    {
                        var categoriasDisponibles = string.Join(", ", categorias.Select(c => c.Nombre));
                        dto.Errores.Add($"Fila {numeroFila}: Categoría '{dto.Categoria}' no existe. Disponibles: {categoriasDisponibles}");
                    }
                }

                // Unidad (requerido y validado)
                dto.Unidad = campos[3].Trim();
                if (string.IsNullOrWhiteSpace(dto.Unidad))
                {
                    dto.Errores.Add($"Fila {numeroFila}: La unidad es obligatoria");
                }
                else if (!UnidadMedidaProducto.Unidades.Contains(dto.Unidad))
                {
                    dto.Errores.Add($"Fila {numeroFila}: Unidad inválida '{dto.Unidad}'");
                }

                // Precio Compra
                dto.PrecioCompraStr = campos[4].Trim();
                if (string.IsNullOrWhiteSpace(dto.PrecioCompraStr))
                {
                    dto.Errores.Add($"Fila {numeroFila}: El precio de compra es obligatorio");
                }
                else
                {
                    var precioNormalizado = dto.PrecioCompraStr.Replace(',', '.');

                    if (!decimal.TryParse(precioNormalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precioCompra) || precioCompra <= 0)
                    {
                        dto.Errores.Add($"Fila {numeroFila}: Precio de compra inválido '{dto.PrecioCompraStr}'");
                    }
                    else
                    {
                        dto.PrecioCompra = precioCompra;
                    }
                }

                // Precio Venta
                dto.PrecioVentaStr = campos[5].Trim();
                if (string.IsNullOrWhiteSpace(dto.PrecioVentaStr))
                {
                    dto.Errores.Add($"Fila {numeroFila}: El precio de venta es obligatorio");
                }
                else
                {
                    var precioNormalizado = dto.PrecioVentaStr.Replace(',', '.');

                    if (!decimal.TryParse(precioNormalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precioVenta) || precioVenta <= 0)
                    {
                        dto.Errores.Add($"Fila {numeroFila}: Precio de venta inválido '{dto.PrecioVentaStr}'");
                    }
                    else
                    {
                        dto.PrecioVenta = precioVenta;

                        // Validar que precio venta >= precio compra
                        if (dto.PrecioVenta < dto.PrecioCompra)
                        {
                            dto.Errores.Add($"Fila {numeroFila}: Precio de venta (S/ {dto.PrecioVenta:N2}) debe ser >= precio de compra (S/ {dto.PrecioCompra:N2})");
                        }
                    }
                }

                // Descripción (opcional)
                if (campos.Length > 6 && !string.IsNullOrWhiteSpace(campos[6]))
                {
                    dto.Descripcion = campos[6].Trim();
                }
            }
            catch (Exception ex)
            {
                dto.Errores.Add($"Fila {numeroFila}: Error al procesar datos - {ex.Message}");
            }

            return dto;
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}