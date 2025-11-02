/**
 * Modal Manager - Asegura que los modales estén al nivel correcto del DOM
 */

(function() {
    'use strict';

    /**
     * Inicializar el gestor de modales
     */
    function initModalManager() {
        console.log('🎭 Inicializando Modal Manager...');
        
        // Esperar a que el DOM esté completamente cargado
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', setupModals);
        } else {
            setupModals();
        }
    }

    /**
     * Configurar modales
     */
    function setupModals() {
        ensureModalsContainer();
        moveModalsToGlobalContainer();
        setupModalObserver();
        console.log('✅ Modal Manager inicializado');
    }

    /**
     * Asegurar que existe el contenedor de modales
     */
    function ensureModalsContainer() {
        let modalsContainer = document.getElementById('modals-container');
        
        if (!modalsContainer) {
            console.log('📦 Creando contenedor de modales...');
            modalsContainer = document.createElement('div');
            modalsContainer.id = 'modals-container';
            document.body.appendChild(modalsContainer);
            console.log('✅ Contenedor de modales creado');
        }
        
        return modalsContainer;
    }

    /**
     * Mover todos los modales al contenedor global
     */
    function moveModalsToGlobalContainer() {
        const modalsContainer = document.getElementById('modals-container');
        if (!modalsContainer) {
            console.error('❌ No se encontró el contenedor de modales');
            return;
        }

        // Buscar todos los modales en el DOM
        const modals = document.querySelectorAll('.modal-overlay');
        
        if (modals.length === 0) {
            console.log('ℹ️ No se encontraron modales para mover');
            return;
        }

        console.log(`📦 Encontrados ${modals.length} modal(es)...`);
        
        let movedCount = 0;
        modals.forEach(modal => {
            // Verificar si el modal ya está en el contenedor correcto
            if (modal.parentElement !== modalsContainer) {
                // Mover el modal al contenedor global
                modalsContainer.appendChild(modal);
                movedCount++;
                console.log(`✅ Modal movido: ${modal.id || 'sin-id'}`);
            }
        });

        if (movedCount > 0) {
            console.log(`✅ ${movedCount} modal(es) movido(s) al contenedor global`);
        } else {
            console.log('✅ Todos los modales ya están en el contenedor correcto');
        }
    }

    /**
     * Observar cambios en el DOM para detectar nuevos modales
     */
    function setupModalObserver() {
        const observer = new MutationObserver((mutations) => {
            let hasNewModals = false;
            
            mutations.forEach((mutation) => {
                mutation.addedNodes.forEach((node) => {
                    if (node.nodeType === 1) {
                        if (node.classList?.contains('modal-overlay') ||
                            node.querySelector?.('.modal-overlay')) {
                            hasNewModals = true;
                        }
                    }
                });
            });
            
            if (hasNewModals) {
                console.log('🔍 Nuevos modales detectados, moviendo...');
                setTimeout(() => {
                    moveModalsToGlobalContainer();
                }, 100);
            }
        });

        // Observar cambios en el body
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    /**
     * Reinicializar después de carga dinámica
     */
    function reinitializeModals() {
        console.log('🔄 Reinicializando modales...');
        setTimeout(() => {
            moveModalsToGlobalContainer();
        }, 100);
    }

    // Inicializar
    initModalManager();

    // Exponer funciones globalmente
    window.reinitializeModals = reinitializeModals;
    window.moveModalsToGlobalContainer = moveModalsToGlobalContainer;

    console.log('✅ Modal Manager cargado');

})();