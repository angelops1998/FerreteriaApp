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
