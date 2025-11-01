/**
 * Gestión de Modales de Usuarios
 * Puerto 92 - Sistema de Gestión
 */

// Variables globales
let roles = [];
let locales = [];
let generatedPassword = ''; // Almacenar contraseña generada

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', async function() {
    await cargarRolesYLocales();
    setupSearch();
    setupNavActive();
    setupModalEventListeners();
    setupCreateFormSubmit(); // Interceptar submit de crear usuario
    checkClipboardPermissions(); // Verificar permisos de portapapeles
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

        // Llenar formulario (SIN campo de contraseña)
        document.getElementById('editId').value = usuario.id;
        document.getElementById('editNombreCompleto').value = usuario.nombreCompleto;
        document.getElementById('editUserName').value = usuario.userName;
        document.getElementById('editRolId').value = usuario.rolId;
        document.getElementById('editLocalId').value = usuario.localId;
        document.getElementById('editActivo').value = usuario.activo.toString().toLowerCase();

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
 * Abrir modal de resetear contraseña
 */
function openResetPasswordModal(id, nombre, username, rol) {
    // Generar contraseña temporal
    generatedPassword = generateTemporaryPassword();
    
    // Llenar información del usuario
    document.getElementById('resetPasswordUserId').value = id;
    document.getElementById('resetPasswordTemp').value = generatedPassword;
    document.getElementById('resetPasswordNombre').textContent = nombre;
    document.getElementById('resetPasswordUsername').textContent = username;
    document.getElementById('resetPasswordRol').textContent = rol;
    document.getElementById('resetPasswordGenerated').textContent = generatedPassword;
    
    // Configurar acción del formulario
    document.getElementById('resetPasswordForm').action = `/Usuarios/ResetPassword/${id}`;
    
    // Mostrar modal
    const modal = document.getElementById('resetPasswordModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Cerrar modal de resetear contraseña
 */
function closeResetPasswordModal() {
    closeModal('resetPasswordModal');
    generatedPassword = ''; // Limpiar contraseña generada
}

/**
 * Generar contraseña temporal segura
 */
function generateTemporaryPassword() {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789';
    let password = 'Puerto92_';
    
    for (let i = 0; i < 8; i++) {
        password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    
    return password;
}

/**
 * Copiar contraseña al portapapeles (modal de resetear)
 */
async function copyPassword() {
    try {
        const passwordElement = document.getElementById('resetPasswordGenerated');
        
        if (!passwordElement) {
            throw new Error('Elemento de contraseña no encontrado');
        }
        
        const password = passwordElement.textContent.trim();
        
        if (!password || password === '') {
            throw new Error('Contraseña vacía');
        }
        
        await navigator.clipboard.writeText(password);
        
        // Cambiar temporalmente el botón para indicar que se copió
        const button = document.getElementById('copyResetPasswordBtn');
        if (!button) return;
        
        const originalHTML = button.innerHTML;
        button.innerHTML = '<i class="fa-solid fa-check"></i>';
        button.style.background = '#10B981';
        
        setTimeout(() => {
            button.innerHTML = originalHTML;
            button.style.background = '#3B82F6';
        }, 2000);
        
    } catch (err) {
        // Intentar método alternativo (fallback)
        try {
            const passwordElement = document.getElementById('resetPasswordGenerated');
            const password = passwordElement ? passwordElement.textContent.trim() : '';
            
            const tempInput = document.createElement('input');
            tempInput.value = password;
            tempInput.style.position = 'absolute';
            tempInput.style.left = '-9999px';
            document.body.appendChild(tempInput);
            tempInput.select();
            document.execCommand('copy');
            document.body.removeChild(tempInput);
            
            // Feedback visual
            const button = document.getElementById('copyResetPasswordBtn');
            if (button) {
                const originalHTML = button.innerHTML;
                button.innerHTML = '<i class="fa-solid fa-check"></i>';
                button.style.background = '#10B981';
                
                setTimeout(() => {
                    button.innerHTML = originalHTML;
                    button.style.background = '#3B82F6';
                }, 2000);
            }
        } catch (fallbackErr) {
            alert('Error al copiar la contraseña. Por favor seleccione y copie manualmente (Ctrl+C).');
        }
    }
}

/**
 * Copiar contraseña generada al crear usuario
 */
async function copyGeneratedPassword() {
    try {
        const passwordElement = document.getElementById('generatedPassword');
        if (!passwordElement) {
            throw new Error('Elemento de contraseña no encontrado');
        }
        
        const password = passwordElement.textContent.trim();
        
        if (!password || password === '') {
            throw new Error('Contraseña vacía');
        }
        
        await navigator.clipboard.writeText(password);
        
        // Cambiar temporalmente el botón para indicar que se copió
        const button = document.getElementById('copyPasswordBtn');
        if (!button) return;
        
        const originalHTML = button.innerHTML;
        button.innerHTML = '<i class="fa-solid fa-check"></i>';
        button.style.background = '#10B981';
        
        setTimeout(() => {
            button.innerHTML = originalHTML;
            button.style.background = '#3B82F6';
        }, 2000);
        
    } catch (err) {
        console.error('Error al copiar contraseña:', err);
        
        // Intentar método alternativo (fallback)
        try {
            const passwordElement = document.getElementById('generatedPassword');
            const password = passwordElement ? passwordElement.textContent.trim() : '';
            
            // Crear elemento temporal para copiar
            const tempInput = document.createElement('input');
            tempInput.value = password;
            tempInput.style.position = 'absolute';
            tempInput.style.left = '-9999px';
            document.body.appendChild(tempInput);
            tempInput.select();
            document.execCommand('copy');
            document.body.removeChild(tempInput);
            
            // Feedback visual
            const button = document.getElementById('copyPasswordBtn');
            if (button) {
                const originalHTML = button.innerHTML;
                button.innerHTML = '<i class="fa-solid fa-check"></i>';
                button.style.background = '#10B981';
                
                setTimeout(() => {
                    button.innerHTML = originalHTML;
                    button.style.background = '#3B82F6';
                }, 2000);
            }
        } catch (fallbackErr) {
            console.error('Método fallback también falló:', fallbackErr);
            alert('Error al copiar la contraseña. Por favor seleccione y copie manualmente (Ctrl+C).');
        }
    }
}

/**
 * Configurar submit del formulario de crear usuario
 */
function setupCreateFormSubmit() {
    const createForm = document.getElementById('createForm');
    if (!createForm) return;

    createForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const formData = new FormData(this);
        const submitButton = this.querySelector('button[type="submit"]');
        
        // Deshabilitar botón mientras se procesa
        submitButton.disabled = true;
        submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Creando...';
        
        try {
            const response = await fetch('/Usuarios/CreateAjax', {
                method: 'POST',
                body: formData
            });
            
            const result = await response.json();
            
            if (result.success) {
                // Cerrar modal de crear
                closeModal('createModal');
                
                // Mostrar modal con contraseña generada
                document.getElementById('generatedUserNombre').textContent = result.nombreCompleto;
                document.getElementById('generatedUserUsername').textContent = result.userName;
                document.getElementById('generatedUserRol').textContent = result.rolNombre;
                document.getElementById('generatedPassword').textContent = result.password;
                
                const passwordModal = document.getElementById('passwordGeneratedModal');
                passwordModal.style.display = 'flex';
                passwordModal.classList.add('active');
                
                // Recargar la tabla de usuarios después de un momento
                setTimeout(() => {
                    window.location.reload();
                }, 5000); // 5 segundos para que el admin copie la contraseña
                
            } else {
                alert('Error: ' + result.message);
            }
            
        } catch (error) {
            console.error('Error al crear usuario:', error);
            alert('Error al crear el usuario. Por favor intenta nuevamente.');
        } finally {
            // Rehabilitar botón
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="fa-solid fa-plus"></i> Crear Usuario';
        }
    });
}

/**
 * Cerrar modal de contraseña generada
 */
function closePasswordGeneratedModal() {
    const modal = document.getElementById('passwordGeneratedModal');
    modal.classList.remove('active');
    setTimeout(() => {
        modal.style.display = 'none';
        // Recargar la página para ver el nuevo usuario
        window.location.reload();
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
window.openResetPasswordModal = openResetPasswordModal;
window.closeResetPasswordModal = closeResetPasswordModal;
window.copyPassword = copyPassword;
window.copyGeneratedPassword = copyGeneratedPassword;
window.closePasswordGeneratedModal = closePasswordGeneratedModal;
window.closeModal = closeModal;