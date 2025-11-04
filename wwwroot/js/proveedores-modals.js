/**
 * Gestión de Modales de Proveedores
 * Puerto 92 - Sistema de Gestión
 * CON CATEGORÍAS DINÁMICAS DESDE EL SISTEMA
 */

// Variables globales
let categoriasProveedores = [];

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

/**
 * Función de inicialización que se ejecuta cada vez que se carga la página
 */
function initProveedoresPage() {
    console.log('🔄 Inicializando página de proveedores...');
    
    // Resetear categorías
    categoriasProveedores = [];
    
    // Cargar categorías primero
    cargarCategoriasProveedores().then(() => {
        setupSearch();
        setupModalEventListeners();
        setupRUCValidation();
        console.log('✅ Página de proveedores inicializada correctamente');
    }).catch(error => {
        console.error('❌ Error al inicializar página:', error);
        showNotification('Error al cargar las categorías', 'error');
    });
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initProveedoresPage);

// Exponer función para reinicializar después de navegación SPA
window.initProveedoresPage = initProveedoresPage;

// ==========================================
// CARGA DE DATOS
// ==========================================

/**
 * Cargar TODAS las categorías del sistema (Bebidas, Cocina, Utensilios)
 */
async function cargarCategoriasProveedores() {
    console.log('📥 Cargando categorías para proveedores...');
    
    try {
        const response = await fetch('/Proveedores/GetCategorias');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        const data = await response.json();
        
        console.log('📦 Respuesta del servidor:', data);
        
        if (!Array.isArray(data)) {
            throw new Error('La respuesta no es un array');
        }
        
        categoriasProveedores = data;
        console.log(`✅ Cargadas ${categoriasProveedores.length} categorías para proveedores:`, categoriasProveedores);
        
        // Llenar selects de Crear y Editar
        llenarSelectCategoriasProveedores('createProveedorCategoria');
        llenarSelectCategoriasProveedores('editProveedorCategoria');
        
        return categoriasProveedores;

    } catch (error) {
        console.error('❌ Error al cargar categorías:', error);
        categoriasProveedores = [];
        
        // Actualizar selects con mensaje de error
        ['createProveedorCategoria', 'editProveedorCategoria'].forEach(selectId => {
            const select = document.getElementById(selectId);
            if (select) {
                select.innerHTML = '<option value="">Error al cargar categorías</option>';
            }
        });
        
        throw error;
    }
}

/**
 * Llenar select de categorías con agrupación por tipo
 */
function llenarSelectCategoriasProveedores(selectId) {
    const select = document.getElementById(selectId);
    
    if (!select) {
        console.warn(`⚠️ Select ${selectId} no encontrado en el DOM`);
        return;
    }
    
    console.log(`📝 Llenando select ${selectId} con ${categoriasProveedores.length} categorías`);
    
    // Limpiar todas las opciones
    select.innerHTML = '';
    
    // Si no hay categorías
    if (categoriasProveedores.length === 0) {
        const option = document.createElement('option');
        option.value = '';
        option.textContent = 'No hay categorías disponibles';
        option.disabled = true;
        option.selected = true;
        select.appendChild(option);
        console.warn('⚠️ No hay categorías para mostrar');
        return;
    }
    
    // Agregar opción placeholder
    const placeholder = document.createElement('option');
    placeholder.value = '';
    placeholder.textContent = 'Seleccione una categoría...';
    placeholder.disabled = true;
    placeholder.selected = true;
    select.appendChild(placeholder);
    
    // Agrupar categorías por tipo
    const categoriasPorTipo = {
        'Bebidas': [],
        'Cocina': [],
        'Utensilios': []
    };
    
    categoriasProveedores.forEach(cat => {
        if (categoriasPorTipo[cat.tipo]) {
            categoriasPorTipo[cat.tipo].push(cat);
        }
    });
    
    // Crear optgroups
    ['Bebidas', 'Cocina', 'Utensilios'].forEach(tipo => {
        const categoriasTipo = categoriasPorTipo[tipo];
        
        if (categoriasTipo.length > 0) {
            const optgroup = document.createElement('optgroup');
            optgroup.label = tipo;
            
            categoriasTipo.forEach(cat => {
                const option = document.createElement('option');
                option.value = cat.value; // Nombre de la categoría
                option.textContent = cat.text; // Nombre de la categoría
                optgroup.appendChild(option);
                console.log(`  ✓ ${tipo} > ${cat.text}`);
            });
            
            select.appendChild(optgroup);
        }
    });
    
    console.log(`✅ Select ${selectId} llenado correctamente con ${categoriasProveedores.length} categorías agrupadas por tipo`);
}

