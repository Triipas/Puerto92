/**
 * Gestión de Modales de Locales
 * Puerto 92 - Sistema de Gestión
 */

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', function() {
    setupSearch();
    setupNavActive();
    setupModalEventListeners();
});

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de crear local
 */
function openCreateLocalModal() {
    const modal = document.getElementById('createLocalModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    document.getElementById('createLocalForm').reset();
}

/**
 * Abrir modal de editar local
 */
async function openEditLocalModal(id) {
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

    } catch (error) {
        console.error('Error al cargar local:', error);
        showNotification('Error al cargar la información del local', 'error');
    }
}

/**
 * Abrir modal de eliminar/desactivar local
 */
async function openDeleteLocalModal(id, codigo, nombre, direccion, distrito, ciudad) {
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

    } catch (error) {
        console.error('Error al cargar información del local:', error);
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
    }, 200); // Esperar animación
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
    if (!searchInput) return;

    searchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase();
        const rows = document.querySelectorAll('#localesTable tbody tr');
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            row.style.display = text.includes(searchValue) ? '' : 'none';
        });
    });
}

// ==========================================
// UTILIDADES
// ==========================================

/**
 * Marcar nav-link activo
 */
function setupNavActive() {
    const links = document.querySelectorAll('.nav-link');
    links.forEach(link => {
        if (link.href === window.location.href) {
            link.classList.add('active');
        }
    });
}

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