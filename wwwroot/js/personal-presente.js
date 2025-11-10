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
    
    // ⭐ VERIFICAR QUE LAS VARIABLES YA ESTÉN CARGADAS (definidas en el HEAD)
    if (typeof window.KARDEX_ID === 'undefined') {
        console.error('❌ ERROR CRÍTICO: window.KARDEX_ID no está definido');
        console.error('    Las variables deben estar definidas en el HEAD del HTML');
        showNotification('Error de configuración. Recargue la página.', 'error');
        return;
    }
    
    console.log('✅ Variables globales verificadas:');
    console.log('   KARDEX_ID:', window.KARDEX_ID);
    console.log('   TIPO_KARDEX:', window.TIPO_KARDEX);
    console.log('   DENTRO_DE_HORARIO:', window.DENTRO_DE_HORARIO);
    console.log('   ENVIO_HABILITADO_MANUALMENTE:', window.ENVIO_HABILITADO_MANUALMENTE);
    
    inicializarVariables();
    inicializarEventos();
    actualizarContador();
    
    console.log('✅ Personal Presente inicializado correctamente');
});

/**
 * Inicializar variables desde el contexto
 */
function inicializarVariables() {
    empleadoResponsableId = window.EMPLEADO_RESPONSABLE_ID || '';
    
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
    
    // Actualizar hora en tiempo real
    actualizarHoraActual();
    setInterval(actualizarHoraActual, 1000);
    
    console.log('✅ Eventos configurados');
}

/**
 * Actualizar hora actual en la interfaz
 */
function actualizarHoraActual() {
    const horaElements = document.querySelectorAll('#horaActual');
    if (horaElements.length === 0) return;
    
    const ahora = new Date();
    const horas = ahora.getHours();
    const minutos = ahora.getMinutes().toString().padStart(2, '0');
    const ampm = horas >= 12 ? 'p. m.' : 'a. m.';
    const horas12 = horas % 12 || 12;
    
    const horaFormateada = `${horas12.toString().padStart(2, '0')}:${minutos} ${ampm}`;
    
    horaElements.forEach(el => {
        el.textContent = horaFormateada;
    });
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
 * Enviar al administrador - CON VALIDACIÓN DE HORARIO
 */
async function enviarAlAdministrador() {
    console.log('📤 Iniciando envío al administrador...');
    
    // ⭐ VALIDACIÓN: Verificar que las variables existan
    if (typeof window.KARDEX_ID === 'undefined' || typeof window.TIPO_KARDEX === 'undefined') {
        showNotification('Error: Variables de configuración no encontradas. Recargue la página.', 'error');
        return;
    }
    
    // ⭐ VALIDAR HORARIO
    const dentroDeHorario = window.DENTRO_DE_HORARIO === true || window.DENTRO_DE_HORARIO === 'true';
    const habilitadoManual = window.ENVIO_HABILITADO_MANUALMENTE === true || window.ENVIO_HABILITADO_MANUALMENTE === 'true';
    
    if (!dentroDeHorario && !habilitadoManual) {
        showNotification(
            'Fuera de horario. El envío ha sido bloqueado. El horario límite de envío es 5:30 PM.',
            'error'
        );
        return;
    }
    
    // Validar que hay al menos el responsable seleccionado
    if (empleadosSeleccionados.length === 0) {
        showNotification('Error: No hay empleados seleccionados', 'error');
        return;
    }
    
    // Validar que el responsable esté incluido
    if (empleadoResponsableId && !empleadosSeleccionados.includes(empleadoResponsableId)) {
        showNotification('Error: El responsable principal debe estar seleccionado', 'error');
        return;
    }
    
    // ⭐ RECUPERAR DATOS DEL KARDEX (guardados en sessionStorage)
    const observacionesKardex = sessionStorage.getItem('kardexObservaciones') || '';
    const descripcionFaltantes = sessionStorage.getItem('kardexDescripcionFaltantes') || '';
    
    const btnEnviar = document.getElementById('btnEnviarAlAdministrador');
    if (!btnEnviar) {
        showNotification('Error: Botón de envío no encontrado', 'error');
        return;
    }
    
    btnEnviar.disabled = true;
    btnEnviar.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Enviando...';
    
    // Deshabilitar checkboxes
    document.querySelectorAll('.personal-checkbox').forEach(cb => cb.disabled = true);
    
    try {
        const requestData = {
            kardexId: parseInt(window.KARDEX_ID),
            tipoKardex: window.TIPO_KARDEX,
            empleadosPresentes: empleadosSeleccionados,
            observacionesKardex: observacionesKardex
        };
        
        // ⭐ AGREGAR DESCRIPCIÓN DE FALTANTES SI ES KARDEX DE SALÓN
        if (window.TIPO_KARDEX === 'Mozo Salón') {
            requestData.descripcionFaltantes = descripcionFaltantes;
            console.log(`📋 Descripción de faltantes incluida: ${descripcionFaltantes ? 'Sí' : 'No'}`);
        }
        
        console.log('📦 Datos a enviar:', requestData);
        
        const response = await fetch('/Kardex/GuardarPersonalPresente', {
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
            console.log('✅ Kardex enviado exitosamente al administrador');
            
            // Limpiar sessionStorage
            sessionStorage.removeItem('kardexObservaciones');
            sessionStorage.removeItem('kardexDescripcionFaltantes');
            
            showNotification('Kardex enviado exitosamente al administrador', 'success');
            
            setTimeout(() => {
                window.location.href = result.redirectUrl || '/Kardex/MiKardex';
            }, 1500);
        } else {
            throw new Error(result.message || 'Error al enviar');
        }
    } catch (error) {
        console.error('❌ Error al enviar:', error);
        showNotification(error.message || 'Error al enviar el kardex', 'error');
        
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

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

window.enviarAlAdministrador = enviarAlAdministrador;
window.manejarCambioCheckbox = manejarCambioCheckbox;