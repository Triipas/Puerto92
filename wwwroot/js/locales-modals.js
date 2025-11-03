/**
 * Gestión de Modales de Locales - VERSIÓN ARREGLADA
 * Puerto 92 - Sistema de Gestión
 */

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

/**
 * Función de inicialización que se ejecuta cada vez que se carga la página
 */
function initLocalesPage() {
    console.log('🔄 Inicializando página de locales...');
    
    setupSearch();
    setupModalEventListeners();
    
    console.log('✅ Página de locales inicializada correctamente');
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initLocalesPage);

// ⭐ NUEVO: Exponer función para reinicializar después de navegación SPA
window.initLocalesPage = initLocalesPage;

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de crear local
 */
function openCreateLocalModal() {
    console.log('📝 Abriendo modal de crear local...');
    
    const modal = document.getElementById('createLocalModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    document.getElementById('createLocalForm').reset();
    
    console.log('✅ Modal de crear local abierto');
}

/**
 * Abrir modal de editar local
 */
async function openEditLocalModal(id) {
    console.log(`✏️ Abriendo modal de editar local: ${id}`);
    
    try {
        const response = await fetch(`/Locales/GetLocal?id=${id}`);
        
        if (!response.ok) {
            throw new Error('Local no encontrado');
        }

        const local = await response.json();

        // Llenar formulario
        document.getElementById('editLocalId').value = local.id;
        document.getElementById('editLocalCodigo').textContent = local.codigo;
        document.getElementById('editLocalCodigoInput').value = local.codigo;
        document.getElementById('editLocalNombre').value = local.nombre;
        document.getElementById('editLocalDireccion').value = local.direccion || '';
        document.getElementById('editLocalDistrito').value = local.distrito || '';
        document.getElementById('editLocalCiudad').value = local.ciudad || '';
        document.getElementById('editLocalTelefono').value = local.telefono || '';
        document.getElementById('editLocalActivo').value = local.activo.toString().toLowerCase();

        // Configurar acción del formulario
        document.getElementById('editLocalForm').action = `/Locales/Edit/${id}`;
        
        // Mostrar modal
        const modal = document.getElementById('editLocalModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de editar local abierto');

    } catch (error) {
        console.error('❌ Error al cargar local:', error);
        showNotification('Error al cargar la información del local', 'error');
    }
}

/**
 * Abrir modal de eliminar/desactivar local
 */
async function openDeleteLocalModal(id, codigo, nombre, direccion, distrito, ciudad) {
    console.log(`🗑️ Abriendo modal de eliminar local: ${id}`);
    
    try {
        // Obtener información adicional del local
        const response = await fetch(`/Locales/GetLocalEstadisticas?id=${id}`);
        let estadisticas = { usuarios: 0, valorInventario: 0 };
        
        if (response.ok) {
            estadisticas = await response.json();
        }

        // Llenar información del local
        document.getElementById('deleteLocalId').value = id;
        document.getElementById('deleteLocalCodigo').textContent = codigo;
        document.getElementById('deleteLocalCodigoWarning').textContent = codigo;
        document.getElementById('deleteLocalNombre').textContent = nombre;
        document.getElementById('deleteLocalDireccion').textContent = direccion || 'No especificada';
        document.getElementById('deleteLocalDistritoCiudad').textContent = `${distrito || ''}, ${ciudad || ''}`;
        
        // Llenar estadísticas
        document.getElementById('deleteLocalUsuarios').textContent = estadisticas.usuarios;
        document.getElementById('deleteLocalValorInventario').textContent = estadisticas.valorInventario.toFixed(2);
        
        // Configurar acción del formulario
        document.getElementById('deleteLocalForm').action = `/Locales/Delete/${id}`;
        
        // Mostrar modal
        const modal = document.getElementById('deleteLocalModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de eliminar local abierto');

    } catch (error) {
        console.error('❌ Error al cargar información del local:', error);
        
        // Continuar mostrando el modal con información básica
        document.getElementById('deleteLocalId').value = id;
        document.getElementById('deleteLocalCodigo').textContent = codigo;
        document.getElementById('deleteLocalCodigoWarning').textContent = codigo;
        document.getElementById('deleteLocalNombre').textContent = nombre;
        document.getElementById('deleteLocalDireccion').textContent = direccion || 'No especificada';
        document.getElementById('deleteLocalDistritoCiudad').textContent = `${distrito || ''}, ${ciudad || ''}`;
        
        document.getElementById('deleteLocalForm').action = `/Locales/Delete/${id}`;
        
        const modal = document.getElementById('deleteLocalModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
    }
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

    console.log('🔍 Configurando búsqueda de locales...');

    // Remover event listeners anteriores clonando el nodo
    const newSearchInput = searchInput.cloneNode(true);
    searchInput.parentNode.replaceChild(newSearchInput, searchInput);

    newSearchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#localesTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const isVisible = text.includes(searchValue);
            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount++;
        });
        
        console.log(`✅ Búsqueda: "${searchValue}" - Mostrando ${visibleCount} de ${rows.length} locales`);
    });
    
    console.log('✅ Búsqueda de locales configurada correctamente');
}

// ==========================================
// UTILIDADES
// ==========================================

/**
 * Mostrar notificación
 */
function showNotification(message, type = 'info') {
    console.log(`[${type.toUpperCase()}] ${message}`);
    if (type === 'error') {
        alert(message);
    }
}

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

window.openCreateLocalModal = openCreateLocalModal;
window.openEditLocalModal = openEditLocalModal;
window.openDeleteLocalModal = openDeleteLocalModal;
window.closeModal = closeModal;