/**
 * Gestión de Modales de Productos
 * Puerto 92 - Sistema de Gestión
 */

// Variables globales
let categorias = [];

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

/**
 * Función de inicialización que se ejecuta cada vez que se carga la página
 */
function initProductosPage() {
    console.log('🔄 Inicializando página de productos...');
    
    categorias = [];
    
    cargarCategorias().then(() => {
        setupSearch();
        setupModalEventListeners();
        setupCreateFormHandler();
        console.log('✅ Página de productos inicializada correctamente');
    });
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initProductosPage);

// ⭐ Exponer función para reinicializar después de navegación SPA
window.initProductosPage = initProductosPage;

// ==========================================
// CARGA DE DATOS
// ==========================================

/**
 * Cargar categorías de productos desde el servidor
 */
async function cargarCategorias() {
    console.log('📥 Cargando categorías...');
    
    try {
        const response = await fetch('/Productos/GetCategorias');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        categorias = await response.json();

        console.log(`✅ Cargadas ${categorias.length} categorías`);

        // Llenar select de Crear
        await llenarSelectCategorias('createCategoriaId');

        console.log('✅ Selects llenados correctamente');

    } catch (error) {
        console.error('❌ Error al cargar categorías:', error);
        showNotification('Error al cargar las categorías. Recargue la página.', 'error');
    }
}

/**
 * Llenar select de categorías
 */
async function llenarSelectCategorias(selectId) {
    const select = document.getElementById(selectId);
    if (!select) {
        console.warn(`⚠️ Select ${selectId} no encontrado`);
        return;
    }
    
    // Limpiar opciones excepto la primera (placeholder)
    while (select.options.length > 1) {
        select.remove(1);
    }
    
    if (categorias.length === 0) {
        const option = document.createElement('option');
        option.value = '';
        option.textContent = 'No hay categorías disponibles';
        option.disabled = true;
        select.appendChild(option);
        console.warn('⚠️ No hay categorías disponibles');
        return;
    }
    
    // Agregar categorías
    categorias.forEach(cat => {
        const option = document.createElement('option');
        option.value = cat.value;
        option.textContent = cat.text;
        select.appendChild(option);
    });
    
    // Cambiar placeholder
    select.options[0].textContent = 'Seleccione una categoría';
    
    console.log(`✅ Select ${selectId} llenado con ${categorias.length} categorías`);
}

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

/**
 * Abrir modal de crear producto
 */
