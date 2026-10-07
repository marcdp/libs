// state
let state = null;

// utils
const DB_NAME = "xshell-sw" + self.location.pathname.substring(0, self.location.pathname.lastIndexOf("/")).replaceAll("/", "-");
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
self.addEventListener("install", (event) => {
    event.waitUntil(self.skipWaiting()); // move from waiting -> active asap
    console.log("sw: installing ...");
});
self.addEventListener("activate", (event) => {
    event.waitUntil(self.clients.claim()); // start controlling open pages
    console.log("sw: activated");
});
self.addEventListener("message", (event) => {
    event.waitUntil((async () => {
        console.log("sw: received message:", event.data);
        if (event.data.type === "init") {
            state = event.data.payload;
            await saveDBState("state", state);
            event.ports?.[0]?.postMessage({ type: "ready" });
        }
    })());
});
self.addEventListener("fetch", (event) => {
    event.respondWith((async () => {
        if (!state) {
            state = await loadDBState("state") || { rules: [] };
        }
        return handleRequest(event.request);
    })());
});


// methods
async function handleRequest(request) {

    // debug
    const debug = false;
    
    // if request is outside scope, just fetch
    if (!request.url.startsWith(self.registration.scope)) {
        if (debug) console.log("sw: request outside scope: " + request.url);
        return fetch(request);
    }

    // if no rules, just fetch
    if (!state?.rules?.length) {
        if (debug) console.log("sw: request no rules, just fetch: " + request.url);
        return fetch(request);
    }

    // create URL object for the request
    const requestUrl = new URL(request.url);

    // determine the rule to use
    let rule = null;
    let ruleSrcUrl = null;
    for (const targetRule of state.rules) {
        const srcUrl = new URL(targetRule.src, self.location.origin);
        if (requestUrl.origin === srcUrl.origin && (requestUrl.pathname === srcUrl.pathname || requestUrl.pathname.startsWith(srcUrl.pathname + "/"))) {
            rule = targetRule;
            ruleSrcUrl = srcUrl;
            break;
        }
    }

    // no matching rule    
    if (!rule) {
        if (debug) console.log("sw: no matching rule: " + request.url)
        return fetch(request);
    }

    // resolve the configured virtual asset path against the physical assetsUrl
    const relativePath = requestUrl.pathname.substring(ruleSrcUrl.pathname.length).replace(/^\/+/, "");
    const baseUrl = rule.dst.endsWith("/") ? rule.dst : rule.dst + "/";
    const url = new URL(relativePath, baseUrl);

    // preserve query string
    url.search = requestUrl.search;

    // fetch the real resource
    if (debug) console.log("sw: fetching: " + request.url + " --> " + url.toString());
    const response = await fetch(url, {
        method: request.method,
        headers: request.headers,
        body: request.method !== "GET" && request.method !== "HEAD"
            ? request.body
            : undefined,
        //mode: "same-origin",
        //credentials: "same-origin",
        //redirect: "manual"
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
