/**
 * Kardex de Cocina - Sistema de Conteo con Categorías Colapsables
 * Puerto 92
 */

// Variables globales
let autoguardadoTimeout = null;

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', function() {
    console.log(`🍳 Inicializando Kardex de Cocina: ${TIPO_KARDEX}...`);
    
    inicializarEventos();
    inicializarBusqueda();
    validarEstadoInicial();
    
    console.log('✅ Kardex de Cocina inicializado');
});

/**
 * Inicializar eventos de inputs
 */
function inicializarEventos() {
    // Eventos de inputs de stock
    document.querySelectorAll('.input-stock').forEach(input => {
        input.addEventListener('input', function() {
            manejarCambioStock(this);
        });
        
        // Validar según unidad
        input.addEventListener('keypress', function(e) {
            validarEntradaSegunUnidad(e, this);
        });
        
        // Prevenir valores negativos
        input.addEventListener('input', function() {
            if (this.value < 0) {
                this.value = 0;
            }
        });
    });
    
    // Eventos de selects de unidad
    document.querySelectorAll('.select-unidad').forEach(select => {
        select.addEventListener('change', function() {
            manejarCambioUnidad(this);
        });
    });
    
    // Cerrar con ESC
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            document.querySelectorAll('.modal-overlay.active').forEach(modal => {
                closeModal(modal.id);
            });
        }
    });
}

/**
 * Inicializar búsqueda
 */
function inicializarBusqueda() {
    const searchInput = document.getElementById('searchProducto');
    if (!searchInput) return;
    
    searchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase().trim();
        const rows = document.querySelectorAll('.kardex-cocina-table tbody tr');
        
        let visibleCount = 0;
        let categoriasConResultados = new Set();
        
        rows.forEach(row => {
            const codigo = row.querySelector('.td-codigo')?.textContent.toLowerCase() || '';
            const nombre = row.querySelector('.td-nombre')?.textContent.toLowerCase() || '';
            const producto = row.dataset.producto?.toLowerCase() || '';
            
            const isVisible = codigo.includes(searchValue) || 
                            nombre.includes(searchValue) || 
                            producto.includes(searchValue);
            
            row.style.display = isVisible ? '' : 'none';
            
            if (isVisible) {
                visibleCount++;
                // Marcar categoría como teniendo resultados
                const categoriaCard = row.closest('.categoria-card');
                if (categoriaCard) {
                    const categoriaNombre = categoriaCard.dataset.categoria;
                    categoriasConResultados.add(categoriaNombre);
                }
            }
        });
        
        // Expandir automáticamente categorías con resultados
        if (searchValue) {
            document.querySelectorAll('.categoria-card').forEach(card => {
                const categoriaNombre = card.dataset.categoria;
                const categoriaBody = card.querySelector('.categoria-body');
                const toggleIcon = card.querySelector('.categoria-toggle i');
                
                if (categoriasConResultados.has(categoriaNombre)) {
                    categoriaBody.classList.add('expanded');
                    if (toggleIcon) toggleIcon.classList.remove('fa-chevron-down');
                    if (toggleIcon) toggleIcon.classList.add('fa-chevron-up');
                } else {
                    categoriaBody.classList.remove('expanded');
                    if (toggleIcon) toggleIcon.classList.remove('fa-chevron-up');
                    if (toggleIcon) toggleIcon.classList.add('fa-chevron-down');
                }
            });
        }
        
        console.log(`🔍 Búsqueda: "${searchValue}" - Mostrando ${visibleCount} productos en ${categoriasConResultados.size} categoría(s)`);
    });
}

/**
 * Validar estado inicial
 */
function validarEstadoInicial() {
    const inputs = document.querySelectorAll('.input-stock');
    
    inputs.forEach(input => {
        if (input.value && input.value !== '0') {
            input.classList.add('has-value');
            input.closest('tr')?.classList.add('row-complete');
        }
    });
    
    actualizarProgreso();
}

// ==========================================
// CATEGORÍAS COLAPSABLES
// ==========================================

/**
 * Toggle de categoría
 */
function toggleCategoria(categoriaId) {
    const categoriaBody = document.getElementById(`categoria-${categoriaId}`);
    const toggleIcon = document.getElementById(`toggle-icon-${categoriaId}`);
    
    if (!categoriaBody || !toggleIcon) return;
    
    const isExpanded = categoriaBody.classList.contains('expanded');
    
    if (isExpanded) {
        // Colapsar
        categoriaBody.classList.remove('expanded');
        toggleIcon.classList.remove('fa-chevron-up');
        toggleIcon.classList.add('fa-chevron-down');
    } else {
        // Expandir
        categoriaBody.classList.add('expanded');
        toggleIcon.classList.remove('fa-chevron-down');
        toggleIcon.classList.add('fa-chevron-up');
    }
    
    console.log(`📦 Categoría ${categoriaId}: ${isExpanded ? 'Colapsada' : 'Expandida'}`);
}

// ==========================================
// MANEJO DE CAMBIOS
// ==========================================

/**
 * Manejar cambio en stock final
 */
function manejarCambioStock(input) {
    const detalleId = input.dataset.detalleId;
    const valor = input.value !== '' ? parseFloat(input.value) : null;
    
    // Marcar input con valor
    if (valor !== null && valor >= 0) {
        input.classList.add('has-value');
        input.closest('tr')?.classList.add('row-complete');
    } else {
        input.classList.remove('has-value');
        input.closest('tr')?.classList.remove('row-complete');
    }
    
    // Autoguardar después de 2 segundos
    programarAutoguardado(detalleId, 'StockFinal', valor, null);
    
    // Actualizar progreso
    actualizarProgreso();
}

/**
 * Manejar cambio en unidad de medida
 */
function manejarCambioUnidad(select) {
    const detalleId = select.dataset.detalleId;
    const unidad = select.value;
    
    // Actualizar validación del input correspondiente
    const row = select.closest('tr');
    const input = row?.querySelector('.input-stock');
    
    if (input) {
        const permiteDecimales = (unidad === 'KG' || unidad === 'L');
        select.dataset.permiteDecimales = permiteDecimales ? 'true' : 'false';
        input.step = permiteDecimales ? '0.01' : '1';
        
        // Redondear valor existente si cambió a unidad entera
        if (!permiteDecimales && input.value) {
            const valorActual = parseFloat(input.value);
            input.value = Math.floor(valorActual);
        }
    }
    
    // Autoguardar
    programarAutoguardado(detalleId, 'UnidadMedida', null, unidad);
    
    console.log(`📏 Unidad cambiada: ${unidad} (Detalle: ${detalleId})`);
}

/**
 * Validar entrada según unidad de medida
 */
function validarEntradaSegunUnidad(event, input) {
    const row = input.closest('tr');
    const select = row?.querySelector('.select-unidad');
    const permiteDecimales = select?.dataset.permiteDecimales === 'true';
    
    const char = String.fromCharCode(event.which);
    
    if (permiteDecimales) {
        // Permitir números y punto decimal
        if (!/[0-9.]/.test(char)) {
            event.preventDefault();
        }
        // Solo un punto decimal
        if (char === '.' && input.value.includes('.')) {
            event.preventDefault();
        }
    } else {
        // Solo números enteros
        if (!/[0-9]/.test(char)) {
            event.preventDefault();
        }
    }
}

/**
 * Programar autoguardado
 */
function programarAutoguardado(detalleId, campo, valorNumerico, valorTexto) {
    // Cancelar timeout anterior
    if (autoguardadoTimeout) {
        clearTimeout(autoguardadoTimeout);
    }
    
    // Mostrar indicador de guardando
    mostrarIndicadorAutoguardado('saving');
    
    // Programar nuevo autoguardado
    autoguardadoTimeout = setTimeout(async () => {
        await ejecutarAutoguardado(detalleId, campo, valorNumerico, valorTexto);
    }, 2000);
}

/**
 * Ejecutar autoguardado
 */
async function ejecutarAutoguardado(detalleId, campo, valorNumerico, valorTexto) {
    try {
        const response = await fetch('/Kardex/AutoguardarCocina', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                KardexId: KARDEX_ID,
                DetalleId: parseInt(detalleId),
                Campo: campo,
                ValorNumerico: valorNumerico,
                ValorTexto: valorTexto
            })
        });
        
        if (response.ok) {
            mostrarIndicadorAutoguardado('saved');
            console.log(`✅ Autoguardado: Detalle ${detalleId}, Campo: ${campo}`);
        } else {
            console.error('❌ Error en autoguardado');
            mostrarIndicadorAutoguardado('error');
        }
    } catch (error) {
        console.error('❌ Error en autoguardado:', error);
        mostrarIndicadorAutoguardado('error');
    }
}

/**
 * Mostrar indicador de autoguardado
 */
function mostrarIndicadorAutoguardado(estado) {
    const indicador = document.getElementById('autoguardadoIndicador');
    if (!indicador) return;
    
    indicador.classList.remove('saving', 'show');
    
    if (estado === 'saving') {
        indicador.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Guardando...';
        indicador.classList.add('saving', 'show');
    } else if (estado === 'saved') {
        indicador.innerHTML = '<i class="fa-solid fa-circle-check"></i> Guardado';
        indicador.classList.add('show');
        
        setTimeout(() => {
            indicador.classList.remove('show');
        }, 3000);
    } else if (estado === 'error') {
        indicador.innerHTML = '<i class="fa-solid fa-circle-xmark"></i> Error al guardar';
        indicador.style.background = '#FEE2E2';
        indicador.style.color = '#DC2626';
        indicador.classList.add('show');
        
        setTimeout(() => {
            indicador.classList.remove('show');
            indicador.style.background = '';
            indicador.style.color = '';
        }, 5000);
    }
}

// ==========================================
// PROGRESO Y VALIDACIÓN
// ==========================================

/**
 * Actualizar barra de progreso
 */
function actualizarProgreso() {
    const rows = document.querySelectorAll('.kardex-cocina-table tbody tr:not([style*="display: none"])');
    const total = rows.length;
    let completos = 0;
    
    rows.forEach(row => {
        if (row.classList.contains('row-complete')) {
            completos++;
        }
    });
    
    const porcentaje = total > 0 ? (completos / total * 100) : 0;
    
    // Actualizar barra
    const progressFill = document.querySelector('.progress-fill');
    if (progressFill) {
        progressFill.style.width = `${porcentaje}%`;
    }
    
    // Actualizar texto
    const progressValue = document.querySelector('.progress-value');
    if (progressValue) {
        progressValue.textContent = `${completos} de ${total} productos`;
    }
    
    // Actualizar stats
    const statsCompletos = document.querySelector('.stat-item:nth-child(1) span');
    if (statsCompletos) {
        statsCompletos.textContent = `${completos} completados`;
    }
    
    // Actualizar contadores de categorías
    actualizarContadoresCategorias();
}

/**
 * Actualizar contadores de cada categoría
 */
function actualizarContadoresCategorias() {
    document.querySelectorAll('.categoria-card').forEach(card => {
        const rows = card.querySelectorAll('.kardex-cocina-table tbody tr');
        const total = rows.length;
        const completos = Array.from(rows).filter(r => r.classList.contains('row-complete')).length;
        
        const subtitulo = card.querySelector('.categoria-subtitulo');
        if (subtitulo) {
            subtitulo.textContent = `${completos} de ${total} productos completados`;
        }
    });
}

/**
 * Continuar a Personal Presente
 */
function continuarAPersonalPresente() {
    // Validar que todos los campos estén completos
    const rows = document.querySelectorAll('.kardex-cocina-table tbody tr:not([style*="display: none"])');
    const incompletas = [];
    
    rows.forEach((row, index) => {
        const input = row.querySelector('.input-stock');
        
        if (!input.value || input.value === '') {
            const nombre = row.querySelector('.td-nombre')?.textContent || `Producto ${index + 1}`;
            incompletas.push(nombre);
            row.classList.add('row-incomplete');
        }
    });
    
    if (incompletas.length > 0) {
        showNotification(
            `Hay ${incompletas.length} producto(s) sin registrar. Por favor complete todos los campos antes de continuar.`,
            'warning'
        );
        
        // Scroll a la primera fila incompleta
        const primeraIncompleta = document.querySelector('.row-incomplete');
        if (primeraIncompleta) {
            primeraIncompleta.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        
        return;
    }
    
    // ⭐ ASEGURAR QUE LAS VARIABLES ESTÉN DEFINIDAS
    if (typeof KARDEX_ID === 'undefined' || typeof TIPO_KARDEX === 'undefined') {
        showNotification('Error: Variables no definidas. Recargue la página.', 'error');
        return;
    }
    
    // Redirigir a Personal Presente (URL encode el tipo para manejar caracteres especiales)
    console.log('✅ Redirigiendo a Personal Presente...');
    console.log(`   KARDEX_ID: ${KARDEX_ID}`);
    console.log(`   TIPO_KARDEX: ${TIPO_KARDEX}`);
    
    window.location.href = `/Kardex/PersonalPresente?id=${KARDEX_ID}&tipo=${encodeURIComponent(TIPO_KARDEX)}`;
}

// ==========================================
// UTILIDADES
// ==========================================

function showNotification(message, type = 'info') {
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

// Exponer funciones globales
window.toggleCategoria = toggleCategoria;
window.continuarAPersonalPresente = continuarAPersonalPresente;