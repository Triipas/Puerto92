/**
 * Modal Manager - Asegura que los modales estén al nivel correcto del DOM
 * 
 * Este script mueve todos los modales fuera del contenedor principal
 * para que el overlay cubra toda la pantalla incluyendo el sidebar
 */

(function() {
    'use strict';

    /**
     * Inicializar el gestor de modales
     */
    function initModalManager() {
        console.log('🎭 Inicializando Modal Manager...');
        
        // Crear contenedor de modales si no existe
        ensureModalsContainer();
        
        // Mover todos los modales al contenedor global
        moveModalsToGlobalContainer();
        
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
            modalsContainer.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                pointer-events: none;
                z-index: 99999;
            `;
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
        if (!modalsContainer) return;

        // Buscar todos los modales en el DOM
        const modals = document.querySelectorAll('.modal-overlay');
        
        if (modals.length === 0) {
            console.log('ℹ️ No se encontraron modales para mover');
            return;
        }

        console.log(`📦 Moviendo ${modals.length} modal(es) al contenedor global...`);
        
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
            console.log(`✅ ${movedCount} modal(es) movido(s) correctamente`);
        } else {
            console.log('✅ Todos los modales ya están en el contenedor correcto');
        }
    }

    /**
     * Reinicializar después de carga dinámica de contenido
     */
    function reinitializeModals() {
        console.log('🔄 Reinicializando modales después de carga dinámica...');
        
        // Esperar un momento para que el DOM se actualice
        setTimeout(() => {
            moveModalsToGlobalContainer();
        }, 100);
    }

    // Inicializar cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initModalManager);
    } else {
        initModalManager();
    }

    // Observar cambios en el DOM para detectar nuevos modales
    const observer = new MutationObserver((mutations) => {
        let shouldReinitialize = false;
        
        mutations.forEach((mutation) => {
            mutation.addedNodes.forEach((node) => {
                if (node.nodeType === 1 && (
                    node.classList?.contains('modal-overlay') ||
                    node.querySelector?.('.modal-overlay')
                )) {
                    shouldReinitialize = true;
                }
            });
        });
        
        if (shouldReinitialize) {
            console.log('🔍 Nuevos modales detectados en el DOM');
            reinitializeModals();
        }
    });

    // Observar el body completo
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    // Exponer función para reinicializar manualmente
    window.reinitializeModals = reinitializeModals;
    window.moveModalsToGlobalContainer = moveModalsToGlobalContainer;

    console.log('✅ Modal Manager cargado y activo');

})();