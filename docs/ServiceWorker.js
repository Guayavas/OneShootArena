const cacheName = "GuayavasInc-OneShootArena-0.2";
const contentToCache = [
    "Build/docs.loader.js",
    "Build/docs.framework.js",
    "Build/docs.data",
    "Build/docs.wasm",
    "TemplateData/style.css"

];

self.addEventListener('install', function (e) {
    console.log('[Service Worker] Install');
    
    e.waitUntil((async function () {
      const cache = await caches.open(cacheName);
      console.log('[Service Worker] Caching all: app shell and content');
      await cache.addAll(contentToCache);
    })());
});

self.addEventListener('fetch', function (e) {
    // Only handle GET requests for caching.
    // POST and other methods are not supported by the Cache API and should bypass the Service Worker.
    if (e.request.method !== 'GET') {
        return;
    }

    e.respondWith((async function () {
      let response = await caches.match(e.request);
      console.log(`[Service Worker] Fetching resource: ${e.request.url}`);
      if (response) { return response; }

      response = await fetch(e.request);

      // Avoid caching Unity Services API calls even if they are GET
      const url = new URL(e.request.url);
      if (!url.host.includes('services.api.unity.com')) {
          const cache = await caches.open(cacheName);
          console.log(`[Service Worker] Caching new resource: ${e.request.url}`);
          cache.put(e.request, response.clone());
      }

      return response;
    })());
});