async function openCreateProductoModal() {
    console.log('📝 Abriendo modal de crear producto...');
    
    // Asegurar que los datos estén cargados
    if (categorias.length === 0) {
        console.log('⚠️ Categorías no cargadas, recargando...');
        await cargarCategorias();
    }
    
    const modal = document.getElementById('createProductoModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    document.getElementById('createProductoForm').reset();
    document.getElementById('guardarYAgregarOtro').checked = false;
    
    // Ocultar warning de precios
    const warningDiv = document.getElementById('precioWarning');
    if (warningDiv) {
        warningDiv.style.display = 'none';
    }
    
    // Re-llenar los selects por si acaso
    await llenarSelectCategorias('createCategoriaId');
    
    console.log('✅ Modal de crear producto abierto');
}

/**
 * Abrir modal de editar producto
 */
async function openEditProductoModal(id) {
    console.log(`✏️ Abriendo modal de editar producto: ${id}`);
    
    try {
        // Asegurar que los datos estén cargados
        if (categorias.length === 0) {
            console.log('⚠️ Categorías no cargadas, recargando...');
            await cargarCategorias();
        }
        
        const response = await fetch(`/Productos/GetProducto?id=${id}`);
        
        if (!response.ok) {
            throw new Error('Producto no encontrado');
        }

        const producto = await response.json();

        // Llenar formulario
        document.getElementById('editProductoId').value = producto.id;
        document.getElementById('editProductoCodigoDisplay').textContent = producto.codigo;
        document.getElementById('editProductoCodigoInput').value = producto.codigo;
        
        // Obtener nombre de categoría
        const categoria = categorias.find(c => c.value == producto.categoriaId);
        document.getElementById('editProductoCategoriaDisplay').textContent = categoria ? categoria.text : 'Categoría';
        document.getElementById('editProductoCategoriaInput').value = producto.categoriaId;
        
        document.getElementById('editProductoNombre').value = producto.nombre;
        document.getElementById('editProductoUnidad').value = producto.unidad;
        document.getElementById('editProductoPrecioCompra').value = producto.precioCompra;
        document.getElementById('editProductoPrecioVenta').value = producto.precioVenta;
        document.getElementById('editProductoDescripcion').value = producto.descripcion || '';

        // Configurar acción del formulario
        document.getElementById('editProductoForm').action = `/Productos/Edit/${id}`;
        
        // Ocultar warning de precios
        const warningDiv = document.getElementById('editPrecioWarning');
        if (warningDiv) {
            warningDiv.style.display = 'none';
        }
        
        // Mostrar modal
        const modal = document.getElementById('editProductoModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de editar producto abierto');

    } catch (error) {
        console.error('❌ Error al cargar producto:', error);
        showNotification('Error al cargar la información del producto', 'error');
    }
}

/**
 * Abrir modal de desactivar producto
 */
function openDesactivarProductoModal(id, codigo, nombre, categoria) {
    console.log(`🗑️ Abriendo modal de desactivar producto: ${id}`);
    
    document.getElementById('desactivarProductoId').value = id;
    document.getElementById('desactivarProductoCodigo').textContent = codigo;
    document.getElementById('desactivarProductoNombre').textContent = nombre;
    document.getElementById('desactivarProductoCategoria').textContent = categoria;

    document.getElementById('desactivarProductoForm').action = `/Productos/Desactivar/${id}`;

    const modal = document.getElementById('desactivarProductoModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    console.log('✅ Modal de desactivar producto abierto');
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
    const createForm = document.getElementById('createProductoForm');
    if (!createForm) return;

    // Remover listener anterior si existe
    createForm.onsubmit = null;
    
    createForm.addEventListener('submit', async function(e) {
        const guardarYAgregarOtro = document.getElementById('guardarYAgregarOtro');
        
        // Validar precios antes de enviar
        const precioCompra = parseFloat(document.querySelector('input[name="PrecioCompra"]')?.value || 0);
        const precioVenta = parseFloat(document.querySelector('input[name="PrecioVenta"]')?.value || 0);
        
        if (precioVenta < precioCompra) {
            e.preventDefault();
            showNotification('El precio de venta debe ser mayor o igual al precio de compra', 'error');
            return;
        }
        
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
                    showNotification('Producto agregado exitosamente. Puede agregar otro.', 'success');
                    
                    // Enfocar el campo de nombre
                    const nombreInput = this.querySelector('input[name="Nombre"]');
                    if (nombreInput) {
                        nombreInput.focus();
                    }
                } else {
                    const errorText = await response.text();
                    showNotification('Error al guardar el producto', 'error');
                }
            } catch (error) {
                console.error('Error:', error);
                showNotification('Error al guardar el producto', 'error');
            } finally {
                submitButton.disabled = false;
                submitButton.innerHTML = '<i class="fa-solid fa-plus"></i> Agregar Producto';
            }
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

    console.log('🔍 Configurando búsqueda de productos...');

    // Remover event listeners anteriores clonando el nodo
    const newSearchInput = searchInput.cloneNode(true);
    searchInput.parentNode.replaceChild(newSearchInput, searchInput);

    newSearchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#productosTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const isVisible = text.includes(searchValue);
            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount++;
        });
        
        console.log(`✅ Búsqueda: "${searchValue}" - Mostrando ${visibleCount} de ${rows.length} productos`);
    });
    
    console.log('✅ Búsqueda de productos configurada correctamente');
}

// ==========================================
// UTILIDADES
// ==========================================

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

window.openCreateProductoModal = openCreateProductoModal;
window.openEditProductoModal = openEditProductoModal;
window.openDesactivarProductoModal = openDesactivarProductoModal;
window.openCargaMasivaModal = openCargaMasivaModal;
window.closeModal = closeModal;
window.handleDrop = handleDrop;
window.mostrarNombreArchivo = mostrarNombreArchivo;