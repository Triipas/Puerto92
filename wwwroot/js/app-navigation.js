/**
 * Sistema de Navegación SPA-like para Puerto 92
 * Carga dinámica de contenido sin recargar la página
 */

class AppNavigation {
    constructor() {
        this.contentContainer = document.getElementById('main-content-area');
        this.cache = new Map(); // Caché de contenido cargado
        this.currentUrl = window.location.pathname;
        this.isLoading = false;
        
        this.init();
    }

    init() {
        this.setupNavigationListeners();
        this.setupHistoryManagement();
        this.setupFormInterception();
        
        // Guardar contenido inicial en caché
        if (this.contentContainer) {
            this.cache.set(this.currentUrl, this.contentContainer.innerHTML);
        }
    }

    /**
     * Configurar listeners para la navegación
     */
    setupNavigationListeners() {
        // Interceptar clics en enlaces del sidebar
        document.querySelectorAll('.nav-link[data-navigate]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const url = link.getAttribute('href');
                
                // Actualizar estado activo
                document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
                link.classList.add('active');
                
                this.navigateTo(url);
            });
        });

        // Interceptar clics en enlaces internos del contenido
        document.addEventListener('click', (e) => {
            const link = e.target.closest('a[data-ajax-link]');
            if (link && !link.hasAttribute('data-modal')) {
                e.preventDefault();
                this.navigateTo(link.href);
            }
        });
    }

    /**
     * Configurar manejo del historial del navegador
     */
    setupHistoryManagement() {
        window.addEventListener('popstate', (e) => {
            if (e.state && e.state.url) {
                this.loadContent(e.state.url, false);
            }
        });
    }

    /**
     * Interceptar envío de formularios para AJAX
     */
    setupFormInterception() {
        document.addEventListener('submit', (e) => {
            const form = e.target;
            
            // Solo interceptar formularios con data-ajax-form
            if (form.hasAttribute('data-ajax-form')) {
                e.preventDefault();
                this.submitForm(form);
            }
        });
    }

    /**
     * Navegar a una URL
     */
    async navigateTo(url, addToHistory = true) {
        if (this.isLoading || url === this.currentUrl) return;

        this.showLoadingState();

        try {
            await this.loadContent(url, addToHistory);
            this.currentUrl = url;
        } catch (error) {
            console.error('Error al navegar:', error);
            this.showError('Error al cargar el contenido');
        } finally {
            this.hideLoadingState();
        }
    }

    /**
     * Cargar contenido de una URL
     */
    async loadContent(url, addToHistory = true) {
        // Verificar caché
        if (this.cache.has(url)) {
            this.updateContent(this.cache.get(url));
            if (addToHistory) {
                this.updateHistory(url);
            }
            return;
        }

        // Hacer petición AJAX
        const response = await fetch(url, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const html = await response.text();
        
        // Guardar en caché
        this.cache.set(url, html);
        
        // Actualizar contenido
        this.updateContent(html);
        
        // Actualizar historial
        if (addToHistory) {
            this.updateHistory(url);
        }

        // Scroll al inicio
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    /**
     * Actualizar el contenido del contenedor
     */
    updateContent(html) {
        if (!this.contentContainer) return;

        // Animación de salida
        this.contentContainer.style.opacity = '0';
        this.contentContainer.style.transform = 'translateY(10px)';

        setTimeout(() => {
            this.contentContainer.innerHTML = html;
            
            // Recargar scripts de la página
            this.reloadScripts();
            
            // Animación de entrada
            setTimeout(() => {
                this.contentContainer.style.opacity = '1';
                this.contentContainer.style.transform = 'translateY(0)';
            }, 50);
        }, 200);
    }

    /**
     * Actualizar el historial del navegador
     */
    updateHistory(url) {
        window.history.pushState({ url }, '', url);
    }

    /**
     * Recargar scripts después de actualizar contenido
     */
    reloadScripts() {
        // Buscar scripts en el nuevo contenido
        const scripts = this.contentContainer.querySelectorAll('script');
        
        scripts.forEach(oldScript => {
            const newScript = document.createElement('script');
            Array.from(oldScript.attributes).forEach(attr => {
                newScript.setAttribute(attr.name, attr.value);
            });
            newScript.textContent = oldScript.textContent;
            oldScript.parentNode.replaceChild(newScript, oldScript);
        });

        // Reinicializar funciones específicas de página
        this.initPageSpecificScripts();
    }

    /**
     * Inicializar scripts específicos de cada página
     */
    initPageSpecificScripts() {
        const currentPath = window.location.pathname.toLowerCase();
        
        console.log('🔄 Inicializando scripts para:', currentPath);
        
        // Página de Usuarios
        if (currentPath.includes('/usuarios')) {
            if (typeof window.initUsuariosPage === 'function') {
                console.log('✅ Inicializando scripts de usuarios');
                window.initUsuariosPage();
            }
        }
        
        // Página de Locales
        if (currentPath.includes('/locales')) {
            if (typeof window.initLocalesPage === 'function') {
                console.log('✅ Inicializando scripts de locales');
                window.initLocalesPage();
            }
        }
        // 🆕 Página de Utensilios (Contador)
if (currentPath.includes('/utensilios')) {
    if (typeof window.initCatalogoUtensilios === 'function') {
        console.log('✅ Inicializando scripts de catálogo de utensilios');
        window.initCatalogoUtensilios();
    }
}

// 🆕 Página de Productos (Supervisora de Calidad)
 if (currentPath.includes('/productos')) {
    if (typeof window.initCatalogoProductos === 'function') {
        console.log('✅ Inicializando scripts de catálogo de productos');
        window.initCatalogoProductos();
    }
}

// 🆕 Página de Proveedores (Supervisora de Calidad)
if (currentPath.includes('/proveedores')) {
    if (typeof window.initGestionProveedores === 'function') {
        console.log('✅ Inicializando scripts de gestión de proveedores');
        window.initGestionProveedores();
    }
}
        
        // Funciones genéricas
        if (typeof setupSearch === 'function') setupSearch();
        if (typeof setupModalEventListeners === 'function') setupModalEventListeners();
    }

    /**
     * Enviar formulario via AJAX
     */
    async submitForm(form) {
        this.showLoadingState();

        try {
            const formData = new FormData(form);
            const response = await fetch(form.action, {
                method: form.method,
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (response.ok) {
                const result = await response.json();
                
                if (result.success) {
                    if (result.redirectUrl) {
                        await this.navigateTo(result.redirectUrl);
                    } else {
                        // Recargar contenido actual
                        this.cache.delete(this.currentUrl);
                        await this.loadContent(this.currentUrl, false);
                    }
                    
                    if (result.message) {
                        this.showNotification(result.message, 'success');
                    }
                } else {
                    this.showNotification(result.message || 'Error al procesar', 'error');
                }
            }
        } catch (error) {
            console.error('Error al enviar formulario:', error);
            this.showError('Error al procesar el formulario');
        } finally {
            this.hideLoadingState();
        }
    }

    /**
     * Mostrar estado de carga
     */
    showLoadingState() {
        this.isLoading = true;
        
        // Crear overlay de carga si no existe
        let loader = document.getElementById('page-loader');
        if (!loader) {
            loader = document.createElement('div');
            loader.id = 'page-loader';
            loader.innerHTML = `
                <div class="loader-spinner">
                    <i class="fa-solid fa-spinner fa-spin"></i>
                    <span>Cargando...</span>
                </div>
            `;
            document.body.appendChild(loader);
        }
        
        loader.classList.add('active');
    }

    /**
     * Ocultar estado de carga
     */
    hideLoadingState() {
        this.isLoading = false;
        const loader = document.getElementById('page-loader');
        if (loader) {
            loader.classList.remove('active');
        }
    }

    /**
     * Mostrar notificación
     */
    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `app-notification ${type}`;
        notification.innerHTML = `
            <i class="fa-solid fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
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

    /**
     * Mostrar error
     */
    showError(message) {
        this.showNotification(message, 'error');
    }

    /**
     * Limpiar caché (útil después de crear/editar/eliminar)
     */
    clearCache() {
        this.cache.clear();
    }

    /**
     * Recargar página actual
     */
    async reload() {
        this.cache.delete(this.currentUrl);
        await this.loadContent(this.currentUrl, false);
    }
}

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
    window.appNavigation = new AppNavigation();
});

// Exponer funciones útiles globalmente
window.reloadCurrentPage = () => window.appNavigation?.reload();
window.clearNavigationCache = () => window.appNavigation?.clearCache();