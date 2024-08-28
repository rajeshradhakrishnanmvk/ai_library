const cacheName = 'kanban-board-v1';
const cacheAssets = [
    '/kanban.html',
    '/css/style.css',
    '/js/main.js',
    '/js/kanbanService.js',
    '/js/serviceWorker.js'
];

self.addEventListener('install', function(e) {
    e.waitUntil(
        caches.open(cacheName)
            .then(cache => {
                console.log('Caching files');
                return cache.addAll(cacheAssets);
            })
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', function(e) {
    e.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cache => {
                    if (cache !== cacheName) {
                        console.log('Clearing old cache');
                        return caches.delete(cache);
                    }
                })
            );
        })
    );
    return self.clients.claim();
});

// Fetch event
self.addEventListener('fetch', (e) => {
    e.respondWith(
        caches.match(e.request)
            .then(response => {
                return response || fetch(e.request);
            })
            .catch(err => {
                console.error('Fetch failed:', err);
            })
    );
});
