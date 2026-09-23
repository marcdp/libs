// state
let state = null;


// db
const DB_NAME = "xshell-sw";
const DB_VERSION = 1;
const STORE_NAME = "state";
function openDB() {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open(DB_NAME, DB_VERSION);
        req.onupgradeneeded = () => {
            const db = req.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                db.createObjectStore(STORE_NAME);
            }
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}
async function saveDBState(key, value) {
    const db = await openDB();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readwrite");
        tx.objectStore(STORE_NAME).put(value, key);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}
async function loadDBState(key) {
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, "readonly");
    const req = tx.objectStore(STORE_NAME).get(key);
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}


// events
self.addEventListener("install", () => {
    self.skipWaiting(); // move from waiting -> active asap
    console.log("sw: installing ...");
});
self.addEventListener("activate", (event) => {
    event.waitUntil(self.clients.claim()); // start controlling open pages
    console.log("sw: activated");
});
self.addEventListener("message", async (event) => {
    console.log("sw: received message:", event.data);
    if (event.data.type == "init") {
        // init
        state = event.data.payload;
        // save state
        await saveDBState("state", state);
        // reply to sender port
        event.ports?.[0]?.postMessage({ type: "ready" });
    }
});
self.addEventListener("fetch", event => {
    event.respondWith((async () => {
        if (!state) {
            state = await loadDBState("state") || { rules: [] };
        }
        return handleRequest(event.request);
    })());
});


// methods
async function handleRequest(request) {

    // if request is outside scope, just fetch
    if (!request.url.startsWith(self.registration.scope)) {
        return fetch(request, { cache: "no-store" });
    }

    // if no rules, just fetch
    if (!state || state.rules.length == 0) {
        return fetch(request, { cache: "no-store" });
    }

    const requestUrl = new URL(request.url);

    // determine the rule to use
    let rule = null;
    let ruleSrcUrl = null;

    for (const targetRule of state.rules) {
        const srcUrl = new URL(targetRule.src);

        if (requestUrl.origin === srcUrl.origin &&
            (
                requestUrl.pathname === srcUrl.pathname ||
                requestUrl.pathname.startsWith(srcUrl.pathname + "/")
            )
        ) {
            rule = targetRule;
            ruleSrcUrl = srcUrl;
            break;
        }
    }

    // no matching rule
    if (!rule) {
        return fetch(request, { cache: "no-store" });
    }

    // resolve virtual /_assets/... path against the physical assetsUrl
    const relativePath = requestUrl.pathname
        .substring(ruleSrcUrl.pathname.length)
        .replace(/^\//, "");

    const baseUrl = rule.dst.endsWith("/") ? rule.dst : rule.dst + "/";
    const url = new URL(relativePath, baseUrl);

    // preserve query string
    url.search = requestUrl.search;

    // fetch the real resource
    const response = await fetch(url, {
        method: request.method,
        headers: request.headers,
        body: request.method !== "GET" && request.method !== "HEAD"
            ? request.body
            : undefined,
        mode: "same-origin",
        credentials: "same-origin",
        redirect: "manual"
    });

    // prevent physical resource URL from leaking through redirect-related headers
    const headers = new Headers(response.headers);
    headers.delete("Location");
    headers.delete("Content-Location");

    return new Response(response.body, {
        status: response.status,
        statusText: response.statusText,
        headers
    });
}