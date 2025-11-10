/**
 * Sistema de Notificaciones - Puerto 92
 * Manejo de notificaciones en tiempo real con polling
 */

class NotificationManager {
    constructor() {
        this.pollingInterval = null;
        this.pollingFrequency = 30000; // 30 segundos
        this.panelOpen = false;
        this.currentNotifications = [];
        this.shownPopups = new Set(); // Para no mostrar pop-ups duplicados

        this.init();
    }

    init() {
        console.log('🔔 Inicializando sistema de notificaciones...');

        // Iniciar polling
        this.startPolling();

        // Configurar event listeners
        this.setupEventListeners();

        // Cargar notificaciones iniciales
        this.cargarContadorNotificaciones();

        console.log('✅ Sistema de notificaciones inicializado');
    }

    /**
     * Iniciar polling para verificar nuevas notificaciones
     */
    startPolling() {
        // Verificar inmediatamente
        this.verificarNuevasNotificaciones();

        // Configurar intervalo
        this.pollingInterval = setInterval(() => {
            this.verificarNuevasNotificaciones();
        }, this.pollingFrequency);

        console.log(`✅ Polling iniciado cada ${this.pollingFrequency / 1000} segundos`);
    }

    /**
     * Detener polling
     */
    stopPolling() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
            this.pollingInterval = null;
            console.log('🛑 Polling detenido');
        }
    }

    /**
     * Verificar si hay nuevas notificaciones
     */
    async verificarNuevasNotificaciones() {
        try {
            const response = await fetch('/Notificaciones/GetNotificaciones?soloNoLeidas=true&cantidad=5');

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const notificaciones = await response.json();

            // Actualizar contador
            this.actualizarContador(notificaciones.length);

            // Mostrar pop-ups solo para notificaciones nuevas
            notificaciones.forEach(n => {
                if (n.mostrarPopup && !this.shownPopups.has(n.id)) {
                    this.mostrarPopup(n);
                    this.shownPopups.add(n.id);
                }
            });

            // Si el panel está abierto, actualizar lista
            if (this.panelOpen) {
                this.cargarNotificaciones();
            }

        } catch (error) {
            console.error('❌ Error al verificar notificaciones:', error);
        }
    }

    /**
     * Cargar contador de notificaciones no leídas
     */
    async cargarContadorNotificaciones() {
        try {
            const response = await fetch('/Notificaciones/GetContadorNoLeidas');

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const data = await response.json();
            this.actualizarContador(data.count);

        } catch (error) {
            console.error('❌ Error al cargar contador:', error);
        }
    }

    /**
     * Actualizar badge del contador
     */
    actualizarContador(count) {
        const bellButton = document.querySelector('.bell-button');
        if (!bellButton) return;

        let badge = bellButton.querySelector('.notification-badge');

        if (count > 0) {
            if (!badge) {
                badge = document.createElement('span');
                badge.className = 'notification-badge';
                bellButton.appendChild(badge);
            }
            badge.textContent = count > 99 ? '99+' : count;
        } else {
            if (badge) {
                badge.remove();
            }
        }
    }

    /**
     * Cargar notificaciones en el panel
     */
    async cargarNotificaciones() {
        const list = document.getElementById('notificationList');
        if (!list) return;

        list.innerHTML = `
            <div class="notification-loading">
                <i class="fa-solid fa-spinner fa-spin"></i>
                <span>Cargando notificaciones...</span>
            </div>
        `;

        try {
            const response = await fetch('/Notificaciones/GetNotificaciones?cantidad=20');

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const notificaciones = await response.json();
            this.currentNotifications = notificaciones;

            if (notificaciones.length === 0) {
                list.innerHTML = `
                    <div class="notification-empty">
                        <i class="fa-solid fa-bell-slash"></i>
                        <p>No tienes notificaciones</p>
                    </div>
                `;
                return;
            }

            list.innerHTML = '';

            notificaciones.forEach(n => {
                const item = this.crearItemNotificacion(n);
                list.appendChild(item);
            });

        } catch (error) {
            console.error('❌ Error al cargar notificaciones:', error);
            list.innerHTML = `
                <div class="notification-empty">
                    <p style="color: #EF4444;">Error al cargar notificaciones</p>
                </div>
            `;
        }
    }

    /**
     * Crear elemento HTML para un item de notificación
     */
    crearItemNotificacion(notificacion) {
        const item = document.createElement('div');
        item.className = `notification-item ${!notificacion.leida ? 'unread' : ''}`;
        item.dataset.id = notificacion.id;

        const tiempoTranscurrido = this.calcularTiempoTranscurrido(notificacion.fechaCreacion);

        item.innerHTML = `
            <div class="notification-content">
                <div class="notification-icon ${notificacion.color}">
                    <i class="fa-solid fa-${notificacion.icono}"></i>
                </div>
                <div class="notification-body">
                    <div class="notification-title">${notificacion.titulo}</div>
                    <div class="notification-message">${notificacion.mensaje}</div>
                    <div class="notification-time">
                        <i class="fa-regular fa-clock"></i>
                        ${tiempoTranscurrido}
                    </div>
                    ${notificacion.urlAccion ? `
                        <div class="notification-actions">
                            <button class="notification-btn notification-btn-primary" onclick="window.notificationManager.accionarNotificacion(${notificacion.id}, '${notificacion.urlAccion}')">
                                ${notificacion.textoAccion || 'Ver más'}
                            </button>
                        </div>
                    ` : ''}
                </div>
            </div>
        `;

        // Click en la notificación
        item.addEventListener('click', (e) => {
            // Solo si no hizo click en el botón de acción
            if (!e.target.classList.contains('notification-btn')) {
                this.marcarComoLeida(notificacion.id);
                if (notificacion.urlAccion) {
                    window.location.href = notificacion.urlAccion;
                }
            }
        });

        return item;
    }

    /**
     * Mostrar pop-up de notificación
     */
    mostrarPopup(notificacion) {
        const container = document.getElementById('notificationPopupContainer');
        if (!container) {
            console.warn('⚠️ Contenedor de pop-ups no encontrado');
            return;
        }

        const popup = document.createElement('div');
        popup.className = `notification-popup ${notificacion.color}`;
        popup.dataset.id = notificacion.id;

        popup.innerHTML = `
            <div class="popup-icon ${notificacion.color}">
                <i class="fa-solid fa-${notificacion.icono}"></i>
            </div>
            <div class="popup-content">
                <div class="popup-title">${notificacion.titulo}</div>
                <div class="popup-message">${notificacion.mensaje}</div>
                ${notificacion.urlAccion ? `
                    <a href="${notificacion.urlAccion}" class="popup-action" onclick="window.notificationManager.marcarComoLeida(${notificacion.id})">
                        <i class="fa-solid fa-arrow-right"></i>
                        ${notificacion.textoAccion || 'Ver más'}
                    </a>
                ` : ''}
            </div>
            <button class="popup-close" onclick="window.notificationManager.cerrarPopup(this)">
                <i class="fa-solid fa-xmark"></i>
            </button>
            <div class="popup-progress"></div>
        `;

        container.appendChild(popup);

        // Animar entrada
        setTimeout(() => {
            popup.classList.add('show');
        }, 100);

        // Auto-cerrar después de 5 segundos
        setTimeout(() => {
            this.cerrarPopup(popup.querySelector('.popup-close'));
        }, 5000);

        console.log(`✅ Pop-up mostrado: ${notificacion.titulo}`);
    }

    /**
     * Cerrar pop-up
     */
    cerrarPopup(button) {
        const popup = button.closest('.notification-popup');
        if (!popup) return;

        popup.classList.remove('show');

        setTimeout(() => {
            popup.remove();
        }, 400);
    }

    /**
     * Accionar notificación (hacer clic en botón de acción)
     */
    async accionarNotificacion(id, url) {
        await this.marcarComoLeida(id);
        window.location.href = url;
    }

    /**
     * Marcar notificación como leída
     */
    async marcarComoLeida(id) {
        try {
            const response = await fetch(`/Notificaciones/MarcarComoLeida?id=${id}`, {
                method: 'POST'
            });

            if (response.ok) {
                // Actualizar UI
                const item = document.querySelector(`.notification-item[data-id="${id}"]`);
                if (item) {
                    item.classList.remove('unread');
                }

                // Actualizar contador
                await this.cargarContadorNotificaciones();

                console.log(`✅ Notificación ${id} marcada como leída`);
            }

        } catch (error) {
            console.error(`❌ Error al marcar notificación ${id} como leída:`, error);
        }
    }

    /**
     * Marcar todas las notificaciones como leídas
     */
    async marcarTodasComoLeidas() {
        try {
            const response = await fetch('/Notificaciones/MarcarTodasComoLeidas', {
                method: 'POST'
            });

            if (response.ok) {
                // Actualizar UI
                document.querySelectorAll('.notification-item.unread').forEach(item => {
                    item.classList.remove('unread');
                });

                // Actualizar contador
                this.actualizarContador(0);

                console.log('✅ Todas las notificaciones marcadas como leídas');
            }

        } catch (error) {
            console.error('❌ Error al marcar todas como leídas:', error);
        }
    }

    /**
     * Calcular tiempo transcurrido desde la creación
     */
    calcularTiempoTranscurrido(fechaCreacion) {
        const ahora = new Date();
        const fecha = new Date(fechaCreacion);
        const diff = Math.floor((ahora - fecha) / 1000); // Segundos

        if (diff < 60) return 'Hace unos segundos';
        if (diff < 3600) return `Hace ${Math.floor(diff / 60)} min`;
        if (diff < 86400) return `Hace ${Math.floor(diff / 3600)} h`;
        if (diff < 604800) return `Hace ${Math.floor(diff / 86400)} días`;
        
        return fecha.toLocaleDateString('es-ES', { day: '2-digit', month: 'short' });
    }

    /**
     * Configurar event listeners
     */
    setupEventListeners() {
        // Cerrar panel al hacer clic fuera
        document.addEventListener('click', (e) => {
            const panel = document.getElementById('notificationPanel');
            const bell = document.getElementById('notificationBell');

            if (panel && bell && !bell.contains(e.target) && !panel.contains(e.target)) {
                panel.classList.remove('show');
                this.panelOpen = false;
            }
        });
    }
}

// ==========================================
// FUNCIONES GLOBALES
// ==========================================

/**
 * Toggle del panel de notificaciones
 */
function toggleNotificationPanel() {
    // ⭐ VERIFICAR QUE notificationManager EXISTE
    if (!window.notificationManager) {
        console.error('❌ NotificationManager no está inicializado');
        return;
    }

    const panel = document.getElementById('notificationPanel');
    
    if (!panel) {
        console.warn('⚠️ Panel de notificaciones no encontrado');
        return;
    }

    const isOpen = panel.classList.contains('show');

    if (isOpen) {
        panel.classList.remove('show');
        window.notificationManager.panelOpen = false;
    } else {
        panel.style.display = 'block';
        setTimeout(() => {
            panel.classList.add('show');
        }, 10);
        window.notificationManager.panelOpen = true;

        // Cargar notificaciones
        window.notificationManager.cargarNotificaciones();
    }
}

/**
 * Marcar todas las notificaciones como leídas
 */
function marcarTodasComoLeidas() {
    if (window.notificationManager) {
        window.notificationManager.marcarTodasComoLeidas();
    }
}

/**
 * Ver todas las notificaciones (redirigir a página completa)
 */
function verTodasLasNotificaciones() {
    // TODO: Implementar página de notificaciones completa
    console.log('📋 Ver todas las notificaciones');
    alert('Función en desarrollo: Ver todas las notificaciones');
}

// ==========================================
// INICIALIZACIÓN
// ==========================================

// ⭐ INICIALIZAR CORRECTAMENTE AL CARGAR LA PÁGINA
document.addEventListener('DOMContentLoaded', () => {
    window.notificationManager = new NotificationManager();
    console.log('✅ NotificationManager expuesto globalmente');
});

// Exponer funciones globalmente
window.toggleNotificationPanel = toggleNotificationPanel;
window.marcarTodasComoLeidas = marcarTodasComoLeidas;
window.verTodasLasNotificaciones = verTodasLasNotificaciones;