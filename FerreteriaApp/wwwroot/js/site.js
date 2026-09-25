// Búsqueda por voz con Web Speech API (en Chrome usa el reconocimiento de Google)
(function () {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) return;

    document.querySelectorAll('.btn-voz').forEach(function (boton) {
        const form = boton.closest('form');
        const input = form.querySelector('input[name="buscar"]');
        const icono = boton.querySelector('i');
        let escuchando = false;

        const reconocimiento = new SpeechRecognition();
        reconocimiento.lang = 'es-ES';
        reconocimiento.interimResults = true;
        reconocimiento.maxAlternatives = 1;

        boton.classList.remove('d-none');

        reconocimiento.onstart = function () {
            escuchando = true;
            icono.className = 'bi bi-mic-fill text-danger';
            input.placeholder = 'Escuchando...';
        };

        reconocimiento.onresult = function (e) {
            let texto = '';
            for (let i = 0; i < e.results.length; i++) {
                texto += e.results[i][0].transcript;
            }
            input.value = texto.trim().replace(/[.,]$/, '');
            if (e.results[e.results.length - 1].isFinal && input.value) {
                form.submit();
            }
        };

        reconocimiento.onerror = function (e) {
            if (e.error === 'not-allowed' || e.error === 'service-not-allowed') {
                alert('Debe permitir el acceso al micrófono para buscar por voz.');
            }
        };

        reconocimiento.onend = function () {
            escuchando = false;
            icono.className = 'bi bi-mic';
            input.placeholder = input.dataset.placeholder;
        };

        input.dataset.placeholder = input.placeholder;

        boton.addEventListener('click', function () {
            if (escuchando) {
                reconocimiento.stop();
            } else {
                reconocimiento.start();
            }
        });
    });
})();

// PWA: registra el service worker y maneja el botón "Instalar app" de la barra
(function () {
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js');
        });
    }

    const item = document.getElementById('itemInstalar');
    const boton = document.getElementById('btnInstalar');
    if (!item || !boton) return;

    // Si ya se abrió desde el ícono de la pantalla de inicio, no hace falta el botón
    const yaInstalada = window.matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
    if (yaInstalada) return;

    // Chrome / Edge / Android: el navegador avisa que la app se puede instalar
    let aviso = null;
    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault(); // en vez del cartel automático, usamos nuestro botón
        aviso = e;
        item.classList.remove('d-none');
    });

    window.addEventListener('appinstalled', function () {
        item.classList.add('d-none');
        aviso = null;
    });

    // iPhone / iPad (Safari): no existe ese aviso, se agrega a mano desde el menú Compartir
    const esIOS = /iphone|ipad|ipod/i.test(navigator.userAgent) ||
                  (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    if (esIOS) item.classList.remove('d-none');

    boton.addEventListener('click', function () {
        if (aviso) {
            aviso.prompt();
            aviso.userChoice.then(function () { aviso = null; item.classList.add('d-none'); });
        } else if (esIOS) {
            alert('Para instalar la app: tocá el botón Compartir (el cuadrado con la flecha hacia arriba) ' +
                  'y elegí "Agregar a pantalla de inicio".');
        }
    });
})();