// ==========================================
// VALIDACIÓN DE RUC
// ==========================================

/**
 * Configurar validación de RUC en tiempo real
 */
function setupRUCValidation() {
    const rucInput = document.getElementById('createRUC');
    
    if (!rucInput) {
        console.warn('⚠️ Input de RUC no encontrado');
        return;
    }

    console.log('🔍 Configurando validación de RUC...');

    // Validar formato mientras escribe
    rucInput.addEventListener('input', function() {
        // Solo permitir números
        this.value = this.value.replace(/[^\d]/g, '');
        
        // Limitar a 11 dígitos
        if (this.value.length > 11) {
            this.value = this.value.substring(0, 11);
        }

        // Validar formato visualmente
        if (this.value.length === 11) {
            this.style.borderColor = '#10B981';
            this.style.background = 'rgba(16, 185, 129, 0.05)';
        } else if (this.value.length > 0) {
            this.style.borderColor = '#F59E0B';
            this.style.background = 'rgba(245, 158, 11, 0.05)';
        } else {
            this.style.borderColor = '';
            this.style.background = '';
        }
    });

    // Validar duplicado al salir del campo
    rucInput.addEventListener('blur', async function() {
        const ruc = this.value.trim();
        
        if (ruc.length === 11) {
            console.log(`🔍 Validando RUC duplicado: ${ruc}`);
            await validarRUCDuplicado(ruc);
        }
    });
    
    console.log('✅ Validación de RUC configurada');
}

/**
 * Validar si el RUC ya existe en el sistema
 */
async function validarRUCDuplicado(ruc) {
    try {
        const response = await fetch(`/Proveedores/BuscarPorRUC?ruc=${ruc}`);
        const result = await response.json();
        
        if (result.success) {
            // RUC ya existe
            showNotification(`El RUC ${ruc} ya está registrado para: ${result.data.nombre}`, 'warning');
            
            const rucInput = document.getElementById('createRUC');
            if (rucInput) {
                rucInput.style.borderColor = '#EF4444';
                rucInput.style.background = 'rgba(239, 68, 68, 0.05)';
            }
            
            return false;
        }
        
        return true;
        
    } catch (error) {
        console.error('❌ Error al validar RUC:', error);
        return true; // Permitir continuar en caso de error de red
    }
}

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de crear proveedor
 */
async function openCreateProveedorModal() {
    console.log('📝 Abriendo modal de crear proveedor...');
    
    try {
        // Asegurar que las categorías estén cargadas
        if (categoriasProveedores.length === 0) {
            console.log('⚠️ Categorías no cargadas, recargando...');
            showNotification('Cargando categorías...', 'info');
            await cargarCategoriasProveedores();
        }
        
        // Verificar nuevamente después de cargar
        if (categoriasProveedores.length === 0) {
            showNotification('No hay categorías disponibles. Por favor, cree categorías desde Configuración Global.', 'warning');
            return;
        }
        
        const modal = document.getElementById('createProveedorModal');
        if (!modal) {
            console.error('❌ Modal createProveedorModal no encontrado');
            return;
        }
        
        // Resetear formulario
        const form = document.getElementById('createProveedorForm');
        if (form) {
            form.reset();
        }
        
        // Resetear estilos del RUC
        const rucInput = document.getElementById('createRUC');
        if (rucInput) {
            rucInput.style.borderColor = '';
            rucInput.style.background = '';
        }
        
        // Re-llenar select de categorías
        llenarSelectCategoriasProveedores('createProveedorCategoria');
        
        // Mostrar modal
        modal.style.display = 'flex';
        setTimeout(() => {
            modal.classList.add('active');
        }, 10);
        
        console.log('✅ Modal de crear proveedor abierto');
        
    } catch (error) {
        console.error('❌ Error al abrir modal:', error);
        showNotification('Error al abrir el modal de crear proveedor', 'error');
    }
}

/**
 * Abrir modal de editar proveedor
 */
