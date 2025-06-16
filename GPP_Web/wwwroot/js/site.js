document.addEventListener('DOMContentLoaded', function () {
    const darkModeSwitch = document.getElementById('darkModeSwitch');
    const darkModeSwitchMobile = document.getElementById('darkModeSwitchMobile');
    const body = document.body;

    // Función para aplicar o remover la clase 'light-mode'
    function toggleLightMode(isLightMode) {
        if (isLightMode) {
            body.classList.add('light-mode');
        } else {
            body.classList.remove('light-mode');
        }
    }

    // Cargar el estado guardado del modo oscuro
    const savedLightMode = localStorage.getItem('lightMode');
    if (savedLightMode === 'enabled') {
        toggleLightMode(true);
        if (darkModeSwitch) {
            darkModeSwitch.checked = true;
        }
        if (darkModeSwitchMobile) {
            darkModeSwitchMobile.checked = true;
        }
    }

    // Event listener para el switch del sidebar de escritorio
    if (darkModeSwitch) {
        darkModeSwitch.addEventListener('change', function () {
            if (this.checked) {
                toggleLightMode(true);
                localStorage.setItem('lightMode', 'enabled');
                // Si el switch móvil existe, sincronizar su estado
                if (darkModeSwitchMobile) {
                    darkModeSwitchMobile.checked = true;
                }
            } else {
                toggleLightMode(false);
                localStorage.setItem('lightMode', 'disabled');
                // Si el switch móvil existe, sincronizar su estado
                if (darkModeSwitchMobile) {
                    darkModeSwitchMobile.checked = false;
                }
            }
        });
    }

    // Event listener para el switch del sidebar móvil
    if (darkModeSwitchMobile) {
        darkModeSwitchMobile.addEventListener('change', function () {
            if (this.checked) {
                toggleLightMode(true);
                localStorage.setItem('lightMode', 'enabled');
                // Si el switch de escritorio existe, sincronizar su estado
                if (darkModeSwitch) {
                    darkModeSwitch.checked = true;
                }
            } else {
                toggleLightMode(false);
                localStorage.setItem('lightMode', 'disabled');
                // Si el switch de escritorio existe, sincronizar su estado
                if (darkModeSwitch) {
                    darkModeSwitch.checked = false;
                }
            }
        });
    }
});