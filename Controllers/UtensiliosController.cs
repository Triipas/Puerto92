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
    [Authorize(Roles = "Contador")]
    public class UtensiliosController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UtensiliosController> _logger;
        private readonly IAuditService _auditService;

        public UtensiliosController(
            ApplicationDbContext context,
            ILogger<UtensiliosController> logger,
            IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        // GET: Utensilios
        public async Task<IActionResult> Index(string? tipo = null)
        {
            // Obtener TODOS los utensilios activos para las estadísticas
            var todosUtensilios = await _context.Utensilios
                .Where(u => u.Activo)
                .ToListAsync();

            // Pasar estadísticas totales en ViewBag
            ViewBag.TotalActivos = todosUtensilios.Count;
            ViewBag.TotalCocina = todosUtensilios.Count(u => u.Tipo == "Cocina");
            ViewBag.TotalMozos = todosUtensilios.Count(u => u.Tipo == "Mozos");
            ViewBag.TotalVajilla = todosUtensilios.Count(u => u.Tipo == "Vajilla");

            var query = _context.Utensilios.Where(u => u.Activo).AsQueryable();

            // Filtrar por tipo si se especifica
            if (!string.IsNullOrEmpty(tipo) && TipoUtensilio.Tipos.Contains(tipo))
            {
                query = query.Where(u => u.Tipo == tipo);
            }

            var utensilios = await query
                .OrderBy(u => u.Tipo)
                .ThenBy(u => u.Codigo)
                .Select(u => new UtensilioViewModel
                {
                    Id = u.Id,
                    Codigo = u.Codigo,
                    Nombre = u.Nombre,
                    Tipo = u.Tipo,
                    Unidad = u.Unidad,
                    Precio = u.Precio,
                    Descripcion = u.Descripcion,
                    Activo = u.Activo,
                    FechaCreacion = u.FechaCreacion,
                    FechaModificacion = u.FechaModificacion,
                    CreadoPor = u.CreadoPor,
                    ModificadoPor = u.ModificadoPor
                })
                .ToListAsync();

            ViewBag.TipoFiltro = tipo;
            return View(utensilios);
        }

        // GET: Utensilios/GetUtensilio?id=1
        [HttpGet]
        public async Task<IActionResult> GetUtensilio(int id)
        {
            var utensilio = await _context.Utensilios.FindAsync(id);

            if (utensilio == null)
            {
                return NotFound();
            }

            var data = new
            {
                id = utensilio.Id,
                codigo = utensilio.Codigo,
                nombre = utensilio.Nombre,
                tipo = utensilio.Tipo,
                unidad = utensilio.Unidad,
                precio = utensilio.Precio,
                descripcion = utensilio.Descripcion ?? "",
                activo = utensilio.Activo
            };

            return Json(data);
        }

        // POST: Utensilios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UtensilioViewModel model)
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
                // Validar tipo
                if (!TipoUtensilio.Tipos.Contains(model.Tipo))
                {
                    if (IsAjaxRequest)
                        return JsonError($"Tipo inválido: {model.Tipo}. Debe ser Cocina, Mozos o Vajilla.");

                    SetErrorMessage($"Tipo inválido: {model.Tipo}");
                    return RedirectToAction(nameof(Index));
                }

                // Validar unidad
                if (!UnidadMedida.Unidades.Contains(model.Unidad))
                {
                    if (IsAjaxRequest)
                        return JsonError($"Unidad inválida: {model.Unidad}");

                    SetErrorMessage($"Unidad inválida: {model.Unidad}");
                    return RedirectToAction(nameof(Index));
                }

                // Generar código si no se proporciona
                string codigo = string.IsNullOrWhiteSpace(model.Codigo)
                    ? await GenerarCodigoUnico()
                    : model.Codigo.Trim();

                // Verificar que el código no exista
                if (await _context.Utensilios.AnyAsync(u => u.Codigo == codigo))
                {
                    codigo = await GenerarCodigoUnico();
                }

                var utensilio = new Utensilio
                {
                    Codigo = codigo,
                    Nombre = model.Nombre.Trim(),
                    Tipo = model.Tipo,
                    Unidad = model.Unidad,
                    Precio = model.Precio,
                    Descripcion = model.Descripcion?.Trim(),
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    CreadoPor = User.Identity!.Name
                };

                _context.Utensilios.Add(utensilio);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Utensilio '{utensilio.Nombre}' ({utensilio.Codigo}) creado por {User.Identity!.Name}");

                await _auditService.RegistrarCreacionUtensilioAsync(
                    codigoUtensilio: utensilio.Codigo,
                    nombreUtensilio: utensilio.Nombre,
                    tipo: utensilio.Tipo);

                SetSuccessMessage($"Utensilio '{utensilio.Nombre}' creado exitosamente con código {codigo}");

                // Redirigir a la vista con el filtro de tipo correcto
                return RedirectToAction(nameof(Index), new { tipo = model.Tipo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear utensilio");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al crear utensilio",
                    detalles: ex.Message);

                if (IsAjaxRequest)
                    return JsonError("Error al crear el utensilio. Por favor intenta nuevamente.");

                SetErrorMessage("Error al crear el utensilio. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Utensilios/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UtensilioViewModel model)
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
                var utensilio = await _context.Utensilios.FindAsync(id);

                if (utensilio == null)
                {
                    return NotFound();
                }

                // Detectar cambios para auditoría
                List<string> cambios = new List<string>();

                if (utensilio.Nombre != model.Nombre)
                    cambios.Add($"Nombre: '{utensilio.Nombre}' → '{model.Nombre}'");

                if (utensilio.Precio != model.Precio)
                    cambios.Add($"Precio: S/ {utensilio.Precio:N2} → S/ {model.Precio:N2}");

                if (utensilio.Unidad != model.Unidad)
                    cambios.Add($"Unidad: '{utensilio.Unidad}' → '{model.Unidad}'");

                if (utensilio.Descripcion != model.Descripcion)
                    cambios.Add($"Descripción modificada");

                // Actualizar datos (código y tipo NO se modifican)
                utensilio.Nombre = model.Nombre.Trim();
                utensilio.Unidad = model.Unidad;
                utensilio.Precio = model.Precio;
                utensilio.Descripcion = model.Descripcion?.Trim();
                utensilio.FechaModificacion = DateTime.Now;
                utensilio.ModificadoPor = User.Identity!.Name;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Utensilio '{utensilio.Nombre}' ({utensilio.Codigo}) editado por {User.Identity!.Name}");

                if (cambios.Any())
                {
                    await _auditService.RegistrarEdicionUtensilioAsync(
                        codigoUtensilio: utensilio.Codigo,
                        nombreUtensilio: utensilio.Nombre,
                        cambios: string.Join(", ", cambios));
                }

                SetSuccessMessage("Utensilio actualizado exitosamente");

                return RedirectToAction(nameof(Index), new { tipo = utensilio.Tipo });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UtensilioExists(model.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar utensilio");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al editar utensilio",
                    detalles: ex.Message);

                SetErrorMessage("Error al actualizar el utensilio. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Utensilios/Desactivar/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id, string motivo)
        {
            try
            {
                var utensilio = await _context.Utensilios.FindAsync(id);

                if (utensilio == null)
                {
                    return NotFound();
                }

                // Desactivar el utensilio (soft delete)
                utensilio.Activo = false;
                utensilio.FechaModificacion = DateTime.Now;
                utensilio.ModificadoPor = User.Identity!.Name;

                await _context.SaveChangesAsync();

                _logger.LogWarning($"Utensilio '{utensilio.Nombre}' ({utensilio.Codigo}) DESACTIVADO por {User.Identity!.Name}. Motivo: {motivo}");

                await _auditService.RegistrarDesactivacionUtensilioAsync(
                    codigoUtensilio: utensilio.Codigo,
                    nombreUtensilio: utensilio.Nombre);

                SetSuccessMessage($"Utensilio '{utensilio.Nombre}' desactivado exitosamente");

                return RedirectToAction(nameof(Index), new { tipo = utensilio.Tipo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar utensilio");

                await _auditService.RegistrarErrorSistemaAsync(
                    error: "Error al desactivar utensilio",
                    detalles: ex.Message);

                SetErrorMessage("Error al desactivar el utensilio. Por favor intenta nuevamente.");
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Utensilios/DescargarPlantilla
        [HttpGet]
        public IActionResult DescargarPlantilla()
        {
            try
            {
                var csv = new StringBuilder();

                // Header
                csv.AppendLine("Codigo;Nombre;Tipo;Unidad;Precio;Descripcion");

                // ⭐ Instrucciones claras en comentarios (Excel ignora líneas que empiezan con #)
                csv.AppendLine("# DEJA EL CODIGO VACIO (;) PARA QUE SE GENERE AUTOMATICAMENTE");
                csv.AppendLine("# Tipos validos: Cocina, Mozos, Vajilla");
                csv.AppendLine("# Unidades validas: Unidad, Juego, Docena, Par, Set");
                csv.AppendLine("# Precio: usa punto como decimal (85.00)");
                csv.AppendLine("");

                // Ejemplos con código VACÍO
                csv.AppendLine(";Sartén Antiadherente 28cm;Cocina;Unidad;85.00;Sartén profesional de 28cm");
                csv.AppendLine(";Cuchillo Chef 20cm;Cocina;Unidad;120.00;Cuchillo de chef acero inoxidable");
                csv.AppendLine(";Bandeja Rectangular;Mozos;Unidad;45.00;Bandeja antideslizante");
                csv.AppendLine(";Plato Hondo Porcelana;Vajilla;Unidad;25.00;Plato hondo blanco");

                // Agregar BOM UTF-8 para Excel
                var bomBytes = Encoding.UTF8.GetPreamble();
                var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
                var bytes = bomBytes.Concat(csvBytes).ToArray();

                return File(bytes, "text/csv", $"Plantilla_Utensilios_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar plantilla");
                SetErrorMessage("Error al descargar la plantilla");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Utensilios/CargaMasiva
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
                    await _auditService.RegistrarCargaMasivaUtensiliosAsync(
                        cantidad: resultado.UtensiliosCargados,
                        resultado: resultado.Mensaje);

                    SetSuccessMessage($"✅ {resultado.UtensiliosCargados} utensilio(s) cargado(s) exitosamente");
                }
                else
                {
                    _logger.LogWarning($"Carga masiva con errores: {resultado.FilasConError} filas con problemas");

                    // Construir mensaje de error detallado
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
                    error: "Error en carga masiva de utensilios",
                    detalles: ex.Message);

                SetErrorMessage($"Error al procesar el archivo: {ex.Message}. Por favor verifica el formato del CSV.");
                return RedirectToAction(nameof(Index));
            }
        }

        // Método auxiliar: Generar código único
        /// <summary>
        /// Generar código único (para creación individual)
        /// </summary>
        private async Task<string> GenerarCodigoUnico()
        {
            var ultimoUtensilio = await _context.Utensilios
                .OrderByDescending(u => u.Id)
                .FirstOrDefaultAsync();

            int siguienteNumero = 1;
            if (ultimoUtensilio != null)
            {
                var partes = ultimoUtensilio.Codigo.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out int numero))
                {
                    siguienteNumero = numero + 1;
                }
            }

            string nuevoCodigo;
            do
            {
                nuevoCodigo = $"UTEN-{siguienteNumero:D3}";
                siguienteNumero++;
            }
            while (await _context.Utensilios.AnyAsync(u => u.Codigo == nuevoCodigo));

            return nuevoCodigo;
        }

        // Método auxiliar: Procesar carga masiva
        private async Task<CargaMasivaResultado> ProcesarCargaMasiva(IFormFile archivo)
        {
            var resultado = new CargaMasivaResultado();
            var utensiliosImportados = new List<Utensilio>();
            var codigosGenerados = new HashSet<string>(); // ⭐ NUEVO: Rastrear códigos generados

            using (var reader = new StreamReader(archivo.OpenReadStream(), Encoding.UTF8))
            {
                string? headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    resultado.Errores.Add("El archivo está vacío");
                    _logger.LogWarning("Archivo CSV vacío");
                    return resultado;
                }

                // Detectar separador automáticamente
                char separador = headerLine.Contains(';') ? ';' : ',';
                _logger.LogInformation($"Separador detectado: '{separador}'");

                // Validar que el header tenga las columnas esperadas
                var columnas = headerLine.Split(separador).Select(c => c.Trim()).ToList();
                _logger.LogInformation($"Columnas encontradas: {string.Join(", ", columnas)}");

                if (columnas.Count < 5)
                {
                    resultado.Errores.Add($"El archivo debe tener al menos 5 columnas (encontradas: {columnas.Count}). Formato esperado: Codigo;Nombre;Tipo;Unidad;Precio;Descripcion");
                    return resultado;
                }

                int numeroFila = 1;

                while (!reader.EndOfStream)
                {
                    numeroFila++;
                    var linea = await reader.ReadLineAsync();

                    // Ignorar líneas vacías o con solo separadores
                    if (string.IsNullOrWhiteSpace(linea) ||
                        linea.Trim().All(c => c == separador || char.IsWhiteSpace(c)))
                    {
                        _logger.LogDebug($"Fila {numeroFila}: Línea vacía, omitiendo");
                        continue;
                    }

                    // Validar que la línea tenga contenido real
                    var campos = linea.Split(separador);
                    bool todasVacias = campos.All(c => string.IsNullOrWhiteSpace(c));

                    if (todasVacias)
                    {
                        _logger.LogDebug($"Fila {numeroFila}: Todos los campos vacíos, omitiendo");
                        continue;
                    }

                    var dto = ParsearLineaCSV(linea, numeroFila, separador);

                    if (!dto.EsValido)
                    {
                        resultado.FilasConError++;
                        resultado.Errores.AddRange(dto.Errores);
                        _logger.LogWarning($"Fila {numeroFila} con errores: {string.Join(", ", dto.Errores)}");
                        continue;
                    }

                    // ⭐ NUEVO: Generar código único considerando códigos ya generados
                    string codigo;
                    if (string.IsNullOrWhiteSpace(dto.Codigo))
                    {
                        // Generar código único
                        codigo = await GenerarCodigoUnicoParaLote(codigosGenerados);
                        _logger.LogInformation($"Fila {numeroFila}: Código generado automáticamente: {codigo}");
                    }
                    else
                    {
                        codigo = dto.Codigo.Trim();

                        // Verificar que no exista en BD ni en el lote actual
                        if (await _context.Utensilios.AnyAsync(u => u.Codigo == codigo) ||
                            codigosGenerados.Contains(codigo))
                        {
                            resultado.FilasConError++;
                            resultado.Errores.Add($"Fila {numeroFila}: El código '{codigo}' ya existe");
                            _logger.LogWarning($"Fila {numeroFila}: Código duplicado: {codigo}");
                            continue;
                        }
                    }

                    // Agregar código a la lista de generados
                    codigosGenerados.Add(codigo);

                    var utensilio = new Utensilio
                    {
                        Codigo = codigo,
                        Nombre = dto.Nombre.Trim(),
                        Tipo = dto.Tipo,
                        Unidad = dto.Unidad,
                        Precio = dto.Precio,
                        Descripcion = dto.Descripcion?.Trim(),
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        CreadoPor = User.Identity!.Name
                    };

                    utensiliosImportados.Add(utensilio);
                    _logger.LogDebug($"Fila {numeroFila}: Utensilio '{utensilio.Nombre}' ({codigo}) listo para importar");
                }
            }

            if (resultado.FilasConError > 0)
            {
                resultado.Mensaje = $"Archivo contiene {resultado.FilasConError} fila(s) con errores. No se importó ningún utensilio.";
                _logger.LogWarning(resultado.Mensaje);
                return resultado;
            }

            if (utensiliosImportados.Count == 0)
            {
                resultado.Errores.Add("No se encontraron utensilios válidos para importar. Verifica que el archivo tenga datos después de la fila de encabezados.");
                _logger.LogWarning("No hay utensilios válidos para importar");
                return resultado;
            }

            // Importar todos los utensilios
            _context.Utensilios.AddRange(utensiliosImportados);
            await _context.SaveChangesAsync();

            resultado.Exitoso = true;
            resultado.UtensiliosCargados = utensiliosImportados.Count;
            resultado.Mensaje = $"{utensiliosImportados.Count} utensilio(s) importado(s) correctamente";

            _logger.LogInformation($"Carga masiva exitosa: {utensiliosImportados.Count} utensilios importados con códigos: {string.Join(", ", codigosGenerados)}");

            return resultado;
        }
        private async Task<string> GenerarCodigoUnicoParaLote(HashSet<string> codigosGenerados)
        {
            // Obtener el último utensilio de la base de datos
            var ultimoUtensilio = await _context.Utensilios
                .OrderByDescending(u => u.Id)
                .FirstOrDefaultAsync();

            int siguienteNumero = 1;

            if (ultimoUtensilio != null)
            {
                var partes = ultimoUtensilio.Codigo.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out int numero))
                {
                    siguienteNumero = numero + 1;
                }
            }

            // Verificar contra BD y códigos ya generados en este lote
            string nuevoCodigo;
            do
            {
                nuevoCodigo = $"UTEN-{siguienteNumero:D3}";
                siguienteNumero++;
            }
            while (await _context.Utensilios.AnyAsync(u => u.Codigo == nuevoCodigo) ||
                   codigosGenerados.Contains(nuevoCodigo));

            return nuevoCodigo;
        }

        // Método auxiliar: Parsear línea CSV
        private UtensilioImportDto ParsearLineaCSV(string linea, int numeroFila, char separador)
        {
            var dto = new UtensilioImportDto { NumeroFila = numeroFila };

            // Dividir por el separador detectado
            var campos = linea.Split(separador);

            if (campos.Length < 5)
            {
                dto.Errores.Add($"Fila {numeroFila}: Formato inválido, se esperan al menos 5 columnas separadas por '{separador}' (encontradas: {campos.Length})");
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

                // Tipo (requerido y validado)
                dto.Tipo = campos[2].Trim();
                if (string.IsNullOrWhiteSpace(dto.Tipo))
                {
                    dto.Errores.Add($"Fila {numeroFila}: El tipo es obligatorio");
                }
                else if (!TipoUtensilio.Tipos.Contains(dto.Tipo))
                {
                    dto.Errores.Add($"Fila {numeroFila}: Tipo inválido '{dto.Tipo}'. Debe ser: Cocina, Mozos o Vajilla");
                }

                // Unidad (requerido y validado)
                dto.Unidad = campos[3].Trim();
                if (string.IsNullOrWhiteSpace(dto.Unidad))
                {
                    dto.Errores.Add($"Fila {numeroFila}: La unidad es obligatoria");
                }
                else if (!UnidadMedida.Unidades.Contains(dto.Unidad))
                {
                    dto.Errores.Add($"Fila {numeroFila}: Unidad inválida '{dto.Unidad}'. Debe ser: Unidad, Juego, Docena, Par o Set");
                }

                // Precio (requerido y validado)
                dto.PrecioStr = campos[4].Trim();
                if (string.IsNullOrWhiteSpace(dto.PrecioStr))
                {
                    dto.Errores.Add($"Fila {numeroFila}: El precio es obligatorio");
                }
                else
                {
                    // Intentar parsear con diferentes formatos (. y ,)
                    var precioNormalizado = dto.PrecioStr.Replace(',', '.');

                    if (!decimal.TryParse(precioNormalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precio) || precio <= 0)
                    {
                        dto.Errores.Add($"Fila {numeroFila}: Precio inválido '{dto.PrecioStr}'. Debe ser un número mayor a 0");
                    }
                    else
                    {
                        dto.Precio = precio;
                    }
                }

                // Descripción (opcional)
                if (campos.Length > 5 && !string.IsNullOrWhiteSpace(campos[5]))
                {
                    dto.Descripcion = campos[5].Trim();
                }
            }
            catch (Exception ex)
            {
                dto.Errores.Add($"Fila {numeroFila}: Error al procesar datos - {ex.Message}");
            }

            return dto;
        }

        // Método auxiliar
        private bool UtensilioExists(int id)
        {
            return _context.Utensilios.Any(e => e.Id == id);
        }
    }
}