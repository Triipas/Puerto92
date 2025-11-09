/**
 * Personal Presente - Puerto 92
 * Gestión de selección de personal presente durante el kardex
 */

// Variables globales
let empleadosSeleccionados = [];
let empleadoResponsableId = '';

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', function() {
    console.log('🔄 Inicializando Personal Presente...');
    
    inicializarVariables();
    inicializarEventos();
    actualizarContador();
    
    console.log('✅ Personal Presente inicializado');
});

/**
 * Inicializar variables desde el contexto
 */
function inicializarVariables() {
    if (typeof EMPLEADO_RESPONSABLE_ID !== 'undefined') {
        empleadoResponsableId = EMPLEADO_RESPONSABLE_ID;
    }
    
    // Obtener empleados pre-seleccionados
    empleadosSeleccionados = [];
    document.querySelectorAll('.personal-checkbox:checked, .personal-checkbox-main:checked').forEach(checkbox => {
        empleadosSeleccionados.push(checkbox.value);
    });
    
    console.log(`📋 ${empleadosSeleccionados.length} empleado(s) pre-seleccionado(s)`);
}

/**
 * Inicializar eventos
 */
function inicializarEventos() {
    // Eventos de checkboxes
    document.querySelectorAll('.personal-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            manejarCambioCheckbox(this);
        });
    });
    
    // Prevenir cambios en el checkbox del responsable principal
    document.querySelectorAll('.personal-checkbox-main').forEach(checkbox => {
        checkbox.addEventListener('click', function(e) {
            e.preventDefault();
            return false;
        });
    });
    
    console.log('✅ Eventos configurados');
}

// ==========================================
// MANEJO DE SELECCIÓN
// ==========================================

/**
 * Manejar cambio en checkbox
 */
function manejarCambioCheckbox(checkbox) {
    const empleadoId = checkbox.value;
    const empleadoNombre = checkbox.dataset.empleadoNombre;
    const personalItem = checkbox.closest('.personal-item');
    
    if (checkbox.checked) {
        // Agregar a la lista
        if (!empleadosSeleccionados.includes(empleadoId)) {
            empleadosSeleccionados.push(empleadoId);
        }
        
        // Animar y marcar como seleccionado
        personalItem.classList.add('selecting');
        setTimeout(() => {
            personalItem.classList.remove('selecting');
            personalItem.classList.add('selected');
        }, 300);
        
        console.log(`✅ Empleado seleccionado: ${empleadoNombre}`);
    } else {
        // Remover de la lista
        empleadosSeleccionados = empleadosSeleccionados.filter(id => id !== empleadoId);
        
        // Quitar marca de seleccionado
        personalItem.classList.remove('selected');
        
        console.log(`❌ Empleado deseleccionado: ${empleadoNombre}`);
    }
    
    // Actualizar contador
    actualizarContador();
}

/**
 * Actualizar contador de seleccionados
 */
function actualizarContador() {
    const contador = document.getElementById('contadorSeleccionados');
    const textoEmpleados = document.getElementById('textoCantidadEmpleados');
    
    if (contador) {
        contador.textContent = empleadosSeleccionados.length;
    }
    
    if (textoEmpleados) {
        const cantidad = empleadosSeleccionados.length;
        const plural = cantidad !== 1 ? 's' : '';
        textoEmpleados.textContent = `${cantidad} empleado${plural}`;
    }
}

// ==========================================
// ENVÍO DE DATOS
// ==========================================

/**
 * Enviar al administrador
 */
async function enviarAlAdministrador() {
    console.log('📤 Enviando personal presente...');
    
    // Validar que hay al menos el responsable seleccionado
    if (empleadosSeleccionados.length === 0) {
        showNotification('Error: No hay empleados seleccionados', 'error');
        return;
    }
    
    // Validar que el responsable esté incluido
    if (!empleadosSeleccionados.includes(empleadoResponsableId)) {
        showNotification('Error: El responsable principal debe estar seleccionado', 'error');
        return;
    }
    
    const btnEnviar = document.getElementById('btnEnviarAlAdministrador');
    btnEnviar.disabled = true;
    btnEnviar.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Enviando...';
    
    // Deshabilitar checkboxes
    document.querySelectorAll('.personal-checkbox').forEach(cb => cb.disabled = true);
    
    try {
        const response = await fetch('/Kardex/GuardarPersonalPresente', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({
                kardexId: KARDEX_ID,
                tipoKardex: TIPO_KARDEX,
                empleadosPresentes: empleadosSeleccionados
            })
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        const result = await response.json();
        
        if (result.success) {
            console.log('✅ Personal presente guardado exitosamente');
            showNotification(result.message, 'success');
            
            setTimeout(() => {
                window.location.href = result.redirectUrl || '/Kardex/MiKardex';
            }, 1500);
        } else {
            throw new Error(result.message || 'Error al guardar');
        }
    } catch (error) {
        console.error('❌ Error al enviar:', error);
        showNotification(error.message || 'Error al enviar los datos', 'error');
        
        // Re-habilitar controles
        btnEnviar.disabled = false;
        btnEnviar.innerHTML = '<i class="fa-solid fa-paper-plane"></i> Enviar al Administrador';
        document.querySelectorAll('.personal-checkbox').forEach(cb => cb.disabled = false);
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

/**
 * Confirmar antes de salir si hay cambios
 */
window.addEventListener('beforeunload', function(e) {
    const checkboxes = document.querySelectorAll('.personal-checkbox');
    let haycambios = false;
    
    checkboxes.forEach(checkbox => {
        const estadoActual = checkbox.checked;
        const estadoInicial = checkbox.defaultChecked;
        
        if (estadoActual !== estadoInicial) {
            hayChangios = true;
        }
    });
    
    if (hayChangios) {
        e.preventDefault();
        e.returnValue = '';
    }
});

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

window.enviarAlAdministrador = enviarAlAdministrador;
window.manejarCambioCheckbox = manejarCambioCheckbox;