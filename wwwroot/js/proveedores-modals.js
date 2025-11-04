/**
 * Gestión de Proveedores - Supervisora de Calidad
 * Puerto 92 - Sistema de Gestión
 */

// Variables globales
let proveedores = [];
let categorias = [];

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

/**
 * Función de inicialización que se ejecuta cada vez que se carga la página
 */
function initGestionProveedores() {
    console.log('🔄 Inicializando gestión de proveedores...');
    
    // Limpiar datos anteriores
    proveedores = [];
    categorias = [];
    
    // Cargar datos y configurar listeners
    cargarEstadisticas();
    cargarProveedores();
    cargarCategorias();
    setupSearch();
    setupModalEventListeners();
    setupFormSubmits();
    setupCategoryFilter();
    setupRucValidation();
    
    console.log('✅ Gestión de proveedores inicializada correctamente');
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initGestionProveedores);

// ⭐ Exponer función para reinicializar después de navegación SPA
window.initGestionProveedores = initGestionProveedores;

// ==========================================
// CARGA DE ESTADÍSTICAS
// ==========================================

/**
 * Cargar estadísticas del dashboard
 */
async function cargarEstadisticas() {
    console.log('📊 Cargando estadísticas...');
    
    try {
        const response = await fetch('/Proveedores/GetEstadisticas');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        const data = await response.json();
        
        // Actualizar las tarjetas de estadísticas
        actualizarEstadistica('totalProveedores', data.totalProveedores || 0);
        actualizarEstadistica('proveedoresActivos', data.proveedoresActivos || 0);
        actualizarEstadistica('proveedoresInactivos', data.proveedoresInactivos || 0);
        
        console.log('✅ Estadísticas cargadas:', data);
        
    } catch (error) {
        console.error('❌ Error al cargar estadísticas:', error);
        // Mostrar 0 en caso de error
        actualizarEstadistica('totalProveedores', 0);
        actualizarEstadistica('proveedoresActivos', 0);
        actualizarEstadistica('proveedoresInactivos', 0);
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
// CARGA DE PROVEEDORES (TABLA)
// ==========================================

/**
 * Cargar lista de proveedores desde el servidor
 */
async function cargarProveedores() {
    console.log('📥 Cargando proveedores...');
    
    try {
        const response = await fetch('/Proveedores/GetProveedores');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        proveedores = await response.json();
        
        console.log(`✅ Cargados ${proveedores.length} proveedores`);
        
        // Renderizar tabla
        renderizarTabla();
        
    } catch (error) {
        console.error('❌ Error al cargar proveedores:', error);
        proveedores = [];
        renderizarTabla();
    }
}

/**
 * Cargar categorías disponibles
 */
async function cargarCategorias() {
    console.log('📥 Cargando categorías de proveedores...');
    
    try {
        const response = await fetch('/Proveedores/GetCategorias');
        
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
 * Renderizar tabla de proveedores
 */
function renderizarTabla() {
    const tbody = document.querySelector('#proveedoresTable tbody');
    if (!tbody) {
        console.warn('⚠️ Tabla de proveedores no encontrada');
        return;
    }
    
    // Limpiar tabla
    tbody.innerHTML = '';
    
    if (proveedores.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 2rem; color: #64748B;">
                    <i class="fa-solid fa-inbox" style="font-size: 2rem; margin-bottom: 0.5rem; display: block;"></i>
                    No hay proveedores registrados
                </td>
            </tr>
        `;
        return;
    }
    
    // Renderizar cada proveedor
    proveedores.forEach(proveedor => {
        const row = document.createElement('tr');
        
        row.innerHTML = `
            <td><strong>${proveedor.ruc}</strong></td>
            <td>
                <div style="font-weight: 500;">${proveedor.nombre}</div>
                <small style="color: #64748B;">Contacto: ${proveedor.personaContacto || 'N/A'}</small>
            </td>
            <td>
                <span class="badge" style="background: #E0F2FE; color: #0369A1;">${proveedor.categoria}</span>
            </td>
            <td>${proveedor.telefono || 'N/A'}</td>
            <td>
                <span class="badge badge-${proveedor.activo ? 'success' : 'danger'}">
                    ${proveedor.activo ? 'Activo' : 'Inactivo'}
                </span>
            </td>
            <td>
                <button onclick="openEditModal('${proveedor.id}')" class="btn-icon" title="Editar">
                    <i class="fa-solid fa-pen"></i>
                </button>
                <button onclick="openDeactivateModal('${proveedor.id}', '${proveedor.ruc}', '${proveedor.nombre}', '${proveedor.categoria}')" 
                        class="btn-icon btn-danger" title="Desactivar">
                    <i class="fa-solid fa-ban"></i>
                </button>
            </td>
        `;
        
        tbody.appendChild(row);
    });
    
    console.log(`✅ Tabla renderizada con ${proveedores.length} proveedores`);
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
        const rows = document.querySelectorAll('#proveedoresTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const isVisible = text.includes(searchValue);
            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount++;
        });
        
        console.log(`✅ Búsqueda: "${searchValue}" - Mostrando ${visibleCount} de ${rows.length} proveedores`);
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
        const rows = document.querySelectorAll('#proveedoresTable tbody tr');
        
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
        
        console.log(`✅ Filtro: "${selectedCategory}" - Mostrando ${visibleCount} proveedores`);
    });
}

// ==========================================
// VALIDACIÓN DE RUC
// ==========================================

/**
 * Configurar validación de RUC en tiempo real
 */
function setupRucValidation() {
    const rucInputs = ['createRuc', 'editRuc'];
    
    rucInputs.forEach(inputId => {
        const input = document.getElementById(inputId);
        if (!input) return;
        
        input.addEventListener('input', function() {
            validarRucInput(this);
        });
        
        input.addEventListener('blur', function() {
            validarRucFinal(this);
        });
    });
}

/**
 * Validar RUC mientras el usuario escribe
 */
function validarRucInput(input) {
    // Solo permitir números
    input.value = input.value.replace(/[^0-9]/g, '');
    
    // Limitar a 11 dígitos
    if (input.value.length > 11) {
        input.value = input.value.substring(0, 11);
    }
    
    // Feedback visual
    const feedbackElement = input.nextElementSibling;
    if (input.value.length === 0) {
        input.style.borderColor = '';
        if (feedbackElement) feedbackElement.textContent = '';
    } else if (input.value.length === 11) {
        input.style.borderColor = '#10B981';
        if (feedbackElement) {
            feedbackElement.textContent = '✓ RUC válido';
            feedbackElement.style.color = '#10B981';
        }
    } else {
        input.style.borderColor = '#F59E0B';
        if (feedbackElement) {
            feedbackElement.textContent = `Faltan ${11 - input.value.length} dígitos`;
            feedbackElement.style.color = '#F59E0B';
        }
    }
}

/**
 * Validar RUC al perder el foco
 */
function validarRucFinal(input) {
    const feedbackElement = input.nextElementSibling;
    
    if (input.value.length > 0 && input.value.length !== 11) {
        input.style.borderColor = '#EF4444';
        if (feedbackElement) {
            feedbackElement.textContent = '✗ El RUC debe tener exactamente 11 dígitos';
            feedbackElement.style.color = '#EF4444';
        }
    }
}

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de agregar proveedor
 */
async function openCreateProveedorModal() {
    console.log('📝 Abriendo modal de agregar proveedor...');
    
    // Asegurar que las categorías estén cargadas
    if (categorias.length === 0) {
        await cargarCategorias();
    }
    
     const modal = document.getElementById('createProveedorModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    // Resetear formulario
    document.getElementById('createProveedorForm').reset();
    
    // Limpiar estilos de validación
    const rucInput = document.getElementById('createProveedorRuc');
    if (rucInput) {
        rucInput.style.borderColor = '';
        const feedback = rucInput.nextElementSibling;
        if (feedback) feedback.textContent = '';
    }
    
    console.log('✅ Modal de agregar proveedor abierto');
}
window.openCreateProveedorModal = openCreateProveedorModal;
/**
 * Abrir modal de editar proveedor
 */
async function openEditProveedorModal(id) {
    console.log(`✏️ Abriendo modal de editar proveedor: ${id}`);
    
    try {
        const response = await fetch(`/Proveedores/GetProveedor?id=${id}`);
        
        if (!response.ok) {
            throw new Error('Proveedor no encontrado');
        }
        
        const proveedor = await response.json();
        
        // Llenar formulario
        document.getElementById('editId').value = proveedor.id;
        document.getElementById('editRuc').value = proveedor.ruc;
        document.getElementById('editNombre').value = proveedor.nombre;
        document.getElementById('editCategoria').value = proveedor.categoria;
        document.getElementById('editTelefono').value = proveedor.telefono || '';
        document.getElementById('editEmail').value = proveedor.email || '';
        document.getElementById('editPersonaContacto').value = proveedor.personaContacto || '';
        document.getElementById('editDireccion').value = proveedor.direccion || '';
        
        // Configurar acción del formulario
        document.getElementById('editProveedorForm').action = `/Proveedores/Edit/${id}`;
        
        // Mostrar modal
        const modal = document.getElementById('editProveedorModal'); 
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de editar proveedor abierto');
        
    } catch (error) {
        console.error('❌ Error al cargar proveedor:', error);
        showNotification('Error al cargar la información del proveedor', 'error');
    }
}

/**
 * Abrir modal de desactivar proveedor
 */
function openDeactivateProveedorModal(id, ruc, nombre, categoria) {
    console.log(`🚫 Abriendo modal de desactivar proveedor: ${id}`);
    
    document.getElementById('deactivateId').value = id;
    document.getElementById('deactivateRuc').textContent = ruc;
    document.getElementById('deactivateNombre').textContent = nombre;
    document.getElementById('deactivateCategoria').textContent = categoria;
    
    document.getElementById('deactivateProveedorId').value = id;
    
    const modal = document.getElementById('deactivateProveedorModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
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
    const createForm = document.getElementById('createProveedorForm');
    if (createForm) {
        createForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            // Validar RUC antes de enviar
            const rucInput = document.getElementById('createProveedorRuc');

            if (rucInput.value.length !== 11) {
                showNotification('El RUC debe tener exactamente 11 dígitos', 'error');
                rucInput.focus();
                return;
            }
            
            await submitCreateForm(this);
        });
    }
    
    // Formulario de editar
    const editForm = document.getElementById('editProveedorForm');
    if (editForm) {
        editForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            // Validar RUC antes de enviar
            const rucInput = document.getElementById('editProveedorRuc');
            if (rucInput.value.length !== 11) {
                showNotification('El RUC debe tener exactamente 11 dígitos', 'error');
                rucInput.focus();
                return;
            }
            
            await submitEditForm(this);
        });
    }
    
    // Formulario de desactivar
    const deactivateForm = document.getElementById('deactivateProveedorForm');
    if (deactivateForm) {
        deactivateForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            await submitDeactivateForm(this);
        });
    }
}

/**
 * Enviar formulario de crear proveedor
 */
async function submitCreateForm(form) {
    const formData = new FormData(form);
    const submitButton = form.querySelector('button[type="submit"]');
    
    submitButton.disabled = true;
    submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Creando...';
    
    try {
        const response = await fetch('/Proveedores/CreateAjax', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('createProveedorModal');
            showNotification('Proveedor creado exitosamente', 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarProveedores();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al crear proveedor:', error);
        showNotification('Error al crear el proveedor', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-save"></i> Crear Proveedor';
    }
}

/**
 * Enviar formulario de editar proveedor
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
            closeModal('editProveedorModal');
            showNotification('Proveedor actualizado exitosamente', 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarProveedores();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al editar proveedor:', error);
        showNotification('Error al actualizar el proveedor', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-save"></i> Guardar Cambios';
    }
}

/**
 * Enviar formulario de desactivar proveedor
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
            closeModal('deactivateProveedorModal');
            showNotification('Proveedor desactivado exitosamente', 'success');
            
            // Recargar datos
            await cargarEstadisticas();
            await cargarProveedores();
        } else {
            showNotification('Error: ' + result.message, 'error');
        }
        
    } catch (error) {
        console.error('Error al desactivar proveedor:', error);
        showNotification('Error al desactivar el proveedor', 'error');
    } finally {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="fa-solid fa-times-circle"></i> Desactivar Proveedor';
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
        const response = await fetch('/Proveedores/ExportarCSV');
        
        if (!response.ok) {
            throw new Error('Error al exportar datos');
        }
        
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `proveedores_${new Date().toISOString().split('T')[0]}.csv`;
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


window.openEditProveedorModal = openEditProveedorModal;        // CORREGIDO
window.openDeactivateProveedorModal = openDeactivateProveedorModal; // CORREGIDO
window.closeModal = closeModal;
window.exportarDatos = exportarDatos;