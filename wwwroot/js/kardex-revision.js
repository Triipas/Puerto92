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
    const modal = document.getElementById('modalAprobar');
    if (modal) {
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }
}

function abrirModalRechazar() {
    const modal = document.getElementById('modalRechazar');
    if (modal) {
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
        document.body.style.overflow = 'auto';
    }
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
    
    // ⭐ DEBUG: Verificar valores antes de enviar
    console.log('🔍 DEBUG - Antes de aprobar:');
    console.log('   KARDEX_IDS:', KARDEX_IDS);
    console.log('   KARDEX_IDS.length:', KARDEX_IDS.length);
    console.log('   TIPO_KARDEX:', TIPO_KARDEX);
    
    const btnSubmit = document.querySelector('#formAprobar button[type="submit"]');
    if (btnSubmit) {
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Aprobando...';
    }
    
    try {
        // ⭐ DETERMINAR SI ES KARDEX CONSOLIDADO O INDIVIDUAL
        const esConsolidado = KARDEX_IDS && KARDEX_IDS.length > 1;
        
        console.log('   Es consolidado:', esConsolidado);
        
        let requestData;
        
        if (esConsolidado) {
            // ✅ KARDEX CONSOLIDADO DE COCINA (3 kardex)
            console.log('🍳 Aprobando kardex consolidado de cocina:', KARDEX_IDS);
            
            requestData = {
                kardexId: KARDEX_IDS[0],
                tipoKardex: 'Cocina',
                accion: 'Aprobar',
                observacionesRevision: observaciones,
                kardexIdsConsolidados: KARDEX_IDS
            };
        } else {
            // ✅ KARDEX INDIVIDUAL
            console.log('📋 Aprobando kardex individual:', KARDEX_IDS[0]);
            
            requestData = {
                kardexId: KARDEX_IDS && KARDEX_IDS.length > 0 ? KARDEX_IDS[0] : 0,
                tipoKardex: TIPO_KARDEX || 'Individual',
                accion: 'Aprobar',
                observacionesRevision: observaciones
            };
        }
        
        console.log('📤 Request completo:', JSON.stringify(requestData, null, 2));
        
        const response = await fetch('/Kardex/AprobarRechazarKardex', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify(requestData)
        });
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('❌ Error HTTP:', response.status, errorText);
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }
        
        const result = await response.json();
        
        if (result.success) {
            console.log('✅ Kardex aprobado exitosamente');
            
            if (esConsolidado) {
                showNotification(`Los ${KARDEX_IDS.length} kardex de cocina han sido aprobados exitosamente`, 'success');
            } else {
                showNotification('Kardex aprobado exitosamente', 'success');
            }
            
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
        // ⭐ DETERMINAR SI ES KARDEX CONSOLIDADO O INDIVIDUAL
        const esConsolidado = KARDEX_IDS && KARDEX_IDS.length > 1;
        
        let requestData;
        
        if (esConsolidado) {
            // ✅ KARDEX CONSOLIDADO DE COCINA (3 kardex)
            console.log('🍳 Rechazando kardex consolidado de cocina:', KARDEX_IDS);
            
            requestData = {
                kardexId: KARDEX_IDS[0], // El primero como referencia
                tipoKardex: 'Cocina',
                accion: 'Rechazar',
                motivoRechazo: motivo,
                kardexIdsConsolidados: KARDEX_IDS // ⭐ PASAR LOS 3 IDS
            };
        } else {
            // ✅ KARDEX INDIVIDUAL
            console.log('📋 Rechazando kardex individual:', KARDEX_IDS[0]);
            
            requestData = {
                kardexId: KARDEX_IDS && KARDEX_IDS.length > 0 ? KARDEX_IDS[0] : 0,
                tipoKardex: TIPO_KARDEX || 'Individual',
                accion: 'Rechazar',
                motivoRechazo: motivo
            };
        }
        
        console.log('📤 Request de rechazo:', requestData);
        
        const response = await fetch('/Kardex/AprobarRechazarKardex', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify(requestData)
        });
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('❌ Error HTTP:', response.status, errorText);
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }
        
        const result = await response.json();
        
        if (result.success) {
            console.log('✅ Kardex rechazado exitosamente');
            
            if (esConsolidado) {
                showNotification(`Los ${KARDEX_IDS.length} kardex de cocina han sido rechazados. Los cocineros recibirán una notificación.`, 'success');
            } else {
                showNotification('Kardex rechazado. El empleado recibirá una notificación.', 'success');
            }
            
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