async function openEditProveedorModal(id) {
    console.log(`✏️ Abriendo modal de editar proveedor: ${id}`);
    
    try {
        // Asegurar que las categorías estén cargadas
        if (categoriasProveedores.length === 0) {
            console.log('⚠️ Categorías no cargadas, recargando...');
            await cargarCategoriasProveedores();
        }
        
        const response = await fetch(`/Proveedores/GetProveedor?id=${id}`);
        
        if (!response.ok) {
            throw new Error('Proveedor no encontrado');
        }

        const proveedor = await response.json();
        console.log('📦 Proveedor cargado:', proveedor);

        // Llenar formulario
        document.getElementById('editProveedorId').value = proveedor.id;
        document.getElementById('editProveedorRUCDisplay').textContent = proveedor.ruc;
        document.getElementById('editProveedorRUCInput').value = proveedor.ruc;
        document.getElementById('editProveedorNombre').value = proveedor.nombre;
        document.getElementById('editProveedorTelefono').value = proveedor.telefono;
        document.getElementById('editProveedorEmail').value = proveedor.email || '';
        document.getElementById('editProveedorPersonaContacto').value = proveedor.personaContacto || '';
        document.getElementById('editProveedorDireccion').value = proveedor.direccion || '';

        // Re-llenar select de categorías y seleccionar la actual
        llenarSelectCategoriasProveedores('editProveedorCategoria');
        document.getElementById('editProveedorCategoria').value = proveedor.categoria;

        // Configurar acción del formulario
        document.getElementById('editProveedorForm').action = `/Proveedores/Edit/${id}`;
        
        // Mostrar modal
        const modal = document.getElementById('editProveedorModal');
        modal.style.display = 'flex';
        setTimeout(() => {
            modal.classList.add('active');
        }, 10);
        
        console.log('✅ Modal de editar proveedor abierto');

    } catch (error) {
        console.error('❌ Error al cargar proveedor:', error);
        showNotification('Error al cargar la información del proveedor', 'error');
    }
}

/**
 * Abrir modal de desactivar proveedor
 */
function openDesactivarProveedorModal(id, ruc, nombre, categoria) {
    console.log(`🗑️ Abriendo modal de desactivar proveedor: ${id}`);
    
    document.getElementById('desactivarProveedorId').value = id;
    document.getElementById('desactivarProveedorRUC').textContent = ruc;
    document.getElementById('desactivarProveedorNombre').textContent = nombre;
    document.getElementById('desactivarProveedorCategoria').textContent = categoria;

    document.getElementById('desactivarProveedorForm').action = `/Proveedores/Desactivar/${id}`;

    const modal = document.getElementById('desactivarProveedorModal');
    modal.style.display = 'flex';
    setTimeout(() => {
        modal.classList.add('active');
    }, 10);
    
    console.log('✅ Modal de desactivar proveedor abierto');
}

/**
 * Cerrar modal
 */
function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;
    
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

    console.log('🔍 Configurando búsqueda de proveedores...');

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
    
    console.log('✅ Búsqueda de proveedores configurada correctamente');
}

// ==========================================
// UTILIDADES
// ==========================================

/**
 * Mostrar notificación
 */
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
// AUTOCOMPLETADO DE RUC PARA PEDIDOS
// ==========================================

/**
 * Buscar proveedor por RUC y autocompletar campos
 * Esta función puede ser llamada desde el módulo de pedidos
 */
async function buscarProveedorPorRUC(ruc, onSuccess, onError) {
    if (!ruc || ruc.length !== 11) {
        if (onError) onError('RUC inválido');
        return;
    }

    console.log(`🔍 Buscando proveedor por RUC: ${ruc}`);
    showLoading();

    try {
        const response = await fetch(`/Proveedores/BuscarPorRUC?ruc=${ruc}`);
        const result = await response.json();

        if (result.success) {
            console.log('✅ Proveedor encontrado:', result.data);
            if (onSuccess) onSuccess(result.data);
        } else {
            console.log('⚠️ Proveedor no encontrado');
            if (onError) onError(result.message || 'RUC no encontrado');
        }
    } catch (error) {
        console.error('❌ Error al buscar proveedor:', error);
        if (onError) onError('Error al buscar proveedor');
    } finally {
        hideLoading();
    }
}

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

window.openCreateProveedorModal = openCreateProveedorModal;
window.openEditProveedorModal = openEditProveedorModal;
window.openDesactivarProveedorModal = openDesactivarProveedorModal;
window.closeModal = closeModal;
window.buscarProveedorPorRUC = buscarProveedorPorRUC;
window.cargarCategoriasProveedores = cargarCategoriasProveedores;