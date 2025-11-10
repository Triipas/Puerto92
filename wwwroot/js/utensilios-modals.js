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

async function cargarCategoriasUtensilios() {
    const createSelect = document.getElementById('createCategoriaId');
    
    try {
        console.log('🔄 Cargando categorías de utensilios...');
        
        // Mostrar loading en el select
        if (createSelect) {
            createSelect.innerHTML = '<option value="">Cargando categorías...</option>';
            createSelect.disabled = true;
        }
        
        const response = await fetch('/Categorias/GetCategoriasPorTipo?tipo=Utensilios', {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });
        
        // Verificar si la respuesta es JSON
        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            const text = await response.text();
            console.error('❌ Respuesta no es JSON:', text.substring(0, 200));
            throw new Error('El servidor devolvió HTML en lugar de JSON. Verifica que el endpoint exista.');
        }
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const categorias = await response.json();
        console.log('✅ Categorías obtenidas:', categorias);
        
        // Llenar select de crear
        if (createSelect) {
            createSelect.innerHTML = '<option value="">Seleccione una categoría...</option>';
            
            if (categorias && categorias.length > 0) {
                categorias.forEach(cat => {
                    createSelect.innerHTML += `<option value="${cat.id}">${cat.nombre}</option>`;
                });
                console.log(`✅ ${categorias.length} categorías cargadas en el select`);
            } else {
                console.warn('⚠️ No se encontraron categorías activas de tipo Utensilios');
                createSelect.innerHTML += '<option value="" disabled>No hay categorías disponibles</option>';
                showNotification('No hay categorías de tipo "Utensilios" disponibles. Créelas primero en el módulo de Categorías.', 'warning');
            }
            
            createSelect.disabled = false;
        }
        
    } catch (error) {
        console.error('❌ Error al cargar categorías:', error);
        
        // Mostrar error específico
        let mensaje = 'Error al cargar categorías.';
        if (error.message.includes('JSON')) {
            mensaje += ' El endpoint puede no existir o no tiene permisos.';
        }
        
        showNotification(mensaje, 'error');
        
        // Mostrar error en el select
        if (createSelect) {
            createSelect.innerHTML = '<option value="">⚠️ Error al cargar</option>';
            createSelect.disabled = false;
        }
    }
}

/**
 * Abrir modal de crear utensilio
 */
