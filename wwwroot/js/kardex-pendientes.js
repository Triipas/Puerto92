/**
 * Kardex Pendientes de Revisión - Puerto 92
 * Manejo de tabs y navegación
 */

// ==========================================
// INICIALIZACIÓN
// ==========================================

document.addEventListener('DOMContentLoaded', function() {
    console.log('🔄 Inicializando Kardex Pendientes de Revisión...');
    
    // Verificar si hay un tab específico en la URL
    const urlParams = new URLSearchParams(window.location.search);
    const tabParam = urlParams.get('tab');
    
    if (tabParam) {
        cambiarTab(tabParam);
    }
    
    console.log('✅ Kardex Pendientes inicializado');
});

// ==========================================
// MANEJO DE TABS
// ==========================================

/**
 * Cambiar entre tabs (Cocina, Mozos, Vajilla)
 */
function cambiarTab(tabId) {
    console.log(`📑 Cambiando a tab: ${tabId}`);
    
    // Remover clase active de todos los tabs
    document.querySelectorAll('.kardex-tab').forEach(tab => {
        tab.classList.remove('active');
    });
    
    // Ocultar todos los paneles
    document.querySelectorAll('.tab-panel').forEach(panel => {
        panel.classList.remove('active');
    });
    
    // Activar tab seleccionado
    const selectedTab = document.querySelector(`.kardex-tab[data-tab="${tabId}"]`);
    if (selectedTab) {
        selectedTab.classList.add('active');
    }
    
    // Mostrar panel correspondiente
    const selectedPanel = document.getElementById(`tab-${tabId}`);
    if (selectedPanel) {
        selectedPanel.classList.add('active');
    }
    
    // Actualizar URL sin recargar la página
    const newUrl = new URL(window.location);
    newUrl.searchParams.set('tab', tabId);
    window.history.pushState({}, '', newUrl);
}

// ==========================================
// UTILIDADES
// ==========================================

/**
 * Mostrar notificación
 */
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

// ==========================================
// EXPORTAR FUNCIONES GLOBALES
// ==========================================

window.cambiarTab = cambiarTab;