/**
 * Kardex de Vajilla - Sistema de Conteo de Utensilios de Cocina
 * Puerto 92
 */

// Variables globales
let autoguardadoTimeout = null;
let utensiliosConFaltantes = [];

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', function() {
    console.log('🍽️ Inicializando Kardex de Vajilla...');
    
    inicializarEventos();
    inicializarBusqueda();
    validarEstadoInicial();
    
    console.log('✅ Kardex de Vajilla inicializado');
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
        
        // Validar que solo sean números enteros positivos
        input.addEventListener('keypress', function(e) {
            const char = String.fromCharCode(e.which);
            if (!/[0-9]/.test(char)) {
                e.preventDefault();
            }
        });
        
        // Prevenir valores negativos o decimales
        input.addEventListener('input', function() {
            if (this.value < 0) {
                this.value = 0;
            }
            if (this.value.includes('.')) {
                this.value = Math.floor(parseFloat(this.value));
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
 * Inicializar búsqueda
 */
function inicializarBusqueda() {
    const searchInput = document.getElementById('searchUtensilio');
    if (!searchInput) return;
    
    searchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#kardexVajillaTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const codigo = row.querySelector('.td-codigo')?.textContent.toLowerCase() || '';
            const descripcion = row.querySelector('.td-descripcion')?.textContent.toLowerCase() || '';
            const categoria = row.querySelector('.td-categoria')?.textContent.toLowerCase() || '';
            
            const isVisible = codigo.includes(searchValue) || 
                            descripcion.includes(searchValue) || 
                            categoria.includes(searchValue);
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
    const valor = input.value !== '' ? parseInt(input.value) : null;
    
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
}

/**
 * Calcular fila localmente (sin servidor)
 */
function calcularFilaLocal(row) {
    const input = row.querySelector('.input-conteo');
    const tdDiferencia = row.querySelector('.td-diferencia');
    
    if (!input || !tdDiferencia) return;
    
    const invInicial = parseInt(row.children[5].textContent) || 0;
    const unidadesContadas = input.value !== '' ? parseInt(input.value) : null;
    
    let diferencia = 0;
    let tieneFaltantes = false;
    let tieneValor = false;
    
    if (unidadesContadas !== null) {
        diferencia = unidadesContadas - invInicial;
        tieneFaltantes = diferencia < 0;
        tieneValor = true;
        
        // Actualizar badge de diferencia
        const badge = tdDiferencia.querySelector('.diferencia-badge');
        if (badge) {
            badge.textContent = (diferencia > 0 ? '+' : '') + diferencia;
            
            // Remover clases anteriores
            badge.classList.remove('exacto', 'faltante', 'sobrante');
            
            // Agregar clase según diferencia
            if (diferencia === 0) {
                badge.classList.add('exacto');
            } else if (diferencia < 0) {
                badge.classList.add('faltante');
            } else {
                badge.classList.add('sobrante');
            }
        }
    }
    
    // Actualizar estado de la fila
    if (tieneFaltantes) {
        row.classList.add('row-faltante');
    } else {
        row.classList.remove('row-faltante');
    }
    
    if (tieneValor) {
        row.classList.add('row-complete');
        row.classList.remove('row-incomplete');
    } else {
        row.classList.remove('row-complete');
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
        const response = await fetch('/Kardex/AutoguardarVajilla', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                KardexId: KARDEX_ID,
                DetalleId: parseInt(detalleId),
                Valor: valor
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
// PROGRESO Y VALIDACIÓN
// ==========================================

/**
 * Actualizar barra de progreso
 */
function actualizarProgreso() {
    const rows = document.querySelectorAll('#kardexVajillaTable tbody tr:not([style*="display: none"])');
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
    const rows = document.querySelectorAll('#kardexVajillaTable tbody tr');
    
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
        
        const response = await fetch(`/Kardex/RecalcularVajilla?id=${KARDEX_ID}`);
        
        if (response.ok) {
            const result = await response.json();
            
            if (result.success) {
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
// MODAL DE SIGUIENTE (FALTANTES)
// ==========================================

/**
 * Abrir modal apropiado según si hay faltantes
 */
function openSiguienteModal() {
    // Validar que todos los campos estén completos
    const rows = document.querySelectorAll('#kardexVajillaTable tbody tr:not([style*="display: none"])');
    const incompletas = [];
    
    utensiliosConFaltantes = [];
    
    rows.forEach((row, index) => {
        const input = row.querySelector('.input-conteo');
        
        if (!input.value || input.value === '') {
            const descripcion = row.querySelector('.td-descripcion')?.textContent || `Fila ${index + 1}`;
            incompletas.push(descripcion);
            row.classList.add('row-incomplete');
        } else {
            // Verificar si tiene faltantes
            const invInicial = parseInt(row.children[5].textContent) || 0;
            const unidadesContadas = parseInt(input.value);
            const diferencia = unidadesContadas - invInicial;
            
            if (diferencia < 0) {
                const categoria = row.querySelector('.td-categoria')?.textContent.trim() || '';
                const codigo = row.querySelector('.td-codigo')?.textContent || '';
                const descripcion = row.querySelector('.td-descripcion')?.textContent || '';
                
                utensiliosConFaltantes.push({
                    categoria: categoria,
                    codigo: codigo,
                    descripcion: descripcion,
                    faltante: Math.abs(diferencia)
                });
            }
        }
    });
    
    if (incompletas.length > 0) {
        showNotification(
            `Hay ${incompletas.length} utensilio(s) sin registrar. Por favor complete todos los campos antes de continuar.`,
            'warning'
        );
        
        // Scroll a la primera fila incompleta
        const primeraIncompleta = document.querySelector('.row-incomplete');
        if (primeraIncompleta) {
            primeraIncompleta.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        
        return;
    }
    
    // Si hay faltantes, mostrar modal de descripción
    if (utensiliosConFaltantes.length > 0) {
        mostrarModalFaltantes();
    } else {
        // Si no hay faltantes, ir directo a personal presente
        continuarAPersonalPresente();
    }
}

/**
 * Mostrar modal de faltantes
 */
function mostrarModalFaltantes() {
    const cantidadElement = document.getElementById('cantidadFaltantes');
    const listaElement = document.getElementById('listaFaltantes');
    
    if (cantidadElement) {
        cantidadElement.textContent = utensiliosConFaltantes.length;
    }
    
    if (listaElement) {
        listaElement.innerHTML = '';
        
        utensiliosConFaltantes.forEach(item => {
            const itemDiv = document.createElement('div');
            itemDiv.style.cssText = 'padding: 0.75rem; border-bottom: 1px solid #E5E7EB; display: flex; justify-content: space-between; align-items: center;';
            itemDiv.innerHTML = `
                <div style="flex: 1;">
                    <div style="font-weight: 600; color: #1F2937; margin-bottom: 0.25rem;">${item.descripcion}</div>
                    <div style="font-size: 12px; color: #6B7280;">
                        ${item.categoria} - ${item.codigo}
                    </div>
                </div>
                <div style="background: #FEE2E2; color: #DC2626; padding: 0.375rem 0.75rem; border-radius: 6px; font-weight: 600;">
                    -${item.faltante}
                </div>
            `;
            listaElement.appendChild(itemDiv);
        });
    }
    
    // Limpiar campos
    document.getElementById('cantidadRotos').value = '';
    document.getElementById('cantidadExtraviados').value = '';
    document.getElementById('descripcionFaltantesTexto').value = '';
    
    const modal = document.getElementById('descripcionFaltantesModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Continuar a personal presente
 */
function continuarAPersonalPresente() {
    const descripcionFaltantes = document.getElementById('descripcionFaltantesTexto')?.value || '';
    const cantidadRotos = parseInt(document.getElementById('cantidadRotos')?.value) || 0;
    const cantidadExtraviados = parseInt(document.getElementById('cantidadExtraviados')?.value) || 0;
    
    // Si hay faltantes y no se ingresó descripción
    if (utensiliosConFaltantes.length > 0 && !descripcionFaltantes.trim()) {
        showNotification('Por favor, ingrese la descripción detallada de qué pasó con los utensilios faltantes', 'warning');
        return;
    }
    
    // Construir descripción completa
    let descripcionCompleta = '';
    if (utensiliosConFaltantes.length > 0) {
        descripcionCompleta = `Cantidad Rotos: ${cantidadRotos}\n`;
        descripcionCompleta += `Cantidad Extraviados: ${cantidadExtraviados}\n\n`;
        descripcionCompleta += `Descripción Detallada:\n${descripcionFaltantes}`;
    }
    
    // Guardar en sessionStorage
    sessionStorage.setItem('kardexDescripcionFaltantes', descripcionCompleta);
    
    // Cerrar modal si está abierto
    closeModal('descripcionFaltantesModal');
    
    // Redirigir a Personal Presente
    console.log('✅ Redirigiendo a Personal Presente...');
    window.location.href = `/Kardex/PersonalPresente?id=${KARDEX_ID}&tipo=${encodeURIComponent('Vajilla')}`;
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
    }, 4000);
}

// Exponer funciones globales
window.closeModal = closeModal;
window.openSiguienteModal = openSiguienteModal;
window.continuarAPersonalPresente = continuarAPersonalPresente;
window.recalcularTodo = recalcularTodo;