async function openCreateUtensilioModal() {
    console.log('📝 Abriendo modal de crear utensilio...');
    
    // Mostrar modal primero
    const modal = document.getElementById('createUtensilioModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    // Resetear formulario
    const form = document.getElementById('createUtensilioForm');
    if (form) {
        form.reset();
    }
    
    const checkbox = document.getElementById('guardarYAgregarOtro');
    if (checkbox) {
        checkbox.checked = false;
    }
    
    // ⭐ CARGAR CATEGORÍAS DESPUÉS DE MOSTRAR EL MODAL
    await cargarCategoriasUtensilios();
    
    console.log('✅ Modal de crear utensilio abierto');
}

/**
 * Abrir modal de editar utensilio
 */
async function openEditUtensilioModal(id) {
    console.log(`✏️ Abriendo modal de editar utensilio: ${id}`);
    
    try {
        const response = await fetch(`/Utensilios/GetUtensilio?id=${id}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error('Utensilio no encontrado');
        }

        const utensilio = await response.json();
        console.log('✅ Utensilio obtenido:', utensilio);

        // Llenar formulario con los datos correctos
        document.getElementById('editUtensilioId').value = utensilio.id;
        document.getElementById('editUtensilioCodigoDisplay').textContent = utensilio.codigo;
        document.getElementById('editUtensilioCodigoInput').value = utensilio.codigo;
        
        // ⭐ USAR categoriaNombre EN LUGAR DE tipo
        const categoriaNombre = utensilio.categoriaNombre || utensilio.tipo || 'Sin categoría';
        document.getElementById('editUtensilioTipoDisplay').textContent = getCategoriaDisplay(categoriaNombre);
        document.getElementById('editUtensilioTipoInput').value = categoriaNombre;
        
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
function openDesactivarUtensilioModal(id, codigo, nombre, categoriaNombre) {
    console.log(`🗑️ Abriendo modal de desactivar utensilio: ${id}`);
    
    document.getElementById('desactivarUtensilioId').value = id;
    document.getElementById('desactivarUtensilioCode').textContent = codigo;
    document.getElementById('desactivarUtensilioNombre').textContent = nombre;
    document.getElementById('desactivarUtensilioTipo').textContent = getCategoriaDisplay(categoriaNombre);

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
    if (!createForm) {
        console.warn('⚠️ Formulario createUtensilioForm no encontrado');
        return;
    }

    console.log('🔧 Configurando handler del formulario de crear');

    // Remover listener anterior si existe
    const newForm = createForm.cloneNode(true);
    createForm.parentNode.replaceChild(newForm, createForm);
    
    newForm.addEventListener('submit', async function(e) {
        const guardarYAgregarOtro = document.getElementById('guardarYAgregarOtro');
        
        // Si está marcado "Guardar y Agregar Otro", prevenir el comportamiento por defecto
        if (guardarYAgregarOtro && guardarYAgregarOtro.checked) {
            e.preventDefault(); // ⭐ PREVENIR RECARGA
            console.log('💾 Guardando con opción "Agregar Otro"...');
            
            const formData = new FormData(this);
            const submitButton = this.querySelector('button[type="submit"]');
            
            // Agregar token antiforgery si existe
            const tokenInput = this.querySelector('input[name="__RequestVerificationToken"]');
            if (tokenInput) {
                formData.append('__RequestVerificationToken', tokenInput.value);
            }
            
            submitButton.disabled = true;
            submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Guardando...';
            
            try {
                const response = await fetch(this.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });
                
                if (response.ok) {
                    console.log('✅ Utensilio guardado exitosamente');
                    
                    // Limpiar formulario pero mantener el modal abierto
                    this.reset();
                    
                    // ⭐ RECARGAR CATEGORÍAS DESPUÉS DEL RESET
                    await cargarCategoriasUtensilios();
                    
                    // Mostrar notificación de éxito
                    showNotification('Utensilio agregado exitosamente. Puede agregar otro.', 'success');
                    
                    // Enfocar el campo de nombre
                    const nombreInput = this.querySelector('input[name="Nombre"]');
                    if (nombreInput) {
                        setTimeout(() => nombreInput.focus(), 100);
                    }
                } else {
                    const errorText = await response.text();
                    console.error('❌ Error del servidor:', errorText);
                    showNotification('Error al guardar el utensilio', 'error');
                }
            } catch (error) {
                console.error('❌ Error:', error);
                showNotification('Error al guardar el utensilio', 'error');
            } finally {
                submitButton.disabled = false;
                submitButton.innerHTML = '<i class="fa-solid fa-plus"></i> Agregar Utensilio';
            }
        }
        // Si no está marcado, dejar que el formulario se envíe normalmente
    });
    
    console.log('✅ Handler del formulario configurado');
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
 * Obtener display amigable de la categoría
 */
function getCategoriaDisplay(categoriaNombre) {
    const categorias = {
        'Cocina': '🔥 Cocina',
        'Mozos': '👔 Mozos',
        'Vajilla': '🍽️ Vajilla'
    };
    return categorias[categoriaNombre] || categoriaNombre;
}

/**
 * Mostrar notificación
 */
function showNotification(message, type = 'info') {
    console.log(`[${type.toUpperCase()}] ${message}`);
    
    // Iconos según el tipo
    const icons = {
        'success': 'check-circle',
        'error': 'exclamation-circle',
        'warning': 'exclamation-triangle',
        'info': 'info-circle'
    };
    
    const icon = icons[type] || icons.info;
    
    // Crear elemento de notificación
    const notification = document.createElement('div');
    notification.className = `app-notification ${type}`;
    notification.innerHTML = `
        <i class="fa-solid fa-${icon}"></i>
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
window.cargarCategoriasUtensilios = cargarCategoriasUtensilios;