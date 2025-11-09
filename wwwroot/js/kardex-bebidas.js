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
 * Abrir modal de completar
 */
function openCompletarModal() {
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
    
    const validacionDiv = document.getElementById('validacionResultado');
    
    if (incompletas.length > 0) {
        validacionDiv.innerHTML = `
            <div style="background: #FEF2F2; border-left: 3px solid #EF4444; padding: 1rem; border-radius: 8px; margin-bottom: 1rem;">
                <div style="display: flex; align-items: start; gap: 0.75rem;">
                    <i class="fa-solid fa-exclamation-circle" style="color: #EF4444; font-size: 20px; margin-top: 2px;"></i>
                    <div>
                        <strong style="color: #991B1B; display: block; margin-bottom: 0.5rem;">
                            Hay ${incompletas.length} producto(s) con campos incompletos
                        </strong>
                        <p style="color: #991B1B; font-size: 13px; margin: 0;">
                            Por favor, complete todos los campos de conteo antes de enviar el kardex.
                        </p>
                    </div>
                </div>
            </div>
        `;
        
        document.getElementById('btnConfirmarCompletar').disabled = true;
        
        // Scroll a la primera fila incompleta
        const primeraIncompleta = document.querySelector('.row-incomplete');
        if (primeraIncompleta) {
            primeraIncompleta.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    } else {
        validacionDiv.innerHTML = `
            <div style="background: #F0FDF4; border-left: 3px solid #10B981; padding: 1rem; border-radius: 8px; margin-bottom: 1rem;">
                <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <i class="fa-solid fa-check-circle" style="color: #10B981; font-size: 20px;"></i>
                    <div>
                        <strong style="color: #065F46; display: block;">
                            ✅ Todos los productos están completos
                        </strong>
                        <p style="color: #065F46; font-size: 13px; margin: 0;">
                            El kardex está listo para ser enviado.
                        </p>
                    </div>
                </div>
            </div>
        `;
        
        document.getElementById('btnConfirmarCompletar').disabled = false;
    }
    
    const modal = document.getElementById('completarKardexModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Confirmar y completar kardex
 */
async function confirmarCompletar() {
    const observaciones = document.getElementById('observacionesGenerales').value;
    const btnConfirmar = document.getElementById('btnConfirmarCompletar');
    
    btnConfirmar.disabled = true;
    btnConfirmar.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Enviando...';
    
    try {
        const response = await fetch('/Kardex/CompletarBebidas', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: `id=${KARDEX_ID}&observaciones=${encodeURIComponent(observaciones)}`
        });
        
        if (response.ok) {
            showNotification('Kardex completado exitosamente', 'success');
            
            setTimeout(() => {
                window.location.href = '/Kardex/MiKardex';
            }, 1500);
        } else {
            const text = await response.text();
            showNotification(text || 'Error al completar el kardex', 'error');
            btnConfirmar.disabled = false;
            btnConfirmar.innerHTML = '<i class="fa-solid fa-paper-plane"></i> Enviar Kardex';
        }
    } catch (error) {
        console.error('Error al completar:', error);
        showNotification('Error al completar el kardex', 'error');
        btnConfirmar.disabled = false;
        btnConfirmar.innerHTML = '<i class="fa-solid fa-paper-plane"></i> Enviar Kardex';
    }
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