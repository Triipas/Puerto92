/**
 * Gestión de Asignaciones de Kardex - Puerto 92
 * Asignación, reasignación y cancelación de responsables
 */

// Variables globales
let asignacionesPendientes = [];
let mesActual = null;
let anioActual = null;
let tipoKardexActual = null;

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

function initAsignacionesPage() {
    console.log('🔄 Inicializando página de asignaciones...');
    
    // Obtener datos del contexto
    extraerDatosContexto();
    
    setupModalEventListeners();
    cargarAsignacionesPendientesDesdeServidor();
    
    console.log('✅ Página de asignaciones inicializada correctamente');
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initAsignacionesPage);

// Exponer función para reinicializar después de navegación SPA
window.initAsignacionesPage = initAsignacionesPage;

// ==========================================
// EXTRACCIÓN DE DATOS DEL CONTEXTO
// ==========================================

/**
 * Extraer mes, año y tipo de kardex actual de la página
 */
function extraerDatosContexto() {
    // Extraer del título del calendario
    const mesAnioElement = document.querySelector('.mes-anio');
    if (mesAnioElement) {
        const texto = mesAnioElement.textContent.trim();
        const match = texto.match(/(\w+)\s+de\s+(\d{4})/);
        if (match) {
            const meses = {
                'Enero': 1, 'Febrero': 2, 'Marzo': 3, 'Abril': 4,
                'Mayo': 5, 'Junio': 6, 'Julio': 7, 'Agosto': 8,
                'Septiembre': 9, 'Octubre': 10, 'Noviembre': 11, 'Diciembre': 12
            };
            mesActual = meses[match[1]] || new Date().getMonth() + 1;
            anioActual = parseInt(match[2]);
        }
    }
    
    // Extraer tipo de kardex de la tab activa
    const tabActiva = document.querySelector('.kardex-tab.active span');
    if (tabActiva) {
        tipoKardexActual = tabActiva.textContent.trim();
    }
    
    console.log(`📅 Contexto: ${tipoKardexActual} - ${mesActual}/${anioActual}`);
}

// ==========================================
// ASIGNAR RESPONSABLE
// ==========================================

/**
 * Abrir modal de asignar responsable
 */
async function openAsignarModal(tipoKardex, fecha) {
    console.log(`📝 Abriendo modal de asignar: ${tipoKardex} - ${fecha}`);
    
    try {
        document.getElementById('asignarTipoKardex').value = tipoKardex;
        document.getElementById('asignarFecha').value = fecha;
        
        const fechaObj = new Date(fecha + 'T00:00:00');
        const fechaFormateada = fechaObj.toLocaleDateString('es-ES', { 
            year: 'numeric', 
            month: '2-digit', 
            day: '2-digit' 
        });
        document.getElementById('asignarSubtitulo').textContent = `${tipoKardex} - ${fechaFormateada}`;
        
        const notaVajilla = document.getElementById('notaVajilla');
        if (tipoKardex === 'Vajilla') {
            notaVajilla.style.display = 'block';
        } else {
            notaVajilla.style.display = 'none';
        }
        
        await cargarEmpleadosDisponibles(tipoKardex, fecha);
        
        const modal = document.getElementById('asignarResponsableModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de asignar responsable abierto');
    } catch (error) {
        console.error('❌ Error al abrir modal de asignar:', error);
        showNotification('Error al cargar los datos', 'error');
    }
}

/**
 * Cargar empleados disponibles según el tipo de kardex
 */
