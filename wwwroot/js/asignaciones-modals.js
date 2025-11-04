/**
 * Gestión de Asignaciones de Kardex - Puerto 92
 * Asignación y reasignación de responsables
 */

// Variables globales
let asignacionesPendientes = [];

// ==========================================
// INICIALIZACIÓN GLOBAL
// ==========================================

function initAsignacionesPage() {
    console.log('🔄 Inicializando página de asignaciones...');
    
    setupModalEventListeners();
    actualizarContadorPendientes();
    cargarAsignacionesPendientes();
    
    console.log('✅ Página de asignaciones inicializada correctamente');
}

// Ejecutar al cargar el documento
document.addEventListener('DOMContentLoaded', initAsignacionesPage);

// Exponer función para reinicializar después de navegación SPA
window.initAsignacionesPage = initAsignacionesPage;

// ==========================================
// ASIGNAR RESPONSABLE
// ==========================================

/**
 * Abrir modal de asignar responsable
 */
async function openAsignarModal(tipoKardex, fecha) {
    console.log(`📝 Abriendo modal de asignar: ${tipoKardex} - ${fecha}`);
    
    try {
        // Configurar datos del modal
        document.getElementById('asignarTipoKardex').value = tipoKardex;
        document.getElementById('asignarFecha').value = fecha;
        
        // Actualizar subtítulo
        const fechaObj = new Date(fecha + 'T00:00:00');
        const fechaFormateada = fechaObj.toLocaleDateString('es-ES', { 
            year: 'numeric', 
            month: '2-digit', 
            day: '2-digit' 
        });
        document.getElementById('asignarSubtitulo').textContent = `${tipoKardex} - ${fechaFormateada}`;
        
        // Mostrar nota especial para Vajilla
        const notaVajilla = document.getElementById('notaVajilla');
        if (tipoKardex === 'Vajilla') {
            notaVajilla.style.display = 'block';
        } else {
            notaVajilla.style.display = 'none';
        }
        
        // Cargar empleados disponibles
        await cargarEmpleadosDisponibles(tipoKardex, fecha);
        
        // Mostrar modal
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
        
        // Limpiar select
        select.innerHTML = '<option value="">Seleccione un empleado</option>';
        
        // Agregar empleados
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
        
        // Setup listener para mostrar info del empleado
        select.addEventListener('change', function() {
            const empleadoInfo = document.getElementById('empleadoInfo');
            const empleadoInfoTexto = document.getElementById('empleadoInfoTexto');
            
            if (this.value) {
                const selectedOption = this.options[this.selectedIndex];
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
            
            // Agregar a auditoría
            agregarAuditoria(`Asignación creada: ${tipoKardex} - Fecha: ${fecha}`);
            
            // Recargar página para reflejar cambios
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

/**
 * Abrir modal de reasignar responsable
 */
async function openReasignarModal(asignacionId, tipoKardex, fecha, empleadoActualId, empleadoActualNombre, registroIniciado) {
    console.log(`🔄 Abriendo modal de reasignar: ${asignacionId}`);
    
    try {
        // Configurar datos del modal
        document.getElementById('reasignarAsignacionId').value = asignacionId;
        document.getElementById('reasignarTipoKardex').value = tipoKardex;
        document.getElementById('reasignarFecha').value = fecha;
        document.getElementById('reasignarEmpleadoActualId').value = empleadoActualId;
        
        // Actualizar subtítulo
        const fechaObj = new Date(fecha + 'T00:00:00');
        const fechaFormateada = fechaObj.toLocaleDateString('es-ES', { 
            year: 'numeric', 
            month: '2-digit', 
            day: '2-digit' 
        });
        document.getElementById('reasignarSubtitulo').textContent = `${tipoKardex} - ${fechaFormateada}`;
        
        // Mostrar empleado actual
        document.getElementById('reasignarEmpleadoActual').textContent = empleadoActualNombre;
        
        // Mostrar/ocultar advertencia de registro iniciado
        const registroWarning = document.getElementById('registroIniciadoWarning');
        if (registroIniciado) {
            registroWarning.style.display = 'block';
        } else {
            registroWarning.style.display = 'none';
        }
        
        // Cargar empleados disponibles
        await cargarEmpleadosDisponiblesReasignacion(tipoKardex, fecha, empleadoActualId);
        
        // Mostrar modal
        const modal = document.getElementById('reasignarResponsableModal');
        modal.style.display = 'flex';
        modal.classList.add('active');
        
        console.log('✅ Modal de reasignar responsable abierto');
    } catch (error) {
        console.error('❌ Error al abrir modal de reasignar:', error);
        showNotification('Error al cargar los datos', 'error');
    }
}

/**
 * Cargar empleados disponibles para reasignación
 */
async function cargarEmpleadosDisponiblesReasignacion(tipoKardex, fecha, empleadoActualId) {
    try {
        const response = await fetch(`/Asignaciones/GetEmpleadosDisponibles?tipoKardex=${encodeURIComponent(tipoKardex)}&fecha=${fecha}`);
        
        if (!response.ok) {
            throw new Error('Error al cargar empleados');
        }
        
        const empleados = await response.json();
        const select = document.getElementById('reasignarNuevoEmpleadoId');
        
        // Limpiar select
        select.innerHTML = '<option value="">Seleccione un empleado</option>';
        
        // Agregar empleados (excepto el actual)
        empleados.forEach(emp => {
            if (emp.id === empleadoActualId) return; // Saltar empleado actual
            
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

/**
 * Confirmar reasignación
 */
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
            
            // Agregar a auditoría
            agregarAuditoria(`Reasignación completada - Nuevo responsable: ${result.data.empleadoNuevo}`);
            
            // Recargar página para reflejar cambios
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
// GUARDAR Y NOTIFICAR
// ==========================================

/**
 * Cargar asignaciones pendientes del calendario
 */
function cargarAsignacionesPendientes() {
    asignacionesPendientes = [];
    
    // Buscar todas las celdas con estado pendiente
    const diasPendientes = document.querySelectorAll('.calendario-dia.pendiente[data-asignacion-id]');
    
    diasPendientes.forEach(dia => {
        const asignacionId = parseInt(dia.dataset.asignacionId);
        const fecha = dia.dataset.fecha;
        const empleadoNombre = dia.querySelector('.empleado-nombre')?.textContent || 'Desconocido';
        const tipoKardex = dia.closest('.card-body')?.querySelector('.kardex-tab.active span')?.textContent || 'Desconocido';
        
        asignacionesPendientes.push({
            id: asignacionId,
            fecha: fecha,
            empleado: empleadoNombre,
            tipoKardex: tipoKardex
        });
    });
    
    console.log(`📊 Asignaciones pendientes: ${asignacionesPendientes.length}`);
    actualizarContadorPendientes();
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
    
    // Actualizar subtítulo
    const plural = asignacionesPendientes.length > 1 ? 's' : '';
    document.getElementById('confirmarSubtitulo').textContent = 
        `Se guardarán ${asignacionesPendientes.length} asignación${plural} y se enviará notificación a cada empleado.`;
    
    // Cargar lista de asignaciones
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
    
    // Mostrar modal
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
            
            // Agregar a auditoría
            agregarAuditoria(`Guardado masivo: ${asignacionesPendientes.length} asignación(es) confirmada(s) y notificada(s)`);
            
            // Recargar página
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
// GESTIÓN DE MODALES
// ==========================================

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
    
    // Listener para botón de guardar asignaciones
    const btnGuardar = document.getElementById('btnGuardarAsignaciones');
    if (btnGuardar) {
        btnGuardar.addEventListener('click', openConfirmarAsignacionesModal);
    }
}

// ==========================================
// UTILIDADES
// ==========================================

/**
 * Agregar mensaje a la auditoría
 */
function agregarAuditoria(mensaje) {
    const auditoriaLista = document.getElementById('auditoriaLista');
    const item = document.createElement('div');
    item.className = 'auditoria-item';
    
    const ahora = new Date();
    const tiempo = ahora.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' });
    
    item.innerHTML = `<span class="auditoria-tiempo">${tiempo}</span> ${mensaje}`;
    auditoriaLista.insertBefore(item, auditoriaLista.firstChild);
    
    // Limpiar primera línea si existe
    const primerParrafo = auditoriaLista.querySelector('p');
    if (primerParrafo) {
        primerParrafo.remove();
    }
}

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

window.openAsignarModal = openAsignarModal;
window.openReasignarModal = openReasignarModal;
window.openConfirmarAsignacionesModal = openConfirmarAsignacionesModal;
window.confirmarAsignacion = confirmarAsignacion;
window.confirmarReasignacion = confirmarReasignacion;
window.guardarYNotificar = guardarYNotificar;
window.closeModal = closeModal;