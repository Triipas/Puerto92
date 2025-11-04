/**
 * Gestión de Catálogo de Utensilios - Panel Contador
 * Puerto 92 - Sistema de Gestión
 */

// Variables globales
let utensilios = [];
let categoriaActual = 'todos'; // 'todos', 'cocina', 'mozos', 'vajilla'

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

/**
 * Función de inicialización que se ejecuta cada vez que se carga la página
 */
function initCatalogoUtensilios() {
    console.log('🔄 Inicializando catálogo de utensilios...');
    
    // Limpiar datos anteriores
    utensilios = [];
    categoriaActual = 'todos';
    
    // Cargar datos y configurar listeners
    cargarEstadisticas();
    cargarUtensilios();
    setupSearch();
    setupModalEventListeners();
    setupCategoryTabs();
    setupFormSubmits();
    
    console.log('✅ Catálogo de utensilios inicializado correctamente');
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initCatalogoUtensilios);

// ⭐ Exponer función para reinicializar después de navegación SPA
window.initCatalogoUtensilios = initCatalogoUtensilios;

// ==========================================
// CARGA DE ESTADÍSTICAS
// ==========================================

/**
 * Cargar estadísticas del dashboard
 */
async function cargarEstadisticas() {
    console.log('📊 Cargando estadísticas...');
    
    try {
        const response = await fetch('/Utensilios/GetEstadisticas');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        const data = await response.json();
        
        // Actualizar las tarjetas de estadísticas
        actualizarEstadistica('totalActivos', data.totalActivos || 0);
        actualizarEstadistica('utensiliosCocina', data.utensiliosCocina || 0);
        actualizarEstadistica('utensiliosMozos', data.utensiliosMozos || 0);
        actualizarEstadistica('vajilla', data.vajilla || 0);
        
        console.log('✅ Estadísticas cargadas:', data);
        
    } catch (error) {
        console.error('❌ Error al cargar estadísticas:', error);
        // Mostrar 0 en caso de error
        actualizarEstadistica('totalActivos', 0);
        actualizarEstadistica('utensiliosCocina', 0);
        actualizarEstadistica('utensiliosMozos', 0);
        actualizarEstadistica('vajilla', 0);
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
// CARGA DE UTENSILIOS (TABLA)
// ==========================================

/**
 * Cargar lista de utensilios desde el servidor
 */
async function cargarUtensilios() {
    console.log('📥 Cargando utensilios...');
    
    try {
        const response = await fetch('/Utensilios/GetUtensilios');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        utensilios = await response.json();
        
        console.log(`✅ Cargados ${utensilios.length} utensilios`);
        
        // Renderizar tabla
        renderizarTabla();
        
    } catch (error) {
        console.error('❌ Error al cargar utensilios:', error);
        utensilios = [];
        renderizarTabla();
    }
}

/**
 * Renderizar tabla de utensilios
 */
function renderizarTabla() {
    const tbody = document.querySelector('#utensiliosTable tbody');
    if (!tbody) {
        console.warn('⚠️ Tabla de utensilios no encontrada');
        return;
    }
    
    // Limpiar tabla
    tbody.innerHTML = '';
    
    // Filtrar utensilios según categoría activa
    const utensiliosFiltrados = filtrarPorCategoria(utensilios);
    
    if (utensiliosFiltrados.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 2rem; color: #64748B;">
                    <i class="fa-solid fa-inbox" style="font-size: 2rem; margin-bottom: 0.5rem; display: block;"></i>
                    No hay utensilios registrados
                </td>
            </tr>
        `;
        return;
    }
    
    // Renderizar cada utensilio
    utensiliosFiltrados.forEach(utensilio => {
        const row = document.createElement('tr');
        
        row.innerHTML = `
            <td>${utensilio.codigo}</td>
            <td>
                <div style="font-weight: 500;">${utensilio.nombre}</div>
                <small style="color: #64748B;">${utensilio.descripcion || ''}</small>
            </td>
            <td>${utensilio.unidad}</td>
            <td>S/ ${parseFloat(utensilio.precio).toFixed(2)}</td>
            <td>
                <span class="badge badge-${utensilio.activo ? 'success' : 'danger'}">
                    ${utensilio.activo ? 'Activo' : 'Inactivo'}
                </span>
            </td>
            <td>
                <button onclick="openEditModal('${utensilio.id}')" class="btn-icon" title="Editar">
                    <i class="fa-solid fa-pen"></i>
                </button>
                <button onclick="openDeactivateModal('${utensilio.id}', '${utensilio.nombre}', '${utensilio.codigo}', '${utensilio.tipo}')" 
                        class="btn-icon btn-danger" title="Desactivar">
                    <i class="fa-solid fa-ban"></i>
                </button>
            </td>
        `;
        
        tbody.appendChild(row);
    });
    
    console.log(`✅ Tabla renderizada con ${utensiliosFiltrados.length} utensilios`);
}

/**
 * Filtrar utensilios según categoría activa
 */
function filtrarPorCategoria(utensilios) {
    if (categoriaActual === 'todos') {
        return utensilios;
    }
    
    return utensilios.filter(u => {
        const tipo = u.tipo.toLowerCase();
        return tipo === categoriaActual;
    });
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
        const rows = document.querySelectorAll('#utensiliosTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const isVisible = text.includes(searchValue);
            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount++;
        });
        
        console.log(`✅ Búsqueda: "${searchValue}" - Mostrando ${visibleCount} de ${rows.length} utensilios`);
    });
    
    console.log('✅ Búsqueda configurada correctamente');
}

/**
 * Configurar tabs de categorías
 */
function setupCategoryTabs() {
    const tabs = document.querySelectorAll('[data-category]');
    
    tabs.forEach(tab => {
        tab.addEventListener('click', function() {
            // Remover clase activa de todos los tabs
            tabs.forEach(t => t.classList.remove('active'));
            
            // Activar tab actual
            this.classList.add('active');
            
            // Cambiar categoría actual
            categoriaActual = this.getAttribute('data-category');
            
            console.log(`📂 Categoría cambiada a: ${categoriaActual}`);
            
            // Re-renderizar tabla
            renderizarTabla();
        });
    });
}

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de agregar utensilio
 */
function openCreateModal() {
    console.log('📝 Abriendo modal de agregar utensilio...');
    
    const modal = document.getElementById('createUtensilioModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    // Resetear formulario
    document.getElementById('createForm').reset();
    
    // Generar código automático (opcional)
    generarCodigoAutomatico();
    
    console.log('✅ Modal de agregar utensilio abierto');
}

/**
 * Abrir modal de editar utensilio
 */
async function openEditModal(id) {
    console.log(`✏️ Abriendo modal de editar utensilio: ${id}`);
    
    try {
        const response = await fetch(`/Utensilios/GetUtensilio?id=${id}`);
        
        if (!response.ok) {
            throw new Error('Utensilio no encontrado');
        }
        
        const utensilio = await response.json();
        
        // Llenar formulario
        document.getElementById('editId').value = utensilio.id;
        document.getElementById('editNombre').value = utensilio.nombre;
        document.getElementById('editTipo').value = utensilio.tipo;
        document.getElementById('editUnidad').value = utensilio.unidad;
        document.getElementById('editPrecio').value = utensilio.precio;
        document.getElementById('editDescripcion').value = utensilio.descripcion || '';
        
        // Configurar acción del formulario
        document.getElementById('editForm').action = `/Utensilios/Edit/${id}`;
        
        // Mostrar modal
        const modal = document.getElementById('editModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de editar utensilio abierto');
        
    } catch (error) {
        console.error('❌ Error al cargar utensilio:', error);
        showNotification('Error al cargar la información del utensilio', 'error');
    }
}

/**
 * Abrir modal de desactivar utensilio
 */
function openDeactivateModal(id, nombre, codigo, tipo) {
    console.log(`🚫 Abriendo modal de desactivar utensilio: ${id}`);
    
    document.getElementById('deactivateId').value = id;
    document.getElementById('deactivateNombre').textContent = nombre;
    document.getElementById('deactivateCodigo').textContent = codigo;
    document.getElementById('deactivateTipo').textContent = tipo;
    
    document.getElementById('deactivateForm').action = `/Utensilios/Deactivate/${id}`;
    
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
    
    // Resetear formulario
    document.getElementById('bulkUploadForm').reset();
    
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
        createForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            await submitCreateForm(this);
        });
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
    
    // Formulario de carga masiva
    const bulkUploadForm = document.getElementById('bulkUploadForm');
    if (bulkUploadForm) {
        bulkUploadForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            await submitBulkUploadForm(this);
        });
    }
}

/**
 * Enviar formulario de crear utensilio
 */
async function submitCreateForm(form) {
    const formData = new FormData(form);
    const submitButton = form.querySelector('button[type="submit"]');
    
    submitButton.disabled = true;
    submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Creando...';
    
    try {
        const response = await fetch('/Utensilios/CreateAjax', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('createModal');
            showNotification('Utensilio creado exitosamente', 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarUtensilios();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al crear utensilio:', error);
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-plus"></i> Crear Utensilio';
    }
}

/**
 * Enviar formulario de editar utensilio
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
            showNotification('Utensilio actualizado exitosamente', 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarUtensilios();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al editar utensilio:', error);
        showNotification('Error al actualizar el utensilio', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-save"></i> Guardar Cambios';
    }
}

/**
 * Enviar formulario de desactivar utensilio
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
            showNotification('Utensilio desactivado exitosamente', 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarUtensilios();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al desactivar utensilio:', error);
        showNotification('Error al desactivar el utensilio', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-ban"></i> Desactivar Utensilio';
    }
}

/**
 * Enviar formulario de carga masiva
 */
async function submitBulkUploadForm(form) {
    const formData = new FormData(form);
    const submitButton = form.querySelector('button[type="submit"]');
    
    submitButton.disabled = true;
    submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Procesando...';
    
    try {
        const response = await fetch('/Utensilios/BulkUpload', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('bulkUploadModal');
            showNotification(`${result.cantidadProcesada} utensilios importados exitosamente`, 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarUtensilios();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al cargar archivo:', error);
        showNotification('Error al procesar el archivo', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-upload"></i> Subir Archivo';
    }
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
        const response = await fetch('/Utensilios/ExportarCSV');
        
        if (!response.ok) {
            throw new Error('Error al exportar datos');
        }
        
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `utensilios_${new Date().toISOString().split('T')[0]}.csv`;
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

/**
 * Descargar plantilla CSV
 */
function descargarPlantilla() {
    console.log('📥 Descargando plantilla CSV...');
    
    // Crear plantilla CSV
    const headers = 'Codigo,Nombre,Tipo,Unidad,Precio,Descripcion\n';
    const ejemplo = 'UTEN-001,Sartén Antiadherente 28cm,Cocina,Unidad,85.50,Sartén antiadherente profesional de 28cm de diámetro\n';
    const csvContent = headers + ejemplo;
    
    // Descargar
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'plantilla_utensilios.csv';
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
    
    showNotification('Plantilla descargada', 'success');
}

// ==========================================
// UTILIDADES
// ==========================================

/**
 * Generar código automático para nuevo utensilio
 */
function generarCodigoAutomatico() {
    const tipoSelect = document.getElementById('createTipo');
    const codigoInput = document.getElementById('createCodigo');
    
    if (!tipoSelect || !codigoInput) return;
    
    tipoSelect.addEventListener('change', function() {
        const tipo = this.value;
        const prefijo = tipo === 'Cocina' ? 'UTEN' : tipo === 'Mozos' ? 'MOZO' : 'VAJI';
        const numero = String(Math.floor(Math.random() * 1000)).padStart(3, '0');
        codigoInput.value = `${prefijo}-${numero}`;
    });
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
        // Puedes usar TempData o un sistema de toast personalizado
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
window.exportarDatos = exportarDatos;
window.descargarPlantilla = descargarPlantilla;