async function cargarEmpleadosDisponibles(tipoKardex, fecha) {
    try {
        const response = await fetch(`/Asignaciones/GetEmpleadosDisponibles?tipoKardex=${encodeURIComponent(tipoKardex)}&fecha=${fecha}`);
        
        if (!response.ok) {
            throw new Error('Error al cargar empleados');
        }
        
        const empleados = await response.json();
        const select = document.getElementById('asignarEmpleadoId');
        
        select.innerHTML = '<option value="">Seleccione un empleado</option>';
        
        empleados.forEach(emp => {
            const option = document.createElement('option');
            option.value = emp.id;
            option.textContent = `${emp.nombreCompleto} (${emp.rol})`;
            option.dataset.disponible = emp.disponible;
            option.dataset.motivo = emp.motivoNoDisponible || '';
            
            if (!emp.disponible) {
                option.disabled = true;
                option.textContent += ` - ${emp.motivoNoDisponible}`;
            }
            
            select.appendChild(option);
        });
        
        console.log(`✅ Cargados ${empleados.length} empleados`);
        
        select.addEventListener('change', function() {
            const empleadoInfo = document.getElementById('empleadoInfo');
            const empleadoInfoTexto = document.getElementById('empleadoInfoTexto');
            
            if (this.value) {
                const empleado = empleados.find(e => e.id === this.value);
                
                if (empleado) {
                    empleadoInfoTexto.textContent = `${empleado.nombreCompleto} - Rol: ${empleado.rol}`;
                    empleadoInfo.style.display = 'block';
                }
            } else {
                empleadoInfo.style.display = 'none';
            }
        });
        
    } catch (error) {
        console.error('❌ Error al cargar empleados:', error);
        showNotification('Error al cargar los empleados disponibles', 'error');
    }
}

/**
 * Confirmar asignación (agregar a pendientes)
 */
async function confirmarAsignacion() {
    const tipoKardex = document.getElementById('asignarTipoKardex').value;
    const fecha = document.getElementById('asignarFecha').value;
    const empleadoId = document.getElementById('asignarEmpleadoId').value;
    
    if (!empleadoId) {
        showNotification('Por favor seleccione un empleado', 'warning');
        return;
    }
    
    const btnAsignar = document.getElementById('btnAsignar');
    btnAsignar.disabled = true;
    btnAsignar.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Asignando...';
    
    try {
        const response = await fetch('/Asignaciones/Asignar', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                TipoKardex: tipoKardex,
                Fecha: fecha,
                EmpleadoId: empleadoId
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('asignarResponsableModal');
            showNotification('Asignación agregada. Recuerde guardar para confirmar y notificar.', 'success');
            
            // ✅ RECARGAR asignaciones pendientes desde el servidor
            await cargarAsignacionesPendientesDesdeServidor();
            
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            showNotification(result.message || 'Error al asignar', 'error');
        }
    } catch (error) {
        console.error('❌ Error al asignar:', error);
        showNotification('Error al crear la asignación', 'error');
    } finally {
        btnAsignar.disabled = false;
        btnAsignar.innerHTML = '<i class="fa-solid fa-user-plus"></i> Asignar';
    }
}

// ==========================================
// REASIGNAR RESPONSABLE
// ==========================================

async function openReasignarModal(asignacionId, tipoKardex, fecha, empleadoActualId, empleadoActualNombre, registroIniciado) {
    console.log(`🔄 Abriendo modal de reasignar: ${asignacionId}`);
    
    try {
        document.getElementById('reasignarAsignacionId').value = asignacionId;
        document.getElementById('reasignarTipoKardex').value = tipoKardex;
        document.getElementById('reasignarFecha').value = fecha;
        document.getElementById('reasignarEmpleadoActualId').value = empleadoActualId;
        
        const fechaObj = new Date(fecha + 'T00:00:00');
        const fechaFormateada = fechaObj.toLocaleDateString('es-ES', { 
            year: 'numeric', 
            month: '2-digit', 
            day: '2-digit' 
        });
        document.getElementById('reasignarSubtitulo').textContent = `${tipoKardex} - ${fechaFormateada}`;
        
        document.getElementById('reasignarEmpleadoActual').textContent = empleadoActualNombre;
        
        const registroWarning = document.getElementById('registroIniciadoWarning');
        if (registroIniciado) {
            registroWarning.style.display = 'block';
        } else {
            registroWarning.style.display = 'none';
        }
        
        await cargarEmpleadosDisponiblesReasignacion(tipoKardex, fecha, empleadoActualId);
        
        const modal = document.getElementById('reasignarResponsableModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de reasignar responsable abierto');
    } catch (error) {
        console.error('❌ Error al abrir modal de reasignar:', error);
        showNotification('Error al cargar los datos', 'error');
    }
}

