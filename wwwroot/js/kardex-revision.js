/**
 * Kardex Revisión - Puerto 92
 * Manejo de revisión y aprobación de kardex
 */

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', function() {
    console.log('🔄 Inicializando Kardex Revisión...');
    
    inicializarBusqueda();
    inicializarFormularios();
    
    console.log('✅ Kardex Revisión inicializado');
});

// ==========================================
// BÚSQUEDA DE PRODUCTOS
// ==========================================

function inicializarBusqueda() {
    const searchInput = document.getElementById('searchProducto');
    if (!searchInput) return;
    
    searchInput.addEventListener('input', function(e) {
        const searchTerm = e.target.value.toLowerCase().trim();
        buscarProductos(searchTerm);
    });
}

function buscarProductos(term) {
    const filas = document.querySelectorAll('.kardex-revision-table tbody tr');
    let encontrados = 0;
    
    filas.forEach(fila => {
        const producto = fila.dataset.producto?.toLowerCase() || '';
        const codigo = fila.querySelector('.td-codigo')?.textContent.toLowerCase() || '';
        
        const coincide = producto.includes(term) || codigo.includes(term);
        
        fila.style.display = coincide || term === '' ? '' : 'none';
        
        if (coincide && term !== '') {
            encontrados++;
        }
    });
    
    console.log(`🔍 Búsqueda: "${term}" - ${encontrados} producto(s) encontrado(s)`);
}

// ==========================================
// CATEGORÍAS COLAPSABLES
// ==========================================

function toggleCategoria(categoriaNormalizada) {
    const categoriaBody = document.getElementById(`categoria-${categoriaNormalizada}`);
    const toggleIcon = document.getElementById(`toggle-icon-${categoriaNormalizada}`);
    
    if (!categoriaBody || !toggleIcon) return;
    
    const isExpanded = categoriaBody.classList.contains('expanded');
    
    if (isExpanded) {
        // Colapsar
        categoriaBody.classList.remove('expanded');
        toggleIcon.style.transform = 'rotate(180deg)';
    } else {
        // Expandir
        categoriaBody.classList.add('expanded');
        toggleIcon.style.transform = 'rotate(0deg)';
    }
}

// ==========================================
// MODALES
// ==========================================

function abrirModalAprobar() {
    console.log('🔓 Abriendo modal de aprobar...');
    
    const modal = document.getElementById('modalAprobar');
    
    if (!modal) {
        console.error('❌ Modal #modalAprobar no encontrado en el DOM');
        alert('Error: Modal no encontrado. Recarga la página.');
        return;
    }
    
    console.log('✅ Modal encontrado:', modal);
    
    // Mostrar modal
    modal.style.display = 'flex';
    
    // Pequeño delay para la animación
    setTimeout(() => {
        modal.classList.add('active');
    }, 10);
    
    // Prevenir scroll del body
    document.body.style.overflow = 'hidden';
    
    console.log('✅ Modal abierto correctamente');
}

function abrirModalRechazar() {
    console.log('🔓 Abriendo modal de rechazar...');
    
    const modal = document.getElementById('modalRechazar');
    
    if (!modal) {
        console.error('❌ Modal #modalRechazar no encontrado en el DOM');
        alert('Error: Modal no encontrado. Recarga la página.');
        return;
    }
    
    console.log('✅ Modal encontrado:', modal);
    
    // Mostrar modal
    modal.style.display = 'flex';
    
    // Pequeño delay para la animación
    setTimeout(() => {
        modal.classList.add('active');
    }, 10);
    
    // Prevenir scroll del body
    document.body.style.overflow = 'hidden';
    
    console.log('✅ Modal abierto correctamente');
}

function closeModal(modalId) {
    console.log('🔒 Cerrando modal:', modalId);
    
    const modal = document.getElementById(modalId);
    
    if (!modal) {
        console.warn('⚠️ Modal no encontrado:', modalId);
        return;
    }
    
    // Quitar clase active para animación
    modal.classList.remove('active');
    
    // Esperar a que termine la animación
    setTimeout(() => {
        modal.style.display = 'none';
    }, 300);
    
    // Restaurar scroll del body
    document.body.style.overflow = 'auto';
    
    console.log('✅ Modal cerrado');
}

// ==========================================
// FORMULARIOS
// ==========================================

function inicializarFormularios() {
    // Formulario de aprobación
    const formAprobar = document.getElementById('formAprobar');
    if (formAprobar) {
        formAprobar.addEventListener('submit', function(e) {
            e.preventDefault();
            aprobarKardex();
        });
    }
    
    // Formulario de rechazo
    const formRechazar = document.getElementById('formRechazar');
    if (formRechazar) {
        formRechazar.addEventListener('submit', function(e) {
            e.preventDefault();
            rechazarKardex();
        });
    }
}

/**
 * Aprobar kardex
 */
async function aprobarKardex() {
    const observaciones = document.getElementById('observacionesAprobacion')?.value || '';
    
    const btnSubmit = document.querySelector('#formAprobar button[type="submit"]');
    if (btnSubmit) {
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Aprobando...';
    }
    
    try {
        const requestData = {
            kardexId: KARDEX_IDS && KARDEX_IDS.length > 0 ? KARDEX_IDS[0] : 0,
            tipoKardex: TIPO_KARDEX || 'Cocina',
            accion: 'Aprobar',
            observacionesRevision: observaciones,
            kardexIdsConsolidados: KARDEX_IDS || []
        };
        
        console.log('📤 Aprobando kardex:', requestData);
        
        const response = await fetch('/Kardex/AprobarRechazarKardex', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify(requestData)
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        const result = await response.json();
        
        if (result.success) {
            console.log('✅ Kardex aprobado exitosamente');
            showNotification('Kardex aprobado exitosamente', 'success');
            
            setTimeout(() => {
                window.location.href = '/Kardex/PendientesDeRevision';
            }, 1500);
        } else {
            throw new Error(result.message || 'Error al aprobar');
        }
    } catch (error) {
        console.error('❌ Error al aprobar:', error);
        showNotification(error.message || 'Error al aprobar el kardex', 'error');
        
        if (btnSubmit) {
            btnSubmit.disabled = false;
            btnSubmit.innerHTML = '<i class="fa-solid fa-check"></i> Aprobar Kardex';
        }
    }
}

/**
 * Rechazar kardex
 */
async function rechazarKardex() {
    const motivo = document.getElementById('motivoRechazo')?.value?.trim();
    
    if (!motivo) {
        showNotification('Debe especificar el motivo del rechazo', 'error');
        return;
    }
    
    const btnSubmit = document.querySelector('#formRechazar button[type="submit"]');
    if (btnSubmit) {
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Rechazando...';
    }
    
    try {
        const requestData = {
            kardexId: KARDEX_IDS && KARDEX_IDS.length > 0 ? KARDEX_IDS[0] : 0,
            tipoKardex: TIPO_KARDEX || 'Cocina',
            accion: 'Rechazar',
            motivoRechazo: motivo,
            kardexIdsConsolidados: KARDEX_IDS || []
        };
        
        console.log('📤 Rechazando kardex:', requestData);
        
        const response = await fetch('/Kardex/AprobarRechazarKardex', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify(requestData)
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        const result = await response.json();
        
        if (result.success) {
            console.log('✅ Kardex rechazado exitosamente');
            showNotification('Kardex rechazado. Los cocineros recibirán una notificación.', 'success');
            
            setTimeout(() => {
                window.location.href = '/Kardex/PendientesDeRevision';
            }, 1500);
        } else {
            throw new Error(result.message || 'Error al rechazar');
        }
    } catch (error) {
        console.error('❌ Error al rechazar:', error);
        showNotification(error.message || 'Error al rechazar el kardex', 'error');
        
        if (btnSubmit) {
            btnSubmit.disabled = false;
            btnSubmit.innerHTML = '<i class="fa-solid fa-xmark"></i> Rechazar Kardex';
        }
    }
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
    }, 4000);
}

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

window.toggleCategoria = toggleCategoria;
window.abrirModalAprobar = abrirModalAprobar;
window.abrirModalRechazar = abrirModalRechazar;
window.closeModal = closeModal;
window.aprobarKardex = aprobarKardex;
window.rechazarKardex = rechazarKardex;