/**
 * Gestión de Modales de Usuarios - VERSIÓN CORREGIDA
 * Puerto 92 - Sistema de Gestión
 */

// Variables globales
let roles = [];
let locales = [];
let localCorporativoId = null;
let generatedPassword = '';

// Roles corporativos
const ROLES_CORPORATIVOS = ['Admin Maestro', 'Contador', 'Supervisora de Calidad'];

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

/**
 * Función de inicialización que se ejecuta cada vez que se carga la página
 */
function initUsuariosPage() {
    console.log('🔄 Inicializando página de usuarios...');
    
    // Limpiar datos anteriores
    roles = [];
    locales = [];
    localCorporativoId = null;
    
    // Cargar datos y configurar listeners
    cargarRolesYLocales().then(() => {
        setupSearch();
        setupModalEventListeners();
        setupRolChangeListeners();
        // ⚠️ IMPORTANTE: Configurar DESPUÉS de que el DOM esté listo
        setupCreateFormSubmit();
        console.log('✅ Página de usuarios inicializada correctamente');
    });
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initUsuariosPage);

// ⭐ Exponer función para reinicializar después de navegación SPA
window.initUsuariosPage = initUsuariosPage;

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
        const rows = document.querySelectorAll('#usuariosTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const isVisible = text.includes(searchValue);
            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount++;
        });
        
        console.log(`✅ Búsqueda: "${searchValue}" - Mostrando ${visibleCount} de ${rows.length} usuarios`);
    });
    
    console.log('✅ Búsqueda configurada correctamente');
}

// ==========================================
// CARGA DE DATOS
// ==========================================

/**
 * Cargar roles y locales desde el servidor
 */
async function cargarRolesYLocales() {
    console.log('📥 Cargando roles y locales...');
    
    try {
        const response = await fetch('/Usuarios/GetRolesYLocales');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        const data = await response.json();

        roles = data.roles || [];
        locales = data.locales || [];

        console.log(`✅ Cargados ${roles.length} roles y ${locales.length} locales`);

        // Encontrar el local corporativo
        const localCorp = locales.find(l => l.text.toLowerCase().includes('corporativo'));
        if (localCorp) {
            localCorporativoId = localCorp.value;
            console.log(`📍 Local corporativo identificado: ${localCorp.text}`);
        }

        // Llenar selects de Crear
        await llenarSelectsModal('create');
        
        // Llenar selects de Editar
        await llenarSelectsModal('edit');

        console.log('✅ Selects llenados correctamente');

    } catch (error) {
        console.error('❌ Error al cargar roles y locales:', error);
        showNotification('Error al cargar datos del formulario', 'error');
    }
}

/**
 * Llenar los selects de un modal específico
 */
async function llenarSelectsModal(tipo) {
    const rolSelectId = `${tipo}RolId`;
    const localSelectId = `${tipo}LocalId`;
    
    // Limpiar y llenar select de roles
    const rolSelect = document.getElementById(rolSelectId);
    if (rolSelect) {
        // Limpiar opciones excepto la primera (placeholder)
        while (rolSelect.options.length > 1) {
            rolSelect.remove(1);
        }
        
        // Agregar roles
        roles.forEach(rol => {
            const option = document.createElement('option');
            option.value = rol.value;
            option.textContent = rol.text;
            rolSelect.appendChild(option);
        });
        
        console.log(`✅ Select de roles (${tipo}) llenado con ${roles.length} opciones`);
    }
    
    // Limpiar y llenar select de locales (sin corporativo por defecto)
    const localSelect = document.getElementById(localSelectId);
    if (localSelect) {
        // Limpiar opciones excepto la primera (placeholder)
        while (localSelect.options.length > 1) {
            localSelect.remove(1);
        }
        
        // Filtrar locales operativos (sin corporativo)
        const localesOperativos = locales.filter(l => l.value !== localCorporativoId);
        
        // Agregar locales
        localesOperativos.forEach(local => {
            const option = document.createElement('option');
            option.value = local.value;
            option.textContent = local.text;
            localSelect.appendChild(option);
        });
        
        console.log(`✅ Select de locales (${tipo}) llenado con ${localesOperativos.length} opciones`);
    }
}

/**
 * Filtrar locales en el select según el tipo de rol
 */
function filterLocales(localSelectId, showOnlyCorporative) {
    const localSelect = document.getElementById(localSelectId);
    if (!localSelect) return;

    // Limpiar todas las opciones excepto la primera (placeholder)
    while (localSelect.options.length > 1) {
        localSelect.remove(1);
    }

    // Filtrar y agregar locales según el criterio
    const filteredLocales = locales.filter(local => {
        if (showOnlyCorporative) {
            return local.value === localCorporativoId;
        } else {
            return local.value !== localCorporativoId;
        }
    });

    // Agregar las opciones filtradas
    filteredLocales.forEach(local => {
        const option = document.createElement('option');
        option.value = local.value;
        option.textContent = local.text;
        localSelect.appendChild(option);
    });
    
    console.log(`✅ Locales filtrados: ${filteredLocales.length} opciones`);
}

