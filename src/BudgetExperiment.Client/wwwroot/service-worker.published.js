// Published service worker — caches all Blazor framework assets for offline/instant loading.
// Cache versioning is handled by service-worker-assets.js (auto-generated at publish time).

// Caution: this is NOT a general-purpose service worker; Blazor WASM cache strategy only.
// API responses are NOT cached — dynamic data always goes to the network.

const cacheNamePrefix = 'offline-cache-';
const selfAssetsManifest = self.assetsManifest;

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

async function onInstall(event) {
    // Pre-cache all assets listed in the Blazor-generated manifest
    const assetsRequests = selfAssetsManifest.assets
        .filter(asset => asset.hash && asset.url)
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));

    const cache = await caches.open(cacheName());

    for (const request of assetsRequests) {
        try {
            await cache.add(request);
        } catch {
            // Non-critical: some assets may fail (e.g. source maps). Continue caching others.
        }
    }
}

async function onActivate(event) {
    // Delete all caches from previous versions
    const cacheKeys = await caches.keys();
    const currentCacheName = cacheName();
    await Promise.all(
        cacheKeys
            .filter(key => key.startsWith(cacheNamePrefix) && key !== currentCacheName)
            .map(key => caches.delete(key))
    );
}

async function onFetch(event) {
    // Only cache GET requests for same-origin assets
    if (event.request.method !== 'GET') {
        return fetch(event.request);
    }

    // Never cache API calls or index.html (dynamic content)
    const requestUrl = new URL(event.request.url);
    if (requestUrl.pathname.startsWith('/api/') ||
        requestUrl.pathname === '/' ||
        requestUrl.pathname === '/index.html') {
        return fetch(event.request);
    }

    // For framework assets: serve from cache if available, otherwise network
    const shouldServeFromCache = isFrameworkAsset(event.request);
    if (shouldServeFromCache) {
        const cache = await caches.open(cacheName());
        const cachedResponse = await cache.match(event.request);
        if (cachedResponse) {
            return cachedResponse;
        }
    }

    return fetch(event.request);
}

function cacheName() {
    return cacheNamePrefix + selfAssetsManifest.version;
}

function isFrameworkAsset(request) {
    const url = new URL(request.url);
    return url.pathname.startsWith('/_framework/') ||
           url.pathname.startsWith('/_content/') ||
           url.pathname.endsWith('.css') ||
           url.pathname.endsWith('.js') ||
           url.pathname.endsWith('.wasm');
}
