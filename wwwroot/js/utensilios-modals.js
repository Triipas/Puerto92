/**
 * Gestión de Modales de Utensilios
 * Puerto 92 - Sistema de Gestión
 */

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

/**
 * Función de inicialización que se ejecuta cada vez que se carga la página
 */
function initUtensiliosPage() {
    console.log('🔄 Inicializando página de utensilios...');
    
    setupSearch();
    setupModalEventListeners();
    setupCreateFormHandler();
    
    console.log('✅ Página de utensilios inicializada correctamente');
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initUtensiliosPage);

// ⭐ Exponer función para reinicializar después de navegación SPA
window.initUtensiliosPage = initUtensiliosPage;

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de crear utensilio
 */
function openCreateUtensilioModal() {
    console.log('📝 Abriendo modal de crear utensilio...');
    
    const modal = document.getElementById('createUtensilioModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    document.getElementById('createUtensilioForm').reset();
    document.getElementById('guardarYAgregarOtro').checked = false;
    
    console.log('✅ Modal de crear utensilio abierto');
}

/**
 * Abrir modal de editar utensilio
 */
async function openEditUtensilioModal(id) {
    console.log(`✏️ Abriendo modal de editar utensilio: ${id}`);
    
    try {
        const response = await fetch(`/Utensilios/GetUtensilio?id=${id}`);
        
        if (!response.ok) {
            throw new Error('Utensilio no encontrado');
        }

        const utensilio = await response.json();

        // Llenar formulario
        document.getElementById('editUtensilioId').value = utensilio.id;
        document.getElementById('editUtensilioCodigoDisplay').textContent = utensilio.codigo;
        document.getElementById('editUtensilioCodigoInput').value = utensilio.codigo;
        document.getElementById('editUtensilioTipoDisplay').textContent = getTipoDisplay(utensilio.tipo);
        document.getElementById('editUtensilioTipoInput').value = utensilio.tipo;
        document.getElementById('editUtensilioNombre').value = utensilio.nombre;
        document.getElementById('editUtensilioUnidad').value = utensilio.unidad;
        document.getElementById('editUtensilioPrecio').value = utensilio.precio;
        document.getElementById('editUtensilioDescripcion').value = utensilio.descripcion || '';

        // Configurar acción del formulario
        document.getElementById('editUtensilioForm').action = `/Utensilios/Edit/${id}`;
        
        // Mostrar modal
        const modal = document.getElementById('editUtensilioModal');
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
function openDesactivarUtensilioModal(id, codigo, nombre, tipo) {
    console.log(`🗑️ Abriendo modal de desactivar utensilio: ${id}`);
    
    document.getElementById('desactivarUtensilioId').value = id;
    document.getElementById('desactivarUtensilioCode').textContent = codigo;
    document.getElementById('desactivarUtensilioNombre').textContent = nombre;
    document.getElementById('desactivarUtensilioTipo').textContent = getTipoDisplay(tipo);

    document.getElementById('desactivarUtensilioForm').action = `/Utensilios/Desactivar/${id}`;

    const modal = document.getElementById('desactivarUtensilioModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    console.log('✅ Modal de desactivar utensilio abierto');
}

/**
 * Abrir modal de carga masiva
 */
function openCargaMasivaModal() {
    console.log('📤 Abriendo modal de carga masiva...');
    
    const modal = document.getElementById('cargaMasivaModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    // Limpiar input file
    const fileInput = document.getElementById('archivoInput');
    if (fileInput) {
        fileInput.value = '';
    }
    
    const nombreArchivo = document.getElementById('nombreArchivo');
    if (nombreArchivo) {
        nombreArchivo.innerHTML = '';
    }
    
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
// MANEJO DE FORMULARIOS
// ==========================================

/**
 * Configurar el manejador del formulario de crear con "Guardar y Agregar Otro"
 */
function setupCreateFormHandler() {
    const createForm = document.getElementById('createUtensilioForm');
    if (!createForm) return;

    // Remover listener anterior si existe
    createForm.onsubmit = null;
    
    createForm.addEventListener('submit', async function(e) {
        const guardarYAgregarOtro = document.getElementById('guardarYAgregarOtro');
        
        // Si está marcado "Guardar y Agregar Otro", prevenir el comportamiento por defecto
        if (guardarYAgregarOtro && guardarYAgregarOtro.checked) {
            e.preventDefault();
            
            const formData = new FormData(this);
            const submitButton = this.querySelector('button[type="submit"]');
            
            submitButton.disabled = true;
            submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Guardando...';
            
            try {
                const response = await fetch(this.action, {
                    method: 'POST',
                    body: formData
                });
                
                if (response.ok) {
                    // Limpiar formulario pero mantener el modal abierto
                    this.reset();
                    
                    // Mostrar notificación de éxito
                    showNotification('Utensilio agregado exitosamente. Puede agregar otro.', 'success');
                    
                    // Enfocar el campo de nombre
                    const nombreInput = this.querySelector('input[name="Nombre"]');
                    if (nombreInput) {
                        nombreInput.focus();
                    }
                } else {
                    showNotification('Error al guardar el utensilio', 'error');
                }
            } catch (error) {
                console.error('Error:', error);
                showNotification('Error al guardar el utensilio', 'error');
            } finally {
                submitButton.disabled = false;
                submitButton.innerHTML = '<i class="fa-solid fa-plus"></i> Agregar Utensilio';
            }
        }
        // Si no está marcado, dejar que el formulario se envíe normalmente
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

    console.log('🔍 Configurando búsqueda de utensilios...');

    // Remover event listeners anteriores clonando el nodo
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
    
    console.log('✅ Búsqueda de utensilios configurada correctamente');
}

// ==========================================
// UTILIDADES
// ==========================================

/**
 * Obtener display amigable del tipo
 */
function getTipoDisplay(tipo) {
    const tipos = {
        'Cocina': '🔥 Cocina',
        'Mozos': '👔 Mozos',
        'Vajilla': '🍽️ Vajilla'
    };
    return tipos[tipo] || tipo;
}

/**
 * Mostrar notificación
 */
function showNotification(message, type = 'info') {
    console.log(`[${type.toUpperCase()}] ${message}`);
    
    // Crear elemento de notificación
    const notification = document.createElement('div');
    notification.className = `app-notification ${type}`;
    notification.innerHTML = `
        <i class="fa-solid fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i>
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

/**
 * Mostrar loading overlay
 */
function showLoading() {
    const loading = document.createElement('div');
    loading.className = 'loading-overlay';
    loading.id = 'loadingOverlay';
    loading.innerHTML = `
        <div class="loading-spinner">
            <i class="fa-solid fa-spinner fa-spin"></i>
            <span>Procesando...</span>
        </div>
    `;
    document.body.appendChild(loading);
}

/**
 * Ocultar loading overlay
 */
function hideLoading() {
    const loading = document.getElementById('loadingOverlay');
    if (loading) {
        loading.remove();
    }
}

// ==========================================
// DRAG & DROP PARA CARGA MASIVA
// ==========================================

/**
 * Manejar drop de archivos
 */
function handleDrop(event) {
    event.preventDefault();
    event.currentTarget.style.borderColor = '#A7F3D0';
    event.currentTarget.style.background = 'white';
    
    const files = event.dataTransfer.files;
    if (files.length > 0 && files[0].name.endsWith('.csv')) {
        document.getElementById('archivoInput').files = files;
        mostrarNombreArchivo(document.getElementById('archivoInput'));
    } else {
        showNotification('Por favor, seleccione un archivo CSV válido', 'error');
    }
}

/**
 * Mostrar nombre del archivo seleccionado
 */
function mostrarNombreArchivo(input) {
    const nombreArchivo = document.getElementById('nombreArchivo');
    if (input.files && input.files[0]) {
        nombreArchivo.innerHTML = `
            <i class="fa-solid fa-file-csv" style="color: #10B981;"></i>
            Archivo seleccionado: <strong>${input.files[0].name}</strong>
        `;
    }
}

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

window.openCreateUtensilioModal = openCreateUtensilioModal;
window.openEditUtensilioModal = openEditUtensilioModal;
window.openDesactivarUtensilioModal = openDesactivarUtensilioModal;
window.openCargaMasivaModal = openCargaMasivaModal;
window.closeModal = closeModal;
window.handleDrop = handleDrop;
window.mostrarNombreArchivo = mostrarNombreArchivo;