// Service worker de la PWA: permite instalar la app y que abra aunque no haya internet.
// - Archivos estáticos (css, js, íconos, imágenes): se guardan en caché y se sirven desde ahí.
// - Páginas: siempre se piden al servidor (tienen datos del usuario, precios y stock al día);
//   si no hay conexión se muestra la página offline.html.
// Al cambiar algo de este archivo, subir la versión para que los celulares tomen la nueva.
const VERSION = 'ferreteria-v1';

const PRECARGA = [
    '/offline.html',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/css/site.css',
    '/js/site.js',
    '/icons/icon-192.png',
    '/images/default-product.png'
];

self.addEventListener('install', function (e) {
    e.waitUntil(caches.open(VERSION).then(function (cache) { return cache.addAll(PRECARGA); }));
    self.skipWaiting();
});

// Borra las cachés de versiones anteriores
self.addEventListener('activate', function (e) {
    e.waitUntil(
        caches.keys().then(function (claves) {
            return Promise.all(claves.filter(function (c) { return c !== VERSION; })
                                     .map(function (c) { return caches.delete(c); }));
        }).then(function () { return self.clients.claim(); })
    );
});

self.addEventListener('fetch', function (e) {
    const req = e.request;
    if (req.method !== 'GET') return; // formularios (POST) van directo al servidor

    // Navegación a una página: red primero, y si falla la página offline
    if (req.mode === 'navigate') {
        e.respondWith(fetch(req).catch(function () { return caches.match('/offline.html'); }));
        return;
    }

    // Estáticos: se responde con la copia guardada (rápido) y en segundo plano se descarga
    // la versión nueva para la próxima vez. site.css?v=abc123 cambia de "v" al modificar el archivo,
    // así que un cambio en css/js se ve enseguida.
    const url = new URL(req.url);
    const esEstatico = /\.(css|js|png|jpe?g|webp|gif|svg|ico|woff2?)$/i.test(url.pathname);
    if (!esEstatico) return;

    e.respondWith(
        caches.match(req).then(function (enCache) {
            const deRed = fetch(req).then(function (resp) {
                if (resp.ok || resp.type === 'opaque') {
                    const copia = resp.clone();
                    caches.open(VERSION).then(function (cache) { cache.put(req, copia); });
                }
                return resp;
            }).catch(function () { return enCache; });
            return enCache || deRed;
        })
    );
});