/**
 * Configurar listeners para detectar cambios en el select de rol
 */
function setupRolChangeListeners() {
    // Modal de Crear
    const createRolSelect = document.getElementById('createRolId');
    if (createRolSelect) {
        // Remover listener anterior si existe
        createRolSelect.removeEventListener('change', handleCreateRolChange);
        createRolSelect.addEventListener('change', handleCreateRolChange);
    }

    // Modal de Editar
    const editRolSelect = document.getElementById('editRolId');
    if (editRolSelect) {
        // Remover listener anterior si existe
        editRolSelect.removeEventListener('change', handleEditRolChange);
        editRolSelect.addEventListener('change', handleEditRolChange);
    }
}

function handleCreateRolChange() {
    handleRolChange('createRolId', 'createLocalId');
}

function handleEditRolChange() {
    handleRolChange('editRolId', 'editLocalId');
}

/**
 * Manejar cambio de rol - bloquear local si es corporativo
 */
function handleRolChange(rolSelectId, localSelectId) {
    const rolSelect = document.getElementById(rolSelectId);
    const localSelect = document.getElementById(localSelectId);

    if (!rolSelect || !localSelect) return;

    const selectedRolId = rolSelect.value;
    const selectedRol = roles.find(r => r.value === selectedRolId);

    if (!selectedRol) {
        filterLocales(localSelectId, false);
        return;
    }

    const isRolCorporativo = ROLES_CORPORATIVOS.includes(selectedRol.text);

    if (isRolCorporativo) {
        localSelect.disabled = true;
        filterLocales(localSelectId, true);
        localSelect.value = localCorporativoId;
        localSelect.style.background = '#F3F4F6';
        localSelect.style.color = '#6B7280';
        localSelect.style.cursor = 'not-allowed';
        addCorporativeMessage(localSelectId);
    } else {
        localSelect.disabled = false;
        localSelect.style.background = '';
        localSelect.style.color = '';
        localSelect.style.cursor = '';
        filterLocales(localSelectId, false);
        
        const firstOperativeLocal = locales.find(l => l.value !== localCorporativoId);
        if (firstOperativeLocal) {
            localSelect.value = firstOperativeLocal.value;
        }
        
        removeCorporativeMessage(localSelectId);
    }
}

/**
 * Agregar mensaje informativo para roles corporativos
 */
function addCorporativeMessage(localSelectId) {
    const existingMessage = document.getElementById(`${localSelectId}-corp-msg`);
    if (existingMessage) return;

    const localSelect = document.getElementById(localSelectId);
    const formGroup = localSelect.closest('.form-group');

    const message = document.createElement('div');
    message.id = `${localSelectId}-corp-msg`;
    message.style.cssText = 'background: #EFF6FF; border: 1px solid #BFDBFE; border-radius: 6px; padding: 0.625rem; margin-top: 0.5rem; display: flex; align-items: start; gap: 0.5rem;';
    message.innerHTML = `
        <i class="fa-solid fa-info-circle" style="color: #3B82F6; margin-top: 2px;"></i>
        <span style="font-size: 12px; color: #1E40AF; line-height: 1.4;">
            Este rol es <strong>corporativo</strong> y se asigna automáticamente a las oficinas centrales.
        </span>
    `;

    formGroup.appendChild(message);
}

/**
 * Remover mensaje informativo
 */
function removeCorporativeMessage(localSelectId) {
    const message = document.getElementById(`${localSelectId}-corp-msg`);
    if (message) {
        message.remove();
    }
}

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de crear usuario
 */
async function openCreateModal() {
    console.log('📝 Abriendo modal de crear usuario...');
    
    // Asegurar que los datos estén cargados
    if (roles.length === 0 || locales.length === 0) {
        console.log('⚠️ Datos no cargados, recargando...');
        await cargarRolesYLocales();
    }
    
    const modal = document.getElementById('createModal');
    modal.style.display = 'flex';
    modal.classList.add('active');

    // Resetear formulario
    document.getElementById('createForm').reset();

    // Re-llenar los selects por si acaso
    await llenarSelectsModal('create');
    
    // Asegurar que se muestran solo locales operativos al inicio
    setTimeout(() => {
        filterLocales('createLocalId', false);
    }, 100);
    
    // ⚠️ RE-CONFIGURAR el submit cada vez que se abre el modal
    setupCreateFormSubmit();
    
    console.log('✅ Modal de crear usuario abierto');
}

/**
 * Abrir modal de editar usuario
 */
