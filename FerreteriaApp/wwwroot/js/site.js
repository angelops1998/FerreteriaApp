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

// Asistente por voz: botón flotante abajo a la derecha. Se presiona y se dice una orden,
// por ejemplo "mostrame el carrito", "abrí el catálogo", "buscá taladros" o "volver".
(function () {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    const contenedor = document.getElementById('asistenteVoz');
    if (!SpeechRecognition || !contenedor) return;

    const boton = document.getElementById('asistenteVozBtn');
    const globo = document.getElementById('asistenteVozGlobo');
    const esPersonal = contenedor.dataset.personal === '1';
    const esCliente = contenedor.dataset.cliente === '1';
    let escuchando = false;
    let ocultarGlobo = null;

    contenedor.classList.remove('d-none');

    // Pasa a minúsculas y quita los acentos para comparar más fácil ("catálogo" -> "catalogo")
    function normalizar(texto) {
        return texto.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[.,¿?¡!]/g, '').trim();
    }

    function mostrar(mensaje, segundos) {
        clearTimeout(ocultarGlobo);
        globo.textContent = mensaje;
        globo.classList.remove('d-none');
        if (segundos) ocultarGlobo = setTimeout(function () { globo.classList.add('d-none'); }, segundos * 1000);
    }

    // Respuesta hablada (si el navegador lo permite)
    function decir(mensaje) {
        mostrar(mensaje, 4);
        if (!window.speechSynthesis) return;
        const voz = new SpeechSynthesisUtterance(mensaje);
        voz.lang = 'es-ES';
        speechSynthesis.cancel();
        speechSynthesis.speak(voz);
    }

    // Busca en el catálogo pasando las palabras a singular ("taladros" -> "taladro"),
    // porque el buscador compara por texto y los productos están cargados en singular
    function buscar(texto) {
        const singular = texto.split(' ').map(function (p) { return p.length > 4 ? p.replace(/s$/, '') : p; }).join(' ');
        ir('/Productos?buscar=' + encodeURIComponent(singular), 'Buscando ' + texto);
    }

    function ir(url, mensaje) {
        decir(mensaje);
        setTimeout(function () { window.location.href = url; }, 700);
    }

    // Lista de comandos: si la frase contiene alguna de las palabras, se ejecuta la acción.
    // Van de lo más específico a lo más general.
    const comandos = [
        { palabras: ['agrega', 'agregar', 'anadi', 'anadir', 'sumar al carrito'], accion: function () {
            const form = document.querySelector('main form[action$="/Carrito/Agregar"]');
            if (!form) return decir('Primero abrí un producto para agregarlo al carrito');
            decir('Agregando al carrito');
            form.submit();
        } },
        { palabras: ['carrito'], permitido: !esPersonal, url: '/Carrito', mensaje: 'Abriendo el carrito' },
        { palabras: ['mis compras', 'mis pedidos'], permitido: esCliente, url: '/Ventas/MisCompras', mensaje: 'Mostrando tus compras' },
        { palabras: ['perfil', 'mi cuenta'], permitido: esPersonal || esCliente, url: '/Account/Perfil', mensaje: 'Abriendo tu perfil' },
        { palabras: ['reporte'], permitido: esPersonal, url: '/Reportes', mensaje: 'Abriendo los reportes' },
        { palabras: ['inventario', 'stock'], permitido: esPersonal, url: '/Inventario', mensaje: 'Abriendo el inventario' },
        { palabras: ['registrar venta', 'nueva venta'], permitido: esPersonal, url: '/Ventas/Registrar', mensaje: 'Registrando una venta' },
        { palabras: ['ventas'], permitido: esPersonal, url: '/Ventas/Manage', mensaje: 'Mostrando las ventas' },
        { palabras: ['compras'], permitido: esPersonal, url: '/Compras', mensaje: 'Mostrando las compras a proveedores' },
        { palabras: ['panel'], permitido: esPersonal, url: '/Admin', mensaje: 'Abriendo el panel general' },
        { palabras: ['iniciar sesion', 'ingresar', 'login'], permitido: !esPersonal && !esCliente, url: '/Account/Login', mensaje: 'Abriendo el inicio de sesión' },
        { palabras: ['registrarme', 'crear cuenta', 'registro'], permitido: !esPersonal && !esCliente, url: '/Account/Register', mensaje: 'Abriendo el registro' },
        { palabras: ['catalogo', 'productos'], url: '/Productos', mensaje: 'Mostrando el catálogo' },
        { palabras: ['inicio', 'principal', 'home'], url: '/', mensaje: 'Volviendo al inicio' },
        { palabras: ['volver', 'atras', 'regresar'], accion: function () { decir('Volviendo'); setTimeout(function () { history.back(); }, 500); } },
        { palabras: ['baja', 'bajar', 'abajo'], accion: function () { decir('Bajando'); window.scrollBy({ top: window.innerHeight * 0.8, behavior: 'smooth' }); } },
        { palabras: ['sube', 'subir', 'arriba'], accion: function () { decir('Subiendo'); window.scrollTo({ top: 0, behavior: 'smooth' }); } }
    ];

    function ejecutar(frase) {
        const texto = normalizar(frase);

        // "buscá martillos" / "buscar taladro" -> búsqueda en el catálogo
        const busqueda = texto.match(/^(busca|buscar|buscame)\s+(.+)$/);
        if (busqueda) {
            return buscar(busqueda[2]);
        }

        const comando = comandos.find(function (c) {
            return c.permitido !== false && c.palabras.some(function (p) { return texto.includes(p); });
        });
        if (comando) {
            return comando.accion ? comando.accion() : ir(comando.url, comando.mensaje);
        }

        // "mostrame taladros" (no es una sección conocida) -> se busca como producto
        const pedido = texto.match(/^(mostrame|muestrame|mostrar|quiero ver|ver)\s+(los |las |el |la |unos |unas )?(.+)$/);
        if (pedido) {
            return buscar(pedido[3]);
        }

        decir('No entendí "' + frase + '". Probá con "mostrame el carrito" o "buscá martillos"');
    }

    const reconocimiento = new SpeechRecognition();
    reconocimiento.lang = 'es-ES';
    reconocimiento.interimResults = true;
    reconocimiento.maxAlternatives = 1;

    reconocimiento.onstart = function () {
        escuchando = true;
        boton.classList.add('escuchando');
        mostrar('Escuchando... decí por ejemplo "mostrame el carrito"');
    };

    reconocimiento.onresult = function (e) {
        const resultado = e.results[e.results.length - 1];
        const frase = resultado[0].transcript.trim();
        if (resultado.isFinal) {
            ejecutar(frase);
        } else {
            mostrar('"' + frase + '"');
        }
    };

    reconocimiento.onerror = function (e) {
        if (e.error === 'not-allowed' || e.error === 'service-not-allowed') {
            mostrar('Debe permitir el acceso al micrófono para usar el asistente.', 5);
        } else if (e.error === 'no-speech') {
            mostrar('No escuché nada, volvé a intentar.', 3);
        }
    };

    reconocimiento.onend = function () {
        escuchando = false;
        boton.classList.remove('escuchando');
    };

    boton.addEventListener('click', function () {
        if (escuchando) {
            reconocimiento.stop();
        } else {
            reconocimiento.start();
        }
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