async function cargarEmpleadosDisponiblesReasignacion(tipoKardex, fecha, empleadoActualId) {
    try {
        const response = await fetch(`/Asignaciones/GetEmpleadosDisponibles?tipoKardex=${encodeURIComponent(tipoKardex)}&fecha=${fecha}`);
        
        if (!response.ok) {
            throw new Error('Error al cargar empleados');
        }
        
        const empleados = await response.json();
        const select = document.getElementById('reasignarNuevoEmpleadoId');
        
        select.innerHTML = '<option value="">Seleccione un empleado</option>';
        
        empleados.forEach(emp => {
            if (emp.id === empleadoActualId) return;
            
            const option = document.createElement('option');
            option.value = emp.id;
            option.textContent = `${emp.nombreCompleto} (${emp.rol})`;
            
            if (!emp.disponible) {
                option.disabled = true;
                option.textContent += ` - ${emp.motivoNoDisponible}`;
            }
            
            select.appendChild(option);
        });
        
        console.log(`✅ Cargados ${empleados.length} empleados para reasignación`);
        
    } catch (error) {
        console.error('❌ Error al cargar empleados:', error);
        showNotification('Error al cargar los empleados disponibles', 'error');
    }
}

async function confirmarReasignacion() {
    const asignacionId = document.getElementById('reasignarAsignacionId').value;
    const nuevoEmpleadoId = document.getElementById('reasignarNuevoEmpleadoId').value;
    const motivo = document.getElementById('reasignarMotivo').value;
    
    if (!nuevoEmpleadoId) {
        showNotification('Por favor seleccione un nuevo responsable', 'warning');
        return;
    }
    
    const btnReasignar = document.getElementById('btnReasignar');
    btnReasignar.disabled = true;
    btnReasignar.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Reasignando...';
    
    try {
        const response = await fetch('/Asignaciones/Reasignar', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                AsignacionId: parseInt(asignacionId),
                NuevoEmpleadoId: nuevoEmpleadoId,
                Motivo: motivo
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('reasignarResponsableModal');
            showNotification(`Reasignación exitosa. Nuevo responsable: ${result.data.empleadoNuevo}`, 'success');
            
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            showNotification(result.message || 'Error al reasignar', 'error');
        }
    } catch (error) {
        console.error('❌ Error al reasignar:', error);
        showNotification('Error al realizar la reasignación', 'error');
    } finally {
        btnReasignar.disabled = false;
        btnReasignar.innerHTML = '<i class="fa-solid fa-arrows-rotate"></i> Reasignar';
    }
}

// ==========================================
// CANCELAR ASIGNACIÓN
// ==========================================

/**
 * Abrir modal de cancelar asignación
 */
function openCancelarModal(asignacionId, tipoKardex, fecha, empleadoNombre, estado) {
    console.log(`❌ Abriendo modal de cancelar: ${asignacionId}`);
    
    document.getElementById('cancelarAsignacionId').value = asignacionId;
    document.getElementById('cancelarTipoKardex').textContent = tipoKardex;
    
    const fechaObj = new Date(fecha + 'T00:00:00');
    const fechaFormateada = fechaObj.toLocaleDateString('es-ES', { 
        year: 'numeric', 
        month: '2-digit', 
        day: '2-digit' 
    });
    document.getElementById('cancelarFecha').textContent = fechaFormateada;
    document.getElementById('cancelarEmpleado').textContent = empleadoNombre;
    document.getElementById('cancelarEstado').textContent = estado;
    
    const modal = document.getElementById('cancelarAsignacionModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Confirmar cancelación
 */
async function confirmarCancelacion() {
    const asignacionId = document.getElementById('cancelarAsignacionId').value;
    const motivo = document.getElementById('cancelarMotivo').value;
    
    if (!motivo || motivo.trim() === '') {
        showNotification('Por favor indique el motivo de la cancelación', 'warning');
        return;
    }
    
    const btnConfirmar = document.getElementById('btnConfirmarCancelar');
    btnConfirmar.disabled = true;
    btnConfirmar.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Cancelando...';
    
    try {
        const response = await fetch('/Asignaciones/CancelarAsignacion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(motivo)
        });
        
        const url = `/Asignaciones/CancelarAsignacion?id=${asignacionId}`;
        const responseFinal = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(motivo)
        });
        
        const result = await responseFinal.json();
        
        if (result.success) {
            closeModal('cancelarAsignacionModal');
            showNotification('Asignación cancelada exitosamente', 'success');
            
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            showNotification(result.message || 'Error al cancelar', 'error');
        }
    } catch (error) {
        console.error('❌ Error al cancelar:', error);
        showNotification('Error al cancelar la asignación', 'error');
    } finally {
        btnConfirmar.disabled = false;
        btnConfirmar.innerHTML = '<i class="fa-solid fa-ban"></i> Sí, Cancelar Asignación';
    }
}

