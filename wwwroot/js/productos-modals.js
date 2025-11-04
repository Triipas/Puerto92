/**
 * Gestión de Catálogo de Productos - Supervisora de Calidad
 * Puerto 92 - Sistema de Gestión
 */

// Variables globales
let productos = [];
let categorias = [];

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

/**
 * Función de inicialización que se ejecuta cada vez que se carga la página
 */
function initCatalogoProductos() {
    console.log('🔄 Inicializando catálogo de productos...');
    
    // Limpiar datos anteriores
    productos = [];
    categorias = [];
    
    // Cargar datos y configurar listeners
    cargarEstadisticas();
    cargarProductos();
    cargarCategorias();
    setupSearch();
    setupModalEventListeners();
    setupFormSubmits();
    setupCategoryFilter();
    
    console.log('✅ Catálogo de productos inicializado correctamente');
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initCatalogoProductos);

// ⭐ Exponer función para reinicializar después de navegación SPA
window.initCatalogoProductos = initCatalogoProductos;

// ==========================================
// CARGA DE ESTADÍSTICAS
// ==========================================

/**
 * Cargar estadísticas del dashboard
 */
async function cargarEstadisticas() {
    console.log('📊 Cargando estadísticas...');
    
    try {
        const response = await fetch('/Productos/GetEstadisticas');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        const data = await response.json();
        
        // Actualizar las tarjetas de estadísticas
        actualizarEstadistica('totalProductos', data.totalProductos || 0);
        actualizarEstadistica('productosActivos', data.productosActivos || 0);
        actualizarEstadistica('productosInactivos', data.productosInactivos || 0);
        actualizarEstadistica('totalCategorias', data.totalCategorias || 0);
        
        console.log('✅ Estadísticas cargadas:', data);
        
    } catch (error) {
        console.error('❌ Error al cargar estadísticas:', error);
        // Mostrar 0 en caso de error
        actualizarEstadistica('totalProductos', 0);
        actualizarEstadistica('productosActivos', 0);
        actualizarEstadistica('productosInactivos', 0);
        actualizarEstadistica('totalCategorias', 0);
    }
}

/**
 * Actualizar valor de una estadística en el DOM
 */
function actualizarEstadistica(elementId, valor) {
    const element = document.getElementById(elementId);
    if (element) {
        element.textContent = valor;
    }
}

// ==========================================
// CARGA DE PRODUCTOS (TABLA)
// ==========================================

/**
 * Cargar lista de productos desde el servidor
 */
async function cargarProductos() {
    console.log('📥 Cargando productos...');
    
    try {
        const response = await fetch('/Productos/GetProductos');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        productos = await response.json();
        
        console.log(`✅ Cargados ${productos.length} productos`);
        
        // Renderizar tabla
        renderizarTabla();
        
    } catch (error) {
        console.error('❌ Error al cargar productos:', error);
        productos = [];
        renderizarTabla();
    }
}

/**
 * Cargar categorías disponibles
 */
async function cargarCategorias() {
    console.log('📥 Cargando categorías...');
    
    try {
        const response = await fetch('/Productos/GetCategorias');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        categorias = await response.json();
        
        console.log(`✅ Cargadas ${categorias.length} categorías`);
        
        // Llenar selects de categorías
        llenarSelectsCategorias();
        
    } catch (error) {
        console.error('❌ Error al cargar categorías:', error);
        categorias = [];
    }
}

/**
 * Llenar los selects de categorías en los modales
 */
function llenarSelectsCategorias() {
    const selects = ['createCategoria', 'editCategoria'];
    
    selects.forEach(selectId => {
        const select = document.getElementById(selectId);
        if (!select) return;
        
        // Limpiar opciones excepto la primera (placeholder)
        while (select.options.length > 1) {
            select.remove(1);
        }
        
        // Agregar categorías
        categorias.forEach(cat => {
            const option = document.createElement('option');
            option.value = cat.value;
            option.textContent = cat.text;
            select.appendChild(option);
        });
    });
    
    console.log('✅ Selects de categorías llenados');
}

/**
 * Renderizar tabla de productos
 */
function renderizarTabla() {
    const tbody = document.querySelector('#productosTable tbody');
    if (!tbody) {
        console.warn('⚠️ Tabla de productos no encontrada');
        return;
    }
    
    // Limpiar tabla
    tbody.innerHTML = '';
    
    if (productos.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" style="text-align: center; padding: 2rem; color: #64748B;">
                    <i class="fa-solid fa-inbox" style="font-size: 2rem; margin-bottom: 0.5rem; display: block;"></i>
                    No hay productos registrados
                </td>
            </tr>
        `;
        return;
    }
    
    // Renderizar cada producto
    productos.forEach(producto => {
        const row = document.createElement('tr');
        
        row.innerHTML = `
            <td><strong>${producto.codigo}</strong></td>
            <td>
                <div style="font-weight: 500;">${producto.nombre}</div>
                <small style="color: #64748B;">${producto.descripcion || ''}</small>
            </td>
            <td><span class="badge" style="background: #E0F2FE; color: #0369A1;">${producto.categoria}</span></td>
            <td>${producto.unidadMedida}</td>
            <td>S/ ${parseFloat(producto.precioCompra).toFixed(2)}</td>
            <td>S/ ${parseFloat(producto.precioVenta).toFixed(2)}</td>
            <td>
                <span class="badge badge-${producto.activo ? 'success' : 'danger'}">
                    ${producto.activo ? 'Activo' : 'Inactivo'}
                </span>
            </td>
            <td>
                <button onclick="openEditModal('${producto.id}')" class="btn-icon" title="Editar">
                    <i class="fa-solid fa-pen"></i>
                </button>
                <button onclick="openDeactivateModal('${producto.id}', '${producto.codigo}', '${producto.nombre}', '${producto.categoria}', '${producto.precioVenta}')" 
                        class="btn-icon btn-danger" title="Desactivar">
                    <i class="fa-solid fa-ban"></i>
                </button>
            </td>
        `;
        
        tbody.appendChild(row);
    });
    
    console.log(`✅ Tabla renderizada con ${productos.length} productos`);
}

// ==========================================
// BÚSQUEDA Y FILTROS
// ==========================================

/**
 * Configurar buscador en tiempo real
 */
function setupSearch() {
    const searchInput = document.getElementById('searchInput');
    if (!searchInput) {
        console.warn('⚠️ Input de búsqueda no encontrado');
        return;
    }
    
    console.log('🔍 Configurando búsqueda...');
    
    // Remover event listeners anteriores
    const newSearchInput = searchInput.cloneNode(true);
    searchInput.parentNode.replaceChild(newSearchInput, searchInput);
    
    newSearchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#productosTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const isVisible = text.includes(searchValue);
            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount++;
        });
        
        console.log(`✅ Búsqueda: "${searchValue}" - Mostrando ${visibleCount} de ${rows.length} productos`);
    });
    
    console.log('✅ Búsqueda configurada correctamente');
}

/**
 * Configurar filtro por categoría
 */
function setupCategoryFilter() {
    const categoryFilter = document.getElementById('categoryFilter');
    if (!categoryFilter) return;
    
    categoryFilter.addEventListener('change', function() {
        const selectedCategory = this.value.toLowerCase();
        const rows = document.querySelectorAll('#productosTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            if (selectedCategory === 'todas') {
                row.style.display = '';
                visibleCount++;
            } else {
                const categoryCell = row.cells[2]; // Columna de categoría
                if (categoryCell) {
                    const categoryText = categoryCell.textContent.toLowerCase();
                    const isVisible = categoryText.includes(selectedCategory);
                    row.style.display = isVisible ? '' : 'none';
                    if (isVisible) visibleCount++;
                }
            }
        });
        
        console.log(`✅ Filtro: "${selectedCategory}" - Mostrando ${visibleCount} productos`);
    });
}

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de agregar producto
 */
async function openCreateModal() {
    console.log('📝 Abriendo modal de agregar producto...');
    
    // Asegurar que las categorías estén cargadas
    if (categorias.length === 0) {
        await cargarCategorias();
    }
    
    const modal = document.getElementById('createModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    // Resetear formulario
    document.getElementById('createForm').reset();
    
    // Generar código automático
    generarCodigoAutomatico();
    
    console.log('✅ Modal de agregar producto abierto');
}

/**
 * Abrir modal de editar producto
 */
async function openEditModal(id) {
    console.log(`✏️ Abriendo modal de editar producto: ${id}`);
    
    try {
        const response = await fetch(`/Productos/GetProducto?id=${id}`);
        
        if (!response.ok) {
            throw new Error('Producto no encontrado');
        }
        
        const producto = await response.json();
        
        // Llenar formulario
        document.getElementById('editId').value = producto.id;
        document.getElementById('editCodigo').value = producto.codigo;
        document.getElementById('editNombre').value = producto.nombre;
        document.getElementById('editCategoria').value = producto.categoria;
        document.getElementById('editUnidadMedida').value = producto.unidadMedida;
        document.getElementById('editPrecioCompra').value = producto.precioCompra;
        document.getElementById('editPrecioVenta').value = producto.precioVenta;
        document.getElementById('editDescripcion').value = producto.descripcion || '';
        
        // Configurar acción del formulario
        document.getElementById('editForm').action = `/Productos/Edit/${id}`;
        
        // Mostrar modal
        const modal = document.getElementById('editModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de editar producto abierto');
        
    } catch (error) {
        console.error('❌ Error al cargar producto:', error);
        showNotification('Error al cargar la información del producto', 'error');
    }
}

/**
 * Abrir modal de desactivar producto
 */
function openDeactivateModal(id, codigo, nombre, categoria, precioVenta) {
    console.log(`🚫 Abriendo modal de desactivar producto: ${id}`);
    
    document.getElementById('deactivateId').value = id;
    document.getElementById('deactivateCodigo').textContent = codigo;
    document.getElementById('deactivateNombre').textContent = nombre;
    document.getElementById('deactivateCategoria').textContent = categoria;
    document.getElementById('deactivatePrecio').textContent = `S/ ${precioVenta}`;
    
    document.getElementById('deactivateForm').action = `/Productos/Deactivate/${id}`;
    
    const modal = document.getElementById('deactivateModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Abrir modal de carga masiva
 */
function openBulkUploadModal() {
    console.log('📤 Abriendo modal de carga masiva...');
    
    const modal = document.getElementById('bulkUploadModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    // Resetear formulario y UI
    document.getElementById('bulkUploadForm').reset();
    document.getElementById('fileUploadArea').style.display = 'block';
    document.getElementById('validateButton').style.display = 'none';
    
    console.log('✅ Modal de carga masiva abierto');
}

/**
 * Cerrar modal
 */
function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    modal.classList.remove('active');
    
    setTimeout(() => {
        modal.style.display = 'none';
    }, 200);
}

/**
 * Configurar event listeners para los modales
 */
function setupModalEventListeners() {
    document.querySelectorAll('.modal-overlay').forEach(overlay => {
        overlay.addEventListener('click', function(e) {
            if (e.target === this) {
                this.classList.remove('active');
                setTimeout(() => {
                    this.style.display = 'none';
                }, 200);
            }
        });
    });
    
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            document.querySelectorAll('.modal-overlay.active').forEach(modal => {
                modal.classList.remove('active');
                setTimeout(() => {
                    modal.style.display = 'none';
                }, 200);
            });
        }
    });
}

// ==========================================
// FORMULARIOS
// ==========================================

/**
 * Configurar submit de formularios
 */
function setupFormSubmits() {
    // Formulario de crear
    const createForm = document.getElementById('createForm');
    if (createForm) {
        // ⭐ Remover listener anterior
        createForm.onsubmit = null;
        
        // ⭐ Asignar nuevo listener
        createForm.onsubmit = async function(e) {
            e.preventDefault();
            e.stopPropagation();
            await submitCreateForm(this);
            return false;
        };
    }
    
    // Formulario de editar
    const editForm = document.getElementById('editForm');
    if (editForm) {
        editForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            await submitEditForm(this);
        });
    }
    
    // Formulario de desactivar
    const deactivateForm = document.getElementById('deactivateForm');
    if (deactivateForm) {
        deactivateForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            await submitDeactivateForm(this);
        });
    }
}

/**
 * Enviar formulario de crear producto
 */
async function submitCreateForm(form) {
    const formData = new FormData(form);
    const submitButton = form.querySelector('button[type="submit"]');
    
    submitButton.disabled = true;
    submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Creando...';
    
    try {
        const response = await fetch('/Productos/CreateAjax', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('createModal');
            showNotification('Producto creado exitosamente', 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarProductos();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al crear producto:', error);
        showNotification('Error al crear el producto', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-save"></i> Crear Producto';
    }
}

/**
 * Enviar formulario de editar producto
 */
async function submitEditForm(form) {
    const formData = new FormData(form);
    const submitButton = form.querySelector('button[type="submit"]');
    
    submitButton.disabled = true;
    submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Guardando...';
    
    try {
        const response = await fetch(form.action, {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('editModal');
            showNotification('Producto actualizado exitosamente', 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarProductos();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al editar producto:', error);
        showNotification('Error al actualizar el producto', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-save"></i> Guardar Cambios';
    }
}

/**
 * Enviar formulario de desactivar producto
 */
async function submitDeactivateForm(form) {
    const formData = new FormData(form);
    const submitButton = form.querySelector('button[type="submit"]');
    
    submitButton.disabled = true;
    submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Desactivando...';
    
    try {
        const response = await fetch(form.action, {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('deactivateModal');
            showNotification('Producto desactivado exitosamente', 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarProductos();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al desactivar producto:', error);
        showNotification('Error al desactivar el producto', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-ban"></i> Sí, Desactivar Producto';
    }
}

// ==========================================
// CARGA MASIVA
// ==========================================

/**
 * Manejar selección de archivo para carga masiva
 */
function handleFileSelect(event) {
    const file = event.target.files[0];
    if (!file) return;
    
    console.log('📁 Archivo seleccionado:', file.name);
    
    // Validar extensión
    const validExtensions = ['.csv', '.xlsx', '.xls'];
    const fileExtension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    
    if (!validExtensions.includes(fileExtension)) {
        showNotification('Formato de archivo no válido. Use CSV o Excel.', 'error');
        event.target.value = '';
        return;
    }
    
    // Mostrar nombre del archivo
    document.getElementById('fileUploadArea').innerHTML = `
        <i class="fa-solid fa-file-excel" style="font-size: 2rem; color: #10B981; margin-bottom: 0.5rem;"></i>
        <p style="font-weight: 500;">${file.name}</p>
        <small style="color: #64748B;">Archivo listo para validar</small>
    `;
    
    // Mostrar botón de validar
    document.getElementById('validateButton').style.display = 'block';
}

/**
 * Validar archivo antes de importar
 */
async function validarArchivo() {
    const fileInput = document.getElementById('bulkUploadFile');
    if (!fileInput.files[0]) {
        showNotification('Por favor seleccione un archivo', 'error');
        return;
    }
    
    const formData = new FormData();
    formData.append('file', fileInput.files[0]);
    
    const validateButton = document.getElementById('validateButton');
    validateButton.disabled = true;
    validateButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Validando...';
    
    try {
        const response = await fetch('/Productos/ValidarCargaMasiva', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Mostrar resumen de validación
            showNotification(`Validación exitosa: ${result.registrosValidos} registros válidos`, 'success');
            
            // Cambiar botón a "Importar"
            validateButton.innerHTML = '<i class="fa-solid fa-upload"></i> Importar';
            validateButton.onclick = importarArchivo;
        } else {
            showNotification('Error en validación: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al validar archivo:', error);
        showNotification('Error al validar el archivo', 'error');
    } finally {
        validateButton.disabled = false;
    }
}

/**
 * Importar archivo validado
 */
async function importarArchivo() {
    const fileInput = document.getElementById('bulkUploadFile');
    if (!fileInput.files[0]) {
        showNotification('Por favor seleccione un archivo', 'error');
        return;
    }
    
    const formData = new FormData();
    formData.append('file', fileInput.files[0]);
    
    const importButton = document.getElementById('validateButton');
    importButton.disabled = true;
    importButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Importando...';
    
    try {
        const response = await fetch('/Productos/BulkUpload', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('bulkUploadModal');
            showNotification(`${result.cantidadProcesada} productos importados exitosamente`, 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarProductos();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al importar archivo:', error);
        showNotification('Error al procesar el archivo', 'error');
    } finally {
        importButton.disabled = false;
        importButton.innerHTML = '<i class="fa-solid fa-upload"></i> Importar';
    }
}

/**
 * Descargar plantilla Excel
 */
function descargarPlantilla() {
    console.log('📥 Descargando plantilla...');
    
    // Crear plantilla CSV con las columnas correctas
    const headers = 'Codigo,Nombre,Categoria,UnidadMedida,PrecioCompra,PrecioVenta,Descripcion\n';
    const ejemplo = 'PROD-001,Cerveza Pilsen,Cervezas,Unidad,3.50,8.00,Cerveza Pilsen en botella de 620ml\n';
    const csvContent = headers + ejemplo;
    
    // Descargar
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'plantilla_productos.csv';
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
    
    showNotification('Plantilla descargada', 'success');
}

// ==========================================
// EXPORTAR DATOS
// ==========================================

/**
 * Exportar datos a CSV
 */
async function exportarDatos() {
    console.log('📤 Exportando datos...');
    
    try {
        const response = await fetch('/Productos/ExportarCSV');
        
        if (!response.ok) {
            throw new Error('Error al exportar datos');
        }
        
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `productos_${new Date().toISOString().split('T')[0]}.csv`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        
        showNotification('Datos exportados exitosamente', 'success');
        
    } catch (error) {
        console.error('❌ Error al exportar:', error);
        showNotification('Error al exportar los datos', 'error');
    }
}

// ==========================================
// UTILIDADES
// ==========================================

/**
 * Generar código automático para nuevo producto
 */
function generarCodigoAutomatico() {
    const codigoInput = document.getElementById('createCodigo');
    if (!codigoInput) return;
    
    // Generar código con formato PROD-XXX
    const numero = String(Math.floor(Math.random() * 1000)).padStart(3, '0');
    codigoInput.value = `PROD-${numero}`;
}

/**
 * Mostrar notificación
 */
function showNotification(message, type = 'info') {
    console.log(`[${type.toUpperCase()}] ${message}`);
    
    // Aquí puedes implementar un sistema de notificaciones toast
    // Por ahora usamos alert para errores
    if (type === 'error') {
        alert(message);
    } else {
        console.log(`✅ ${message}`);
    }
}

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

window.openCreateModal = openCreateModal;
window.openEditModal = openEditModal;
window.openDeactivateModal = openDeactivateModal;
window.openBulkUploadModal = openBulkUploadModal;
window.closeModal = closeModal;
window.handleFileSelect = handleFileSelect;
window.validarArchivo = validarArchivo;
window.importarArchivo = importarArchivo;
window.descargarPlantilla = descargarPlantilla;
window.exportarDatos = exportarDatos;