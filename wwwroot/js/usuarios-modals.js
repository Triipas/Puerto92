/**
 * Gestión de Modales de Usuarios
 * Puerto 92 - Sistema de Gestión
 */

// Variables globales
let roles = [];
let locales = [];

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', async function() {
    await cargarRolesYLocales();
    setupSearch();
    setupNavActive();
    setupModalEventListeners();
});

// ==========================================
// CARGA DE DATOS
// ==========================================

/**
 * Cargar roles y locales desde el servidor
 */
async function cargarRolesYLocales() {
    try {
        const response = await fetch('/Usuarios/GetRolesYLocales');
        const data = await response.json();
        
        roles = data.roles;
        locales = data.locales;

        // Llenar selects de Crear
        populateSelect('createRolId', roles);
        populateSelect('createLocalId', locales);

        // Llenar selects de Editar
        populateSelect('editRolId', roles);
        populateSelect('editLocalId', locales);

    } catch (error) {
        console.error('Error al cargar roles y locales:', error);
        showNotification('Error al cargar datos del formulario', 'error');
    }
}

/**
 * Llenar un select con opciones
 */
function populateSelect(selectId, options) {
    const select = document.getElementById(selectId);
    options.forEach(option => {
        const optionElement = document.createElement('option');
        optionElement.value = option.value;
        optionElement.textContent = option.text;
        select.appendChild(optionElement);
    });
}

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de crear usuario
 */
function openCreateModal() {
    const modal = document.getElementById('createModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    document.getElementById('createForm').reset();
}

/**
 * Abrir modal de editar usuario
 */
async function openEditModal(id) {
    try {
        const response = await fetch(`/Usuarios/GetUsuario?id=${id}`);
        
        if (!response.ok) {
            throw new Error('Usuario no encontrado');
        }

        const usuario = await response.json();

        // Llenar formulario
        document.getElementById('editId').value = usuario.id;
        document.getElementById('editNombreCompleto').value = usuario.nombreCompleto;
        document.getElementById('editUserName').value = usuario.userName;
        document.getElementById('editRolId').value = usuario.rolId;
        document.getElementById('editLocalId').value = usuario.localId;
        document.getElementById('editActivo').value = usuario.activo.toString().toLowerCase();
        document.getElementById('editPassword').value = '';

        // Configurar acción del formulario
        document.getElementById('editForm').action = `/Usuarios/Edit/${id}`;
        
        // Mostrar modal
        const modal = document.getElementById('editModal');
        modal.style.display = 'flex';
        modal.classList.add('active');

    } catch (error) {
        console.error('Error al cargar usuario:', error);
        showNotification('Error al cargar la información del usuario', 'error');
    }
}

/**
 * Abrir modal de eliminar usuario
 */
function openDeleteModal(id, nombre, usuario, rol, local) {
    // Llenar información del usuario
    document.getElementById('deleteId').value = id;
    document.getElementById('deleteNombre').textContent = nombre;
    document.getElementById('deleteUsuario').textContent = usuario;
    document.getElementById('deleteRol').textContent = rol;
    document.getElementById('deleteLocal').textContent = local;
    
    // Configurar acción del formulario
    document.getElementById('deleteForm').action = `/Usuarios/Delete/${id}`;
    
    // Mostrar modal
    const modal = document.getElementById('deleteModal');
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
        const rows = document.querySelectorAll('#usuariosTable tbody tr');
        
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
 * Mostrar notificación (opcional - para futuras mejoras)
 */
function showNotification(message, type = 'info') {
    // Implementación simple con alert
    // Puedes mejorar esto con un sistema de notificaciones más elegante
    console.log(`[${type.toUpperCase()}] ${message}`);
    if (type === 'error') {
        alert(message);
    }
}

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

// Las funciones que necesitan ser llamadas desde el HTML
// ya están definidas en el scope global
window.openCreateModal = openCreateModal;
window.openEditModal = openEditModal;
window.openDeleteModal = openDeleteModal;
window.closeModal = closeModal;