async function openEditModal(id) {
    console.log(`✏️ Abriendo modal de editar usuario: ${id}`);
    
    try {
        // Asegurar que los datos estén cargados
        if (roles.length === 0 || locales.length === 0) {
            console.log('⚠️ Datos no cargados, recargando...');
            await cargarRolesYLocales();
        }
        
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

        // Configurar acción del formulario
        document.getElementById('editForm').action = `/Usuarios/Edit/${id}`;

        // Mostrar modal
        const modal = document.getElementById('editModal');
        modal.style.display = 'flex';
        modal.classList.add('active');

        // Verificar si el rol actual es corporativo
        setTimeout(() => {
            handleRolChange('editRolId', 'editLocalId');
        }, 100);
        
        console.log('✅ Modal de editar usuario abierto');

    } catch (error) {
        console.error('❌ Error al cargar usuario:', error);
        showNotification('Error al cargar la información del usuario', 'error');
    }
}

/**
 * Abrir modal de eliminar usuario
 */
function openDeleteModal(id, nombre, usuario, rol, local) {
    document.getElementById('deleteId').value = id;
    document.getElementById('deleteNombre').textContent = nombre;
    document.getElementById('deleteUsuario').textContent = usuario;
    document.getElementById('deleteRol').textContent = rol;
    document.getElementById('deleteLocal').textContent = local;

    document.getElementById('deleteForm').action = `/Usuarios/Delete/${id}`;

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

    if (modalId === 'createModal') {
        removeCorporativeMessage('createLocalId');
    } else if (modalId === 'editModal') {
        removeCorporativeMessage('editLocalId');
    }

    setTimeout(() => {
        modal.style.display = 'none';
    }, 200);
}

/**
 * Abrir modal de resetear contraseña
 */
function openResetPasswordModal(id, nombre, username, rol) {
    generatedPassword = generateTemporaryPassword();

    document.getElementById('resetPasswordUserId').value = id;
    document.getElementById('resetPasswordTemp').value = generatedPassword;
    document.getElementById('resetPasswordNombre').textContent = nombre;
    document.getElementById('resetPasswordUsername').textContent = username;
    document.getElementById('resetPasswordRol').textContent = rol;
    document.getElementById('resetPasswordGenerated').textContent = generatedPassword;

    document.getElementById('resetPasswordForm').action = `/Usuarios/ResetPassword/${id}`;

    const modal = document.getElementById('resetPasswordModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Cerrar modal de resetear contraseña
 */
function closeResetPasswordModal() {
    closeModal('resetPasswordModal');
    generatedPassword = '';
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
 * Copiar contraseña al portapapeles
 */
async function copyPassword() {
    try {
        const passwordElement = document.getElementById('resetPasswordGenerated');
        const password = passwordElement.textContent.trim();
        await navigator.clipboard.writeText(password);

        const button = document.getElementById('copyResetPasswordBtn');
        const originalHTML = button.innerHTML;
        button.innerHTML = '<i class="fa-solid fa-check"></i>';
        button.style.background = '#10B981';

        setTimeout(() => {
            button.innerHTML = originalHTML;
            button.style.background = '#3B82F6';
        }, 2000);
    } catch (err) {
        console.error('Error al copiar:', err);
        alert('Error al copiar la contraseña. Por favor cópiela manualmente.');
    }
}

/**
 * Copiar contraseña generada al crear usuario
 */
async function copyGeneratedPassword() {
    try {
        const passwordElement = document.getElementById('generatedPassword');
        const password = passwordElement.textContent.trim();
        await navigator.clipboard.writeText(password);

        const button = document.getElementById('copyPasswordBtn');
        const originalHTML = button.innerHTML;
        button.innerHTML = '<i class="fa-solid fa-check"></i>';
        button.style.background = '#10B981';

        setTimeout(() => {
            button.innerHTML = originalHTML;
            button.style.background = '#3B82F6';
        }, 2000);
    } catch (err) {
        console.error('Error al copiar:', err);
        alert('Error al copiar la contraseña. Por favor cópiela manualmente.');
    }
}

/**
 * Configurar submit del formulario de crear usuario
 * ⚠️ VERSIÓN CORREGIDA - SE CONFIGURA CADA VEZ QUE SE ABRE EL MODAL
 */
function setupCreateFormSubmit() {
    const createForm = document.getElementById('createForm');
    
    if (!createForm) {
        console.warn('⚠️ Formulario createForm no encontrado');
        return;
    }

    console.log('📝 Configurando submit del formulario de crear usuario');

    // ⚠️ IMPORTANTE: Remover TODOS los listeners anteriores
    const newForm = createForm.cloneNode(true);
    createForm.parentNode.replaceChild(newForm, createForm);
    
    // Obtener referencia al nuevo formulario
    const form = document.getElementById('createForm');
    
    // Agregar listener de submit
    form.addEventListener('submit', async function (e) {
        e.preventDefault(); // ⚠️ PREVENIR submit tradicional
        e.stopPropagation(); // ⚠️ DETENER propagación

        console.log('🚀 Formulario de crear usuario enviado');

        const formData = new FormData(this);
        const submitButton = this.querySelector('button[type="submit"]');

        if (!submitButton) {
            console.error('❌ Botón de submit no encontrado');
            return;
        }

        submitButton.disabled = true;
        submitButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Creando...';

        try {
            console.log('📤 Enviando datos para crear usuario...');
            
            const response = await fetch('/Usuarios/CreateAjax', {
                method: 'POST',
                body: formData
            });

            console.log('📥 Respuesta recibida:', response.status);

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const result = await response.json();
            console.log('✅ Resultado:', result);

            if (result.success && result.data) {
                console.log('🎉 Usuario creado exitosamente');
                console.log('📋 Datos recibidos:', {
                    nombre: result.data.nombreCompleto,
                    username: result.data.userName,
                    rol: result.data.rolNombre,
                    passwordLength: result.data.password ? result.data.password.length : 0
                });
                
                // Cerrar modal de crear
                closeModal('createModal');

                // Esperar un momento para que se cierre el modal
                setTimeout(() => {
                    // Llenar datos en el modal de contraseña generada
                    const nombreElem = document.getElementById('generatedUserNombre');
                    const usernameElem = document.getElementById('generatedUserUsername');
                    const rolElem = document.getElementById('generatedUserRol');
                    const passwordElem = document.getElementById('generatedPassword');

                    if (nombreElem) nombreElem.textContent = result.data.nombreCompleto || 'N/A';
                    if (usernameElem) usernameElem.textContent = result.data.userName || 'N/A';
                    if (rolElem) rolElem.textContent = result.data.rolNombre || 'N/A';
                    if (passwordElem) passwordElem.textContent = result.data.password || 'N/A';

                    console.log('📝 Datos llenados en modal de contraseña');

                    // Abrir modal de contraseña generada
                    const passwordModal = document.getElementById('passwordGeneratedModal');
                    
                    if (!passwordModal) {
                        console.error('❌ Modal passwordGeneratedModal no encontrado en el DOM');
                        alert(`Usuario creado exitosamente.\n\nContraseña temporal: ${result.data.password}\n\n⚠️ IMPORTANTE: Anote esta contraseña, no se volverá a mostrar.`);
                        window.location.reload();
                        return;
                    }

                    console.log('🔓 Abriendo modal de contraseña generada...');
                    passwordModal.style.display = 'flex';
                    
                    // Forzar reflow
                    void passwordModal.offsetWidth;
                    
                    passwordModal.classList.add('active');
                    console.log('✅ Modal de contraseña generada visible');

                    // Recargar después de 10 segundos
                    setTimeout(() => {
                        console.log('🔄 Recargando página...');
                        window.location.reload();
                    }, 10000);

                }, 400);

            } else {
                console.error('❌ Error en respuesta:', result.message);
                showNotification(result.message || 'Error al crear usuario', 'error');
            }

        } catch (error) {
            console.error('❌ Error al crear usuario:', error);
            showNotification('Error al crear el usuario. Por favor intenta nuevamente.', 'error');
        } finally {
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="fa-solid fa-plus"></i> Crear Usuario';
        }
    });
    
    console.log('✅ Submit del formulario configurado correctamente');
}

/**
 * Cerrar modal de contraseña generada
 */
function closePasswordGeneratedModal() {
    console.log('🔒 Cerrando modal de contraseña generada...');
    
    const modal = document.getElementById('passwordGeneratedModal');
    if (!modal) {
        console.warn('⚠️ Modal passwordGeneratedModal no encontrado');
        window.location.reload();
        return;
    }
    
    modal.classList.remove('active');
    
    setTimeout(() => {
        modal.style.display = 'none';
        console.log('✅ Modal cerrado, recargando página...');
        window.location.reload();
    }, 300);
}

/**
 * Configurar event listeners para los modales
 */
function setupModalEventListeners() {
    document.querySelectorAll('.modal-overlay').forEach(overlay => {
        overlay.addEventListener('click', function (e) {
            if (e.target === this) {
                this.classList.remove('active');
                setTimeout(() => {
                    this.style.display = 'none';
                }, 200);
            }
        });
    });

    document.addEventListener('keydown', function (e) {
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

window.openCreateModal = openCreateModal;
window.openEditModal = openEditModal;
window.openDeleteModal = openDeleteModal;
window.openResetPasswordModal = openResetPasswordModal;
window.closeResetPasswordModal = closeResetPasswordModal;
window.copyPassword = copyPassword;
window.copyGeneratedPassword = copyGeneratedPassword;
window.closePasswordGeneratedModal = closePasswordGeneratedModal;
window.closeModal = closeModal;
window.handleRolChange = handleRolChange;