// ==========================================
// GUARDAR Y NOTIFICAR
// ==========================================

/**
 * Cargar asignaciones pendientes desde el servidor
 */
async function cargarAsignacionesPendientesDesdeServidor() {
    try {
        if (!mesActual || !anioActual || !tipoKardexActual) {
            console.warn('⚠️ Faltan datos de contexto');
            return;
        }
        
        const response = await fetch(`/Asignaciones/GetAsignacionesPendientes?tipoKardex=${encodeURIComponent(tipoKardexActual)}&mes=${mesActual}&anio=${anioActual}`);
        
        if (!response.ok) {
            throw new Error('Error al cargar asignaciones pendientes');
        }
        
        asignacionesPendientes = await response.json();
        console.log(`📊 Asignaciones pendientes cargadas: ${asignacionesPendientes.length}`);
        
        actualizarContadorPendientes();
    } catch (error) {
        console.error('❌ Error al cargar asignaciones pendientes:', error);
    }
}

/**
 * Actualizar contador de asignaciones pendientes
 */
function actualizarContadorPendientes() {
    const contador = document.getElementById('contadorPendientes');
    const btnGuardar = document.getElementById('btnGuardarAsignaciones');
    
    if (asignacionesPendientes.length > 0) {
        contador.textContent = asignacionesPendientes.length;
        btnGuardar.style.display = 'flex';
    } else {
        btnGuardar.style.display = 'none';
    }
}

/**
 * Abrir modal de confirmar asignaciones
 */
function openConfirmarAsignacionesModal() {
    if (asignacionesPendientes.length === 0) {
        showNotification('No hay asignaciones pendientes para guardar', 'info');
        return;
    }
    
    const plural = asignacionesPendientes.length > 1 ? 's' : '';
    document.getElementById('confirmarSubtitulo').textContent = 
        `Se guardarán ${asignacionesPendientes.length} asignación${plural} y se enviará notificación a cada empleado.`;
    
    const lista = document.getElementById('listaAsignacionesPendientes');
    lista.innerHTML = '';
    
    asignacionesPendientes.forEach(asig => {
        const item = document.createElement('div');
        item.style.cssText = 'background: white; border: 1px solid #E5E7EB; border-radius: 8px; padding: 1rem;';
        item.innerHTML = `
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <div>
                    <div style="font-weight: 600; color: #1F2937; margin-bottom: 0.25rem;">${asig.empleado}</div>
                    <div style="font-size: 12px; color: #64748B;">${asig.tipoKardex} - ${new Date(asig.fecha + 'T00:00:00').toLocaleDateString('es-ES')}</div>
                </div>
                <span class="asignacion-badge pendiente">Pendiente</span>
            </div>
        `;
        lista.appendChild(item);
    });
    
    const modal = document.getElementById('confirmarAsignacionesModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
}

/**
 * Guardar y notificar todas las asignaciones pendientes
 */
async function guardarYNotificar() {
    if (asignacionesPendientes.length === 0) {
        showNotification('No hay asignaciones pendientes', 'info');
        return;
    }
    
    const btnConfirmar = document.getElementById('btnConfirmarYNotificar');
    btnConfirmar.disabled = true;
    btnConfirmar.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Procesando...';
    
    try {
        const ids = asignacionesPendientes.map(a => a.id);
        
        const response = await fetch('/Asignaciones/GuardarAsignaciones', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(ids)
        });
        
        const result = await response.json();
        
        if (result.success) {
            closeModal('confirmarAsignacionesModal');
            showNotification(result.message, 'success');
            
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            showNotification(result.message || 'Error al guardar', 'error');
        }
    } catch (error) {
        console.error('❌ Error al guardar:', error);
        showNotification('Error al guardar las asignaciones', 'error');
    } finally {
        btnConfirmar.disabled = false;
        btnConfirmar.innerHTML = '<i class="fa-solid fa-paper-plane"></i> Confirmar y Notificar';
    }
}

// ==========================================
// HISTORIAL
// ==========================================

/**
 * Abrir modal de historial
 */
async function openHistorialModal() {
    console.log('📜 Abriendo modal de historial...');
    
    const modal = document.getElementById('historialModal');
    modal.style.display = 'flex';
    modal.classList.add('active');
    
    await cargarHistorial();
}

/**
 * Cargar historial desde el servidor
 */
async function cargarHistorial() {
    const listaHistorial = document.getElementById('listaHistorial');
    listaHistorial.innerHTML = '<p style="color: #94A3B8; font-size: 13px; text-align: center; padding: 2rem;">Cargando historial...</p>';
    
    try {
        if (!mesActual || !anioActual) {
            throw new Error('Faltan datos de contexto');
        }
        
        const response = await fetch(`/Asignaciones/GetHistorial?mes=${mesActual}&anio=${anioActual}`);
        
        if (!response.ok) {
            throw new Error('Error al cargar historial');
        }
        
        const historial = await response.json();
        
        if (historial.length === 0) {
            listaHistorial.innerHTML = '<p style="color: #94A3B8; font-size: 13px; text-align: center; padding: 2rem;">No hay registros en el historial para este mes.</p>';
            return;
        }
        
        listaHistorial.innerHTML = '';
        
        historial.forEach(item => {
            const fecha = new Date(item.fechaHora);
            const fechaFormateada = fecha.toLocaleDateString('es-ES', { day: '2-digit', month: '2-digit' });
            const horaFormateada = fecha.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' });
            
            const itemDiv = document.createElement('div');
            itemDiv.className = 'historial-item';
            itemDiv.innerHTML = `
                <div style="display: flex; gap: 1rem; align-items: start;">
                    <div style="flex-shrink: 0; width: 70px; text-align: center;">
                        <div style="font-weight: 600; font-size: 13px; color: #1F2937;">${fechaFormateada}</div>
                        <div style="font-size: 11px; color: #94A3B8;">${horaFormateada}</div>
                    </div>
                    <div style="flex: 1; min-width: 0;">
                        <div style="font-weight: 600; font-size: 13px; color: #1E293B; margin-bottom: 0.25rem;">${item.accion}</div>
                        <div style="font-size: 12px; color: #64748B; line-height: 1.5;">${item.descripcion}</div>
                        <div style="font-size: 11px; color: #94A3B8; margin-top: 0.25rem;">Por: ${item.usuario}</div>
                    </div>
                    <div style="flex-shrink: 0;">
                        <span class="historial-badge ${item.nivelSeveridad.toLowerCase()}">${item.resultado}</span>
                    </div>
                </div>
            `;
            
            listaHistorial.appendChild(itemDiv);
        });
        
        console.log(`✅ Historial cargado: ${historial.length} registros`);
        
    } catch (error) {
        console.error('❌ Error al cargar historial:', error);
        listaHistorial.innerHTML = '<p style="color: #EF4444; font-size: 13px; text-align: center; padding: 2rem;">Error al cargar el historial</p>';
    }
}

// ==========================================
// GESTIÓN DE MODALES
// ==========================================

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    modal.classList.remove('active');
    setTimeout(() => {
        modal.style.display = 'none';
    }, 200);
}

function setupModalEventListeners() {
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
    
    const btnGuardar = document.getElementById('btnGuardarAsignaciones');
    if (btnGuardar) {
        btnGuardar.addEventListener('click', openConfirmarAsignacionesModal);
    }
    
    const btnHistorial = document.getElementById('btnVerHistorial');
    if (btnHistorial) {
        btnHistorial.addEventListener('click', openHistorialModal);
    }
}

// ==========================================
// UTILIDADES
// ==========================================

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

window.openAsignarModal = openAsignarModal;
window.openReasignarModal = openReasignarModal;
window.openCancelarModal = openCancelarModal;
window.openConfirmarAsignacionesModal = openConfirmarAsignacionesModal;
window.openHistorialModal = openHistorialModal;
window.confirmarAsignacion = confirmarAsignacion;
window.confirmarReasignacion = confirmarReasignacion;
window.confirmarCancelacion = confirmarCancelacion;
window.guardarYNotificar = guardarYNotificar;
window.closeModal = closeModal;