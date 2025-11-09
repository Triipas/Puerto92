/**
 * Kardex de Bebidas - Sistema de Conteo por Ubicación
 * Puerto 92
 */

// Variables globales
let autoguardadoTimeout = null;
let categorias = new Set();

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', function() {
    console.log('🍹 Inicializando Kardex de Bebidas...');
    
    inicializarEventos();
    cargarCategorias();
    inicializarBusqueda();
    validarEstadoInicial();
    
    console.log('✅ Kardex de Bebidas inicializado');
});

/**
 * Inicializar eventos de inputs
 */
function inicializarEventos() {
    // Detectar cambios en inputs de conteo
    document.querySelectorAll('.input-conteo').forEach(input => {
        input.addEventListener('input', function() {
            manejarCambioConteo(this);
        });
        
        // Validar que solo sean números
        input.addEventListener('keypress', function(e) {
            const char = String.fromCharCode(e.which);
            if (!/[0-9.]/.test(char)) {
                e.preventDefault();
            }
        });
    });
    
    // Cerrar modales con ESC
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            document.querySelectorAll('.modal-overlay.active').forEach(modal => {
                closeModal(modal.id);
            });
        }
    });
}

/**
 * Cargar categorías únicas
 */
function cargarCategorias() {
    document.querySelectorAll('.td-categoria').forEach(td => {
        categorias.add(td.textContent.trim());
    });
    
    const select = document.getElementById('filtroCategorias');
    if (select) {
        categorias.forEach(cat => {
            const option = document.createElement('option');
            option.value = cat;
            option.textContent = cat;
            select.appendChild(option);
        });
        
        select.addEventListener('change', function() {
            filtrarPorCategoria(this.value);
        });
    }
    
    console.log(`📋 ${categorias.size} categorías cargadas`);
}

/**
 * Inicializar búsqueda
 */
function inicializarBusqueda() {
    const searchInput = document.getElementById('searchProducto');
    if (!searchInput) return;
    
    searchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#kardexTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const codigo = row.querySelector('.td-codigo')?.textContent.toLowerCase() || '';
            const descripcion = row.querySelector('.td-descripcion')?.textContent.toLowerCase() || '';
            
            const isVisible = codigo.includes(searchValue) || descripcion.includes(searchValue);
            row.style.display = isVisible ? '' : 'none';
            
            if (isVisible) visibleCount++;
        });
        
        console.log(`🔍 Búsqueda: "${searchValue}" - Mostrando ${visibleCount} productos`);
    });
}

/**
 * Validar estado inicial
 */
function validarEstadoInicial() {
    const inputs = document.querySelectorAll('.input-conteo');
    
    inputs.forEach(input => {
        if (input.value && input.value !== '0') {
            input.classList.add('has-value');
        }
    });
    
    actualizarEstadoFilas();
}

// ==========================================
// MANEJO DE CAMBIOS
// ==========================================

/**
 * Manejar cambio en input de conteo
 */
function manejarCambioConteo(input) {
    const detalleId = input.dataset.detalleId;
    const campo = input.dataset.campo;
    const valor = parseFloat(input.value) || null;
    
    // Marcar input con valor
    if (valor !== null && valor > 0) {
        input.classList.add('has-value');
    } else {
        input.classList.remove('has-value');
    }
    
    // Calcular inmediatamente en el frontend
    calcularFilaLocal(input.closest('tr'));
    
    // Autoguardar después de 2 segundos
    programarAutoguardado(detalleId, campo, valor);
    
    // Actualizar progreso
    actualizarProgreso();
}

/**
 * Calcular fila localmente (sin servidor)
 */
function calcularFilaLocal(row) {
    const inputs = row.querySelectorAll('.input-conteo');
    
    let conteoTotal = 0;
    let todosCompletos = true;
    
    inputs.forEach(input => {
        const valor = parseFloat(input.value) || 0;
        conteoTotal += valor;
        
        if (!input.value) {
            todosCompletos = false;
        }
    });
    
    // Actualizar conteo final
    const tdConteoFinal = row.querySelector('.td-conteo-final');
    if (tdConteoFinal) {
        tdConteoFinal.textContent = conteoTotal.toFixed(2);
    }
    
    // Calcular ventas
    const invInicial = parseFloat(row.children[4].textContent) || 0;
    const ingresos = parseFloat(row.children[5].textContent) || 0;
    const stockEsperado = invInicial + ingresos;
    const ventas = stockEsperado - conteoTotal;
    
    const tdVentas = row.querySelector('.td-ventas');
    if (tdVentas) {
        tdVentas.textContent = ventas.toFixed(2);
    }
    
    // Validar diferencia significativa
    if (stockEsperado > 0) {
        const diferenciaPorcentual = Math.abs((ventas / stockEsperado) * 100);
        
        if (diferenciaPorcentual > 10) {
            row.classList.add('row-diferencia');
            row.title = `⚠️ Diferencia significativa detectada (${diferenciaPorcentual.toFixed(1)}%)`;
        } else {
            row.classList.remove('row-diferencia');
            row.title = '';
        }
    }
    
    // Marcar fila como completa
    if (todosCompletos) {
        row.classList.add('row-complete');
        row.classList.remove('row-incomplete');
    } else {
        row.classList.remove('row-complete');
    }
}

/**
 * Programar autoguardado
 */
function programarAutoguardado(detalleId, campo, valor) {
    // Cancelar timeout anterior
    if (autoguardadoTimeout) {
        clearTimeout(autoguardadoTimeout);
    }
    
    // Mostrar indicador de guardando
    mostrarIndicadorAutoguardado('saving');
    
    // Programar nuevo autoguardado
    autoguardadoTimeout = setTimeout(async () => {
        await ejecutarAutoguardado(detalleId, campo, valor);
    }, 2000);
}

/**
 * Ejecutar autoguardado
 */
async function ejecutarAutoguardado(detalleId, campo, valor) {
    try {
        const response = await fetch('/Kardex/AutoguardarBebidas', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                KardexId: KARDEX_ID,
                DetalleId: parseInt(detalleId),
                Campo: campo,
                Valor: valor
            })
        });
        
        if (response.ok) {
            mostrarIndicadorAutoguardado('saved');
            console.log(`✅ Autoguardado: Detalle ${detalleId}, Campo ${campo}`);
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
// FILTROS Y BÚSQUEDA
// ==========================================

/**
 * Filtrar por categoría
 */
function filtrarPorCategoria(categoria) {
    const rows = document.querySelectorAll('#kardexTable tbody tr');
    
    rows.forEach(row => {
        if (!categoria || row.dataset.categoria === categoria) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
    
    console.log(`📂 Filtrado por categoría: ${categoria || 'Todas'}`);
}

// ==========================================
// PROGRESO Y VALIDACIÓN
// ==========================================

/**
 * Actualizar barra de progreso
 */
function actualizarProgreso() {
    const rows = document.querySelectorAll('#kardexTable tbody tr:not([style*="display: none"])');
    const total = rows.length;
    let completos = 0;
    let conDiferencia = 0;
    
    rows.forEach(row => {
        if (row.classList.contains('row-complete')) {
            completos++;
        }
        if (row.classList.contains('row-diferencia')) {
            conDiferencia++;
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
    
    const statsDiferencia = document.querySelector('.stat-item:nth-child(2)');
    if (statsDiferencia) {
        if (conDiferencia > 0) {
            statsDiferencia.style.display = 'flex';
            statsDiferencia.querySelector('span').textContent = `${conDiferencia} con diferencias`;
        } else {
            statsDiferencia.style.display = 'none';
        }
    }
}

/**
 * Actualizar estado de todas las filas
 */
function actualizarEstadoFilas() {
    const rows = document.querySelectorAll('#kardexTable tbody tr');
    
    rows.forEach(row => {
        calcularFilaLocal(row);
    });
    
    actualizarProgreso();
}

/**
 * Recalcular todo desde el servidor
 */
async function recalcularTodo() {
    try {
        mostrarIndicadorAutoguardado('saving');
        
        const response = await fetch(`/Kardex/RecalcularBebidas?id=${KARDEX_ID}`);
        
        if (response.ok) {
            const result = await response.json();
            
            if (result.success) {
                // Recargar página para obtener datos actualizados
                window.location.reload();
            } else {
                showNotification('Error al recalcular', 'error');
            }
        }
    } catch (error) {
        console.error('Error al recalcular:', error);
        showNotification('Error al recalcular', 'error');
    }
}

// ==========================================
// COMPLETAR KARDEX
// ==========================================

/**
 * Abrir modal de completar - VERSIÓN MODIFICADA
 * Solo valida y pide observaciones, luego redirige a Personal Presente
 */
function openCompletarModal() {
    const validacionDiv = document.getElementById('validacionResultado');
    
    // Limpiar mensaje anterior
    if (validacionDiv) {
        validacionDiv.innerHTML = '';
    }
    
    // Actualizar subtítulo del modal
    const modalSubtitle = document.querySelector('#completarKardexModal .modal-subtitle');
    if (modalSubtitle) {
        modalSubtitle.textContent = 'Revisa los datos antes de continuar al registro de personal';
    }
    
    // Cambiar texto del botón
    const btnConfirmar = document.getElementById('btnConfirmarCompletar');
    if (btnConfirmar) {
        btnConfirmar.innerHTML = '<i class="fa-solid fa-arrow-right"></i> Siguiente: Personal Presente';
        btnConfirmar.disabled = false;
    }
    
    const modal = document.getElementById('completarKardexModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Confirmar y completar kardex
 */
async function confirmarCompletar() {
    // Validar que todos los campos estén completos
    const rows = document.querySelectorAll('#kardexTable tbody tr:not([style*="display: none"])');
    const incompletas = [];
    
    rows.forEach((row, index) => {
        const inputs = row.querySelectorAll('.input-conteo');
        let completo = true;
        
        inputs.forEach(input => {
            if (!input.value || input.value === '') {
                completo = false;
                row.classList.add('row-incomplete');
            }
        });
        
        if (!completo) {
            const descripcion = row.querySelector('.td-descripcion')?.textContent || `Fila ${index + 1}`;
            incompletas.push(descripcion);
        }
    });
    
    if (incompletas.length > 0) {
        showNotification(
            `Hay ${incompletas.length} producto(s) con campos incompletos. Por favor complete todos los campos antes de continuar.`,
            'warning'
        );
        
        // Scroll a la primera fila incompleta
        const primeraIncompleta = document.querySelector('.row-incomplete');
        if (primeraIncompleta) {
            primeraIncompleta.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        
        return;
    }
    
    // Obtener observaciones
    const observaciones = document.getElementById('observacionesGenerales')?.value || '';
    
    // Guardar observaciones en sessionStorage para pasarlas a Personal Presente
    sessionStorage.setItem('kardexObservaciones', observaciones);
    
    // Cerrar modal
    closeModal('completarKardexModal');
    
    // Redirigir a Personal Presente
    console.log('✅ Redirigiendo a Personal Presente...');
    window.location.href = `/Kardex/PersonalPresente?id=${KARDEX_ID}&tipo=${encodeURIComponent('Mozo Bebidas')}`;
}

// ==========================================
// UTILIDADES
// ==========================================

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;
    
    modal.classList.remove('active');
    setTimeout(() => {
        modal.style.display = 'none';
    }, 200);
}

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
    }, 3000);
}

// Exponer funciones globales
window.closeModal = closeModal;
window.openCompletarModal = openCompletarModal;
window.confirmarCompletar = confirmarCompletar;
window.recalcularTodo = recalcularTodo;