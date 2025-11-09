/**
 * Kardex de Salón - Sistema de Conteo de Utensilios
 * Puerto 92
 */

// Variables globales
let autoguardadoTimeout = null;
let categorias = new Set();
let detallesPendientesJustificacion = new Set();

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
    // Detectar cambios en inputs de unidades
    document.querySelectorAll('.input-unidades').forEach(input => {
        input.addEventListener('input', function() {
            manejarCambioUnidades(this);
        });
        
        // Validar que solo sean números enteros
        input.addEventListener('keypress', function(e) {
            const char = String.fromCharCode(e.which);
            // Solo permitir números (sin punto ni decimales)
            if (!/[0-9]/.test(char)) {
                e.preventDefault();
            }
        });
        
        // Prevenir decimales al pegar
        input.addEventListener('paste', function(e) {
            e.preventDefault();
            const pastedText = (e.clipboardData || window.clipboardData).getData('text');
            const numero = parseInt(pastedText);
            
            if (!isNaN(numero) && numero >= 0) {
                this.value = numero;
                manejarCambioUnidades(this);
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
    const searchInput = document.getElementById('searchUtensilio');
    if (!searchInput) return;
    
    searchInput.addEventListener('keyup', function() {
        const searchValue = this.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#kardexTable tbody tr');
        
        let visibleCount = 0;
        
        rows.forEach(row => {
            const codigo = row.querySelector('.td-codigo')?.textContent.toLowerCase() || '';
            const nombre = row.querySelector('.td-nombre')?.textContent.toLowerCase() || '';
            
            const isVisible = codigo.includes(searchValue) || nombre.includes(searchValue);
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
}

// ==========================================
// MANEJO DE CAMBIOS
// ==========================================

/**
 * Manejar cambio en input de unidades
 */
function manejarCambioUnidades(input) {
    const detalleId = input.dataset.detalleId;
    const valor = input.value === '' ? null : parseInt(input.value);
    
    // Validar número entero positivo
    if (valor !== null && (isNaN(valor) || valor < 0)) {
        input.value = '';
        showNotification('Solo se permiten números enteros positivos', 'warning');
        return;
    }
    
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
 * Calcular fila localmente
 */
function calcularFilaLocal(row) {
    const input = row.querySelector('.input-unidades');
    const tdDiferencia = row.querySelector('.td-diferencia');
    const invInicial = parseInt(row.children[5].textContent) || 0;
    
    if (!input.value || input.value === '') {
        row.classList.remove('row-complete', 'row-faltantes');
        if (tdDiferencia) {
            tdDiferencia.textContent = '0';
            tdDiferencia.dataset.tieneFaltantes = 'false';
            
            // Remover botón de justificar si existe
            const btnJustificar = tdDiferencia.querySelector('.btn-justificar');
            if (btnJustificar) {
                btnJustificar.remove();
            }
        }
        return;
    }
    
    const unidadesContadas = parseInt(input.value);
    const diferencia = invInicial - unidadesContadas;
    
    // Actualizar diferencia
    if (tdDiferencia) {
        const btnJustificar = tdDiferencia.querySelector('.btn-justificar');
        
        if (diferencia > 0) {
            // Tiene faltantes
            tdDiferencia.dataset.tieneFaltantes = 'true';
            tdDiferencia.textContent = diferencia.toString();
            
            // Agregar botón de justificar si no existe
            if (!btnJustificar) {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'btn-justificar';
                btn.title = 'Justificar faltantes';
                btn.innerHTML = '<i class="fa-solid fa-file-lines"></i>';
                
                const detalleId = input.dataset.detalleId;
                const nombre = row.querySelector('.td-nombre').textContent;
                btn.onclick = function() {
                    openFaltantesModal(detalleId, nombre, diferencia);
                };
                
                tdDiferencia.appendChild(btn);
            }
            
            row.classList.add('row-faltantes');
            row.classList.remove('row-complete');
            
            // Agregar a pendientes de justificación
            detallesPendientesJustificacion.add(parseInt(input.dataset.detalleId));
        } else {
            // Sin faltantes
            tdDiferencia.dataset.tieneFaltantes = 'false';
            tdDiferencia.textContent = '0';
            
            // Remover botón de justificar
            if (btnJustificar) {
                btnJustificar.remove();
            }
            
            row.classList.remove('row-faltantes');
            row.classList.add('row-complete');
            
            // Remover de pendientes de justificación
            detallesPendientesJustificacion.delete(parseInt(input.dataset.detalleId));
        }
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
            console.log(`✅ Autoguardado: Detalle ${detalleId}, Unidades ${valor}`);
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
// MODAL DE FALTANTES
// ==========================================

/**
 * Abrir modal de descripción de faltantes
 */
function openFaltantesModal(detalleId, nombreUtensilio, cantidad) {
    console.log(`📝 Abriendo modal de faltantes: ${detalleId}`);
    
    document.getElementById('faltantesDetalleId').value = detalleId;
    document.getElementById('faltantesSubtitulo').textContent = `${nombreUtensilio}`;
    document.getElementById('faltantesCantidad').textContent = cantidad;
    document.getElementById('faltantesDescripcion').value = '';
    
    const modal = document.getElementById('faltantesModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    // Focus en el textarea
    setTimeout(() => {
        document.getElementById('faltantesDescripcion').focus();
    }, 300);
}

/**
 * Guardar descripción de faltantes
 */
async function guardarDescripcionFaltantes() {
    const detalleId = document.getElementById('faltantesDetalleId').value;
    const descripcion = document.getElementById('faltantesDescripcion').value.trim();
    
    if (!descripcion) {
        showNotification('Debe especificar qué pasó con los faltantes', 'warning');
        return;
    }
    
    const btnGuardar = document.getElementById('btnGuardarFaltantes');
    btnGuardar.disabled = true;
    btnGuardar.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Guardando...';
    
    try {
        const response = await fetch('/Kardex/GuardarDescripcionFaltantes', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                KardexId: KARDEX_ID,
                DetalleId: parseInt(detalleId),
                DescripcionFaltantes: descripcion
            })
        });
        
        if (response.ok) {
            // Remover de pendientes
            detallesPendientesJustificacion.delete(parseInt(detalleId));
            
            closeModal('faltantesModal');
            showNotification('Descripción de faltantes guardada', 'success');
            
            console.log(`✅ Faltantes justificados: Detalle ${detalleId}`);
        } else {
            showNotification('Error al guardar la descripción', 'error');
        }
    } catch (error) {
        console.error('❌ Error al guardar faltantes:', error);
        showNotification('Error al guardar la descripción', 'error');
    } finally {
        btnGuardar.disabled = false;
        btnGuardar.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Guardar';
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
        if (row.classList.contains('row-complete') || row.classList.contains('row-faltantes')) {
            completos++;
        }
        if (row.classList.contains('row-faltantes')) {
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

// ==========================================
// SIGUIENTE / COMPLETAR
// ==========================================

/**
 * Abrir modal de siguiente (validar antes de ir a Personal Presente)
 */
function openSiguienteModal() {
    const validacionDiv = document.getElementById('validacionResultado');
    validacionDiv.innerHTML = '';
    
    // Validar que todos los utensilios estén contados
    const rows = document.querySelectorAll('#kardexTable tbody tr:not([style*="display: none"])');
    const sinContar = [];
    const faltantesSinJustificar = [];
    
    rows.forEach((row, index) => {
        const input = row.querySelector('.input-unidades');
        const nombre = row.querySelector('.td-nombre')?.textContent || `Fila ${index + 1}`;
        
        if (!input.value || input.value === '') {
            sinContar.push(nombre);
            row.classList.add('row-incomplete');
        } else {
            // Verificar si tiene faltantes sin justificar
            const tieneFaltantes = row.querySelector('.td-diferencia').dataset.tieneFaltantes === 'true';
            if (tieneFaltantes && detallesPendientesJustificacion.has(parseInt(input.dataset.detalleId))) {
                faltantesSinJustificar.push(nombre);
            }
        }
    });
    
    // Mostrar errores de validación
    if (sinContar.length > 0 || faltantesSinJustificar.length > 0) {
        let errorHtml = '<div class="validacion-error">';
        
        if (sinContar.length > 0) {
            errorHtml += `<strong><i class="fa-solid fa-exclamation-triangle"></i> ${sinContar.length} utensilio(s) sin contar</strong>`;
            errorHtml += '<p>Por favor complete todos los campos antes de continuar:</p>';
            errorHtml += '<ul>';
            sinContar.slice(0, 5).forEach(nombre => {
                errorHtml += `<li>${nombre}</li>`;
            });
            if (sinContar.length > 5) {
                errorHtml += `<li>... y ${sinContar.length - 5} más</li>`;
            }
            errorHtml += '</ul>';
        }
        
        if (faltantesSinJustificar.length > 0) {
            errorHtml += `<strong style="margin-top: 1rem;"><i class="fa-solid fa-file-lines"></i> ${faltantesSinJustificar.length} faltante(s) sin justificar</strong>`;
            errorHtml += '<p>Los siguientes utensilios tienen faltantes que deben ser justificados:</p>';
            errorHtml += '<ul>';
            faltantesSinJustificar.slice(0, 5).forEach(nombre => {
                errorHtml += `<li>${nombre}</li>`;
            });
            if (faltantesSinJustificar.length > 5) {
                errorHtml += `<li>... y ${faltantesSinJustificar.length - 5} más</li>`;
            }
            errorHtml += '</ul>';
        }
        
        errorHtml += '</div>';
        validacionDiv.innerHTML = errorHtml;
        
        // Scroll a la primera fila incompleta
        if (sinContar.length > 0) {
            const primeraIncompleta = document.querySelector('.row-incomplete');
            if (primeraIncompleta) {
                primeraIncompleta.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
        
        // No abrir el modal si hay errores
        return;
    }
    
    // Todo correcto, abrir modal
    const modal = document.getElementById('siguienteModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Confirmar y continuar a Personal Presente
 */
async function confirmarSiguiente() {
    const observaciones = document.getElementById('observacionesGenerales')?.value || '';
    
    // Guardar observaciones en sessionStorage
    sessionStorage.setItem('kardexObservaciones', observaciones);
    
    // Cerrar modal
    closeModal('siguienteModal');
    
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
window.openFaltantesModal = openFaltantesModal;
window.guardarDescripcionFaltantes = guardarDescripcionFaltantes;
window.openSiguienteModal = openSiguienteModal;
window.confirmarSiguiente = confirmarSiguiente;