/**
 * Gestión de Categorías - Puerto 92
 * Drag & Drop, Modales y Funcionalidad
 */

let sortableInstance = null;

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

function initCategoriasPage() {
    console.log('🔄 Inicializando página de categorías...');
    
    setupSearch();
    setupModalEventListeners();
    setupDragAndDrop();
    actualizarTipoEnModal();
    
    console.log('✅ Página de categorías inicializada correctamente');
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initCategoriasPage);

// Exponer función para reinicializar después de navegación SPA
window.initCategoriasPage = initCategoriasPage;

// ==========================================
// DRAG & DROP CON SORTABLEJS
// ==========================================

function setupDragAndDrop() {
    const tableBody = document.getElementById('sortableCategoriasTable');
    
    if (!tableBody) {
        console.log('⚠️ No se encontró tabla para drag & drop');
        return;
    }

    // Destruir instancia anterior si existe
    if (sortableInstance) {
        sortableInstance.destroy();
    }

    console.log('🎯 Configurando drag & drop...');

    sortableInstance = new Sortable(tableBody, {
        animation: 150,
        handle: '.drag-handle',
        ghostClass: 'sortable-ghost',
        chosenClass: 'sortable-chosen',
        dragClass: 'dragging',
        
        onEnd: function(evt) {
            console.log(`📦 Elemento arrastrado de ${evt.oldIndex + 1} a ${evt.newIndex + 1}`);
            actualizarOrdenDespuesDeArrastrar();
        }
    });

    console.log('✅ Drag & drop configurado');
}

/**
 * Actualizar orden después de arrastrar y guardar en servidor
 */
async function actualizarOrdenDespuesDeArrastrar() {
    const filas = document.querySelectorAll('#sortableCategoriasTable tr');
    const ordenes = [];

    // Recopilar nuevo orden
    filas.forEach((fila, index) => {
        const categoriaId = parseInt(fila.getAttribute('data-categoria-id'));
        const nuevoOrden = index + 1;
        
        // Actualizar visualmente el número de orden
        const ordenNumber = fila.querySelector('.orden-number');
        if (ordenNumber) {
            ordenNumber.textContent = nuevoOrden;
        }

        ordenes.push({
            Id: categoriaId,
            Orden: nuevoOrden
        });
    });

    console.log('📤 Enviando nuevo orden al servidor:', ordenes);

    try {
        const response = await fetch('/Categorias/UpdateOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify(ordenes)
        });

        if (response.ok) {
            const result = await response.json();
            console.log('✅ Orden actualizado:', result);
            
            // Mostrar notificación de éxito
            showNotification('Orden actualizado exitosamente', 'success');
        } else {
            throw new Error('Error al actualizar orden');
        }
    } catch (error) {
        console.error('❌ Error al actualizar orden:', error);
        showNotification('Error al actualizar el orden. Recargando página...', 'error');
        
        // Recargar página después de 2 segundos
        setTimeout(() => {
            window.location.reload();
        }, 2000);
    }
}

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de crear categoría
 */
function openCreateCategoriaModal() {
    console.log('📝 Abriendo modal de crear categoría...');
    
    const modal = document.getElementById('createCategoriaModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    // Resetear formulario
    document.getElementById('createCategoriaForm').reset();
    
    // Actualizar tipo según tab activa
    actualizarTipoEnModal();
    
    // Calcular siguiente orden disponible
    const filas = document.querySelectorAll('#sortableCategoriasTable tr');
    const siguienteOrden = filas.length + 1;
    document.getElementById('createOrden').value = siguienteOrden;
    
    console.log('✅ Modal de crear categoría abierto');
}

/**
 * Abrir modal de editar categoría
 */
async function openEditCategoriaModal(id) {
    console.log(`✏️ Abriendo modal de editar categoría: ${id}`);
    
    try {
        const response = await fetch(`/Categorias/GetCategoria?id=${id}`);
        
        if (!response.ok) {
            throw new Error('Categoría no encontrada');
        }

        const categoria = await response.json();
        console.log('📥 Datos de categoría:', categoria);

        // Llenar formulario
        document.getElementById('editCategoriaId').value = categoria.id;
        document.getElementById('editCategoriaTipo').value = categoria.tipo;
        document.getElementById('editCategoriaNombre').value = categoria.nombre;
        document.getElementById('editCategoriaOrden').value = categoria.orden;
        document.getElementById('editCategoriaActivo').value = categoria.activo.toString().toLowerCase();

        // Actualizar texto e icono del tipo
        document.getElementById('editCategoriaTipoTexto').textContent = categoria.tipo;
        
        const iconos = {
            'Bebidas': 'wine-glass',
            'Cocina': 'utensils',
            'Utensilios': 'kitchen-set'
        };
        const iconoClase = iconos[categoria.tipo] || 'list';
        document.getElementById('editCategoriaTipoIcono').className = `fa-solid fa-${iconoClase}`;

        // Configurar acción del formulario
        document.getElementById('editCategoriaForm').action = `/Categorias/Edit/${id}`;
        
        // Mostrar modal
        const modal = document.getElementById('editCategoriaModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de editar categoría abierto');

    } catch (error) {
        console.error('❌ Error al cargar categoría:', error);
        showNotification('Error al cargar la información de la categoría', 'error');
    }
}

/**
 * Abrir modal de eliminar categoría
 */
function openDeleteCategoriaModal(id, nombre, cantidadProductos, tipo) {
    console.log(`🗑️ Abriendo modal de eliminar categoría: ${id}`);
    
    // Llenar información
    document.getElementById('deleteCategoriaId').value = id;
    document.getElementById('deleteCategoriaTipo').textContent = tipo;
    document.getElementById('deleteCategoriaNombre').textContent = nombre;
    document.getElementById('deleteCategoriaProductos').textContent = cantidadProductos;
    document.getElementById('deleteCantidadProductos').textContent = cantidadProductos;

    // Configurar acción del formulario
    document.getElementById('deleteCategoriaForm').action = `/Categorias/Delete/${id}`;

    // Mostrar/ocultar secciones según si tiene productos
    const tieneProductos = cantidadProductos > 0;
    document.getElementById('deleteErrorProductos').style.display = tieneProductos ? 'block' : 'none';
    document.getElementById('deleteConfirmacion').style.display = tieneProductos ? 'none' : 'block';
    
    // Habilitar/deshabilitar botón de eliminar
    const btnEliminar = document.getElementById('btnConfirmarEliminar');
    btnEliminar.disabled = tieneProductos;
    btnEliminar.style.opacity = tieneProductos ? '0.5' : '1';
    btnEliminar.style.cursor = tieneProductos ? 'not-allowed' : 'pointer';

    // Mostrar modal
    const modal = document.getElementById('deleteCategoriaModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    console.log('✅ Modal de eliminar categoría abierto');
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
 * Actualizar el tipo en el modal de crear según la tab activa
 */
function actualizarTipoEnModal() {
    // Obtener tipo de la tab activa
    const tabActiva = document.querySelector('.categoria-tab.active');
    if (!tabActiva) return;

    const tipo = tabActiva.querySelector('span:not(.tab-badge)')?.textContent.trim();
    
    if (tipo) {
        const tipoInput = document.getElementById('createTipo');
        const tipoLabel = document.getElementById('tipoSeleccionadoLabel');
        
        if (tipoInput) tipoInput.value = tipo;
        if (tipoLabel) tipoLabel.textContent = tipo;

        // Actualizar ejemplos
        if (typeof actualizarEjemplos === 'function') {
            actualizarEjemplos(tipo);
        }
        
        console.log(`✅ Tipo actualizado en modal: ${tipo}`);
    }
}

// ==========================================
// BÚSQUEDA
// ==========================================

function setupSearch() {
    const searchInput = document.getElementById('searchInput');
    if (!searchInput) {
        console.warn('⚠️ Input de búsqueda no encontrado');
        return;
    }

    console.log('🔍 Configurando búsqueda de categorías...');

    // Remover event listeners anteriores
    const newSearchInput = searchInput.cloneNode(true);
    searchInput.parentNode.replaceChild(newSearchInput, searchInput);

    newSearchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#categoriasTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const isVisible = text.includes(searchValue);
            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount++;
        });
        
        console.log(`✅ Búsqueda: "${searchValue}" - Mostrando ${visibleCount} de ${rows.length} categorías`);
    });
    
    console.log('✅ Búsqueda de categorías configurada');
}

// ==========================================
// CONFIGURACIÓN DE EVENT LISTENERS
// ==========================================

function setupModalEventListeners() {
    // Cerrar modal al hacer click fuera
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

    // Cerrar modal con tecla ESC
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
// UTILIDADES
// ==========================================

function showNotification(message, type = 'info') {
    console.log(`[${type.toUpperCase()}] ${message}`);
    
    // Crear notificación
    const notification = document.createElement('div');
    notification.className = `app-notification ${type}`;
    
    const iconos = {
        'success': 'check-circle',
        'error': 'exclamation-circle',
        'info': 'info-circle',
        'warning': 'exclamation-triangle'
    };
    
    const icono = iconos[type] || 'info-circle';
    
    notification.innerHTML = `
        <i class="fa-solid fa-${icono}"></i>
        <span>${message}</span>
    `;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.classList.add('show');
    }, 100);
    
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

window.openCreateCategoriaModal = openCreateCategoriaModal;
window.openEditCategoriaModal = openEditCategoriaModal;
window.openDeleteCategoriaModal = openDeleteCategoriaModal;
window.closeModal = closeModal;