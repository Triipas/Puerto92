/**
 * Kardex de Salón - Sistema de Conteo de Utensilios
 * Puerto 92
 */

// Variables globales
let autoguardadoTimeout = null;
let categorias = new Set();
let hayFaltantes = false;

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', function() {
    console.log('🍽️ Inicializando Kardex de Salón...');
    
    inicializarEventos();
    cargarCategorias();
    inicializarBusqueda();
    validarEstadoInicial();
    
    console.log('✅ Kardex de Salón inicializado');
});

/**
 * Inicializar eventos de inputs
 */
function inicializarEventos() {
    // Detectar cambios en inputs de conteo
    document.querySelectorAll('.input-unidades').forEach(input => {
        input.addEventListener('input', function() {
            manejarCambioConteo(this);
        });
        
        // Validar que solo sean números enteros
        input.addEventListener('keypress', function(e) {
            const char = String.fromCharCode(e.which);
            if (!/[0-9]/.test(char)) {
                e.preventDefault();
            }
        });
        
        // Prevenir decimales en paste
        input.addEventListener('paste', function(e) {
            e.preventDefault();
            const pastedText = (e.clipboardData || window.clipboardData).getData('text');
            const numericValue = pastedText.replace(/[^0-9]/g, '');
            this.value = numericValue;
            manejarCambioConteo(this);
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
    document.querySelectorAll('tr[data-categoria]').forEach(tr => {
        const categoria = tr.dataset.categoria;
        if (categoria) {
            categorias.add(categoria);
        }
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
    const searchInput = document.getElementById('searchUtensilio');
    if (!searchInput) return;
    
    searchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#kardexTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const numero = row.querySelector('.td-numero')?.textContent.toLowerCase() || '';
            const descripcion = row.querySelector('.td-descripcion')?.textContent.toLowerCase() || '';
            
            const isVisible = numero.includes(searchValue) || descripcion.includes(searchValue);
            row.style.display = isVisible ? '' : 'none';
            
            if (isVisible) visibleCount++;
        });
        
        console.log(`🔍 Búsqueda: "${searchValue}" - Mostrando ${visibleCount} utensilios`);
    });
}

/**
 * Validar estado inicial
 */
function validarEstadoInicial() {
    const inputs = document.querySelectorAll('.input-unidades');
    
    inputs.forEach(input => {
        if (input.value && input.value !== '0') {
            input.classList.add('has-value');
        }
    });
    
    actualizarEstadoFilas();
    verificarFaltantes();
}

// ==========================================
// MANEJO DE CAMBIOS
// ==========================================

/**
 * Manejar cambio en input de conteo
 */
function manejarCambioConteo(input) {
    const detalleId = input.dataset.detalleId;
    const valor = input.value ? parseInt(input.value) : null;
    
    // Marcar input con valor
    if (valor !== null && valor >= 0) {
        input.classList.add('has-value');
    } else {
        input.classList.remove('has-value');
    }
    
    // Calcular inmediatamente en el frontend
    calcularFilaLocal(input.closest('tr'));
    
    // Autoguardar después de 2 segundos
    programarAutoguardado(detalleId, valor);
    
    // Actualizar progreso
    actualizarProgreso();
    
    // Verificar si hay faltantes
    verificarFaltantes();
}

/**
 * Calcular fila localmente (detectar faltantes)
 */
function calcularFilaLocal(row) {
    const input = row.querySelector('.input-unidades');
    const inventarioInicial = parseInt(row.dataset.inventarioInicial) || 0;
    const unidadesContadas = input.value ? parseInt(input.value) : null;
    
    // Limpiar badges anteriores
    const oldBadge = row.querySelector('.faltante-badge');
    if (oldBadge) {
        oldBadge.remove();
    }
    
    // Si no hay valor, no calcular
    if (unidadesContadas === null) {
        row.classList.remove('row-complete', 'row-faltante');
        return;
    }
    
    // Marcar como completo
    row.classList.add('row-complete');
    
    // Calcular diferencia
    const diferencia = inventarioInicial - unidadesContadas;
    
    // Si hay faltantes (diferencia positiva)
    if (diferencia > 0) {
        row.classList.add('row-faltante');
        
        // Agregar badge de faltante
        const badge = document.createElement('span');
        badge.className = 'faltante-badge';
        badge.innerHTML = `
            <i class="fa-solid fa-exclamation-triangle"></i>
            Faltan ${diferencia}
        `;
        
        const tdEditable = row.querySelector('.td-editable');
        tdEditable.appendChild(badge);
    } else {
        row.classList.remove('row-faltante');
    }
}

/**
 * Programar autoguardado
 */
function programarAutoguardado(detalleId, valor) {
    // Cancelar timeout anterior
    if (autoguardadoTimeout) {
        clearTimeout(autoguardadoTimeout);
    }
    
    // Mostrar indicador de guardando
    mostrarIndicadorAutoguardado('saving');
    
    // Programar nuevo autoguardado
    autoguardadoTimeout = setTimeout(async () => {
        await ejecutarAutoguardado(detalleId, valor);
    }, 2000);
}

/**
 * Ejecutar autoguardado
 */
async function ejecutarAutoguardado(detalleId, valor) {
    try {
        const response = await fetch('/Kardex/AutoguardarSalon', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                KardexId: KARDEX_ID,
                DetalleId: parseInt(detalleId),
                UnidadesContadas: valor
            })
        });
        
        if (response.ok) {
            mostrarIndicadorAutoguardado('saved');
            console.log(`✅ Autoguardado: Detalle ${detalleId}`);
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
    let conFaltantes = 0;
    
    rows.forEach(row => {
        if (row.classList.contains('row-complete')) {
            completos++;
        }
        if (row.classList.contains('row-faltante')) {
            conFaltantes++;
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
        progressValue.textContent = `${completos} de ${total} utensilios`;
    }
    
    // Actualizar stats
    const statsCompletos = document.querySelector('.stat-item:nth-child(1) span');
    if (statsCompletos) {
        statsCompletos.textContent = `${completos} completados`;
    }
    
    const statsFaltantes = document.querySelector('.stat-item:nth-child(2)');
    if (statsFaltantes) {
        if (conFaltantes > 0) {
            statsFaltantes.style.display = 'flex';
            statsFaltantes.querySelector('span').textContent = `${conFaltantes} con faltantes`;
        } else {
            statsFaltantes.style.display = 'none';
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
 * Verificar si hay faltantes globalmente
 */
function verificarFaltantes() {
    const rows = document.querySelectorAll('#kardexTable tbody tr');
    hayFaltantes = false;
    
    rows.forEach(row => {
        if (row.classList.contains('row-faltante')) {
            hayFaltantes = true;
        }
    });
    
    console.log(`📊 Hay faltantes: ${hayFaltantes ? 'SÍ' : 'NO'}`);
}

/**
 * Recalcular todo desde el servidor
 */
async function recalcularTodo() {
    try {
        mostrarIndicadorAutoguardado('saving');
        
        const response = await fetch(`/Kardex/RecalcularSalon?id=${KARDEX_ID}`);
        
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
        const input = row.querySelector('.input-unidades');
        
        if (!input.value || input.value === '') {
            const descripcion = row.querySelector('.td-descripcion')?.textContent || `Fila ${index + 1}`;
            incompletas.push(descripcion);
            row.classList.add('row-incomplete');
            row.style.background = '#FEE2E2';
        }
    });
    
    if (incompletas.length > 0) {
        showNotification(
            `Hay ${incompletas.length} utensilio(s) sin contar. Por favor complete todos los campos antes de continuar.`,
            'warning'
        );
        
        // Scroll a la primera fila incompleta
        const primeraIncompleta = document.querySelector('.row-incomplete');
        if (primeraIncompleta) {
            primeraIncompleta.scrollIntoView({ behavior: 'smooth', block: 'center' });
            
            // Remover estilo después de 3 segundos
            setTimeout(() => {
                rows.forEach(row => {
                    if (row.classList.contains('row-incomplete')) {
                        row.classList.remove('row-incomplete');
                        row.style.background = '';
                    }
                });
            }, 3000);
        }
        
        return;
    }
    
    // Verificar si hay faltantes
    verificarFaltantes();
    
    // Mostrar/ocultar sección de descripción de faltantes
    const seccionDescripcionFaltantes = document.getElementById('seccionDescripcionFaltantes');
    const descripcionFaltantesTextarea = document.getElementById('descripcionFaltantes');
    
    if (hayFaltantes) {
        seccionDescripcionFaltantes.style.display = 'block';
        descripcionFaltantesTextarea.required = true;
        
        // Mostrar mensaje informativo
        const validacionDiv = document.getElementById('validacionResultado');
        validacionDiv.innerHTML = `
            <div style="background: #FEF3C7; border-left: 3px solid #F59E0B; padding: 1rem; border-radius: 8px; margin-bottom: 1rem;">
                <div style="display: flex; align-items: start; gap: 0.75rem;">
                    <i class="fa-solid fa-exclamation-triangle" style="color: #F59E0B; font-size: 18px; margin-top: 2px;"></i>
                    <div>
                        <strong style="color: #92400E; font-size: 14px; display: block; margin-bottom: 0.25rem;">
                            Se detectaron utensilios faltantes
                        </strong>
                        <p style="color: #92400E; font-size: 13px; margin: 0;">
                            Debe proporcionar una descripción detallada de qué pasó con los utensilios faltantes antes de continuar.
                        </p>
                    </div>
                </div>
            </div>
        `;
    } else {
        seccionDescripcionFaltantes.style.display = 'none';
        descripcionFaltantesTextarea.required = false;
        
        // Limpiar mensaje de validación
        const validacionDiv = document.getElementById('validacionResultado');
        validacionDiv.innerHTML = '';
    }
    
    const modal = document.getElementById('completarKardexModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Confirmar y completar kardex
 */
function confirmarCompletar() {
    // Si hay faltantes, validar que se haya llenado la descripción
    if (hayFaltantes) {
        const descripcionFaltantes = document.getElementById('descripcionFaltantes').value.trim();
        
        if (!descripcionFaltantes) {
            showNotification('La descripción de faltantes es obligatoria cuando hay utensilios faltantes', 'warning');
            document.getElementById('descripcionFaltantes').focus();
            return;
        }
    }
    
    // Obtener observaciones
    const descripcionFaltantes = document.getElementById('descripcionFaltantes')?.value || '';
    const observaciones = document.getElementById('observacionesGenerales')?.value || '';
    
    // Guardar en sessionStorage para pasarlas a Personal Presente
    sessionStorage.setItem('kardexDescripcionFaltantes', descripcionFaltantes);
    sessionStorage.setItem('kardexObservaciones', observaciones);
    
    // Cerrar modal
    closeModal('completarKardexModal');
    
    // Redirigir a Personal Presente
    console.log('✅ Redirigiendo a Personal Presente...');
    window.location.href = `/Kardex/PersonalPresente?id=${KARDEX_ID}&tipo=${encodeURIComponent('Mozo Salón')}`;
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