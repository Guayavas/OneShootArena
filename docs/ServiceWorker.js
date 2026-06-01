const cacheName = "OneShootArena-static-v1";

// Solo archivos estáticos del build (Unity WebGL)
const contentToCache = [
    "Build/docs.loader.js",
    "Build/docs.framework.js",
    "Build/docs.data",
    "Build/docs.wasm",
    "TemplateData/style.css"
];

self.addEventListener('install', (event) => {
    console.log('[Service Worker] Install');

    event.waitUntil(
        caches.open(cacheName).then((cache) => {
            console.log('[Service Worker] Caching static files');
            return cache.addAll(contentToCache);
        })
    );

    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    console.log('[Service Worker] Activate');

    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(
                keys.map((key) => {
                    if (key !== cacheName) {
                        console.log('[Service Worker] Deleting old cache:', key);
                        return caches.delete(key);
                    }
                })
            )
        )
    );

    self.clients.claim();
});

self.addEventListener('fetch', (event) => {

    const request = event.request;

    // 🔥 CRÍTICO: NO tocar requests dinámicos (Unity Services, Relay, Lobby, POST, etc.)
    if (request.method !== 'GET') {
        return;
    }

    // ❌ NO cachear APIs externas de Unity
    const url = request.url;

    if (
        url.includes("unity.com") ||
        url.includes("services.api.unity") ||
        url.includes("relay") ||
        url.includes("lobby") ||
        url.includes("authentication")
    ) {
        return;
    }

    event.respondWith(
        caches.match(request).then((cachedResponse) => {
            if (cachedResponse) {
                return cachedResponse;
            }

            return fetch(request).then((networkResponse) => {
                // Solo cachear respuestas válidas GET del build
                if (
                    request.url.includes("/Build/") ||
                    request.url.includes("/TemplateData/")
                ) {
                    const responseClone = networkResponse.clone();
                    caches.open(cacheName).then((cache) => {
                        cache.put(request, responseClone);
                    });
                }

                return networkResponse;
            });
        })
    );
});