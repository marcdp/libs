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
        //console.log("url is: " + request.url)
        return fetch(request, { cache: "no-store" });
    }    

    // if no rules, just fetch
    if (!state || state.rules.length == 0) {
        return fetch(request, { cache: "no-store" });
    }

    // determine the rule to use
    let rule = null;
    for(let targetRule of state.rules) {
        if (request.url.startsWith(targetRule.src)) {
            // check exceptions
            let isException = false;
            for(let exception of targetRule.exceptions) {
                if (request.url.startsWith(exception)) {
                    isException = true;
                    break;
                }
            }
            if (isException) {
                break;
            }
            // matched rule
            rule = targetRule;
            break;
        }
    }
    if (!rule) {
        // no matching rule, just fetch
        return fetch(request, { cache: "no-store" });
    }

    // check cache
    //const cache = await caches.open(rule.src + ".v" + rule.version);
    //if (state.mode == "production") {
    //    const cached = await cache.match(request);
    //    if (cached) {
    //        return cached;
    //    }
    //}

    // url to fetch
    let url = new URL(request.url.replace(rule.src, rule.dst));

    // read commands __xshell__ 
    let commands = [];
    const keysToDelete = [];
    for (const [key, value] of url.searchParams.entries()) {
        if (key.startsWith("__xshell__")){
            let keyParts = key.split("__");
            let command = keyParts[2];
            let args = keyParts.slice(3);
            commands.push({ command, args, value });
            keysToDelete.push(key);
        }
    }
    for (const k of keysToDelete) url.searchParams.delete(k);

    // fetch the real file
    //const path = request.url.substring(rule.src.length);
    //const dstUrl = rule.dst + path;
    const response = await fetch(url, {
        method: request.method,
        headers: request.headers,
        body: request.method !== "GET" && request.method !== "HEAD" ? request.body : undefined,
        mode: "same-origin",
        credentials: "same-origin",
        redirect: "manual"   // IMPORTANT: prevents redirects from rewriting module identity
    });

    // headers (remove redirect-related headers that could leak the /src/... URL)
    const headers = new Headers(response.headers);
    headers.delete("Location");
    headers.delete("Content-Location");

    // process commands __xshell__ 
    for (const cmd of commands) {
        if (cmd.command === "replace" && cmd.args.length === 1) {
            // ex: ...?__xshell__replace__XXXX=1234
            const searchValue = cmd.args[0];
            const replaceValue = cmd.value;
            let text = await response.text();
            text = text.split(searchValue).join(replaceValue);
            return new Response(text, {
                status: response.status,
                headers: headers
            });
        }
    }

    // body
    const body = response.body;

    // add to cache
    //if (state.mode == "production" && response.ok && response.type === "basic") {
    //    try {
    //        cache.put(request, response.clone());
    //    } catch (cacheErr) {
    //        console.warn("[SW] Cache error:", cacheErr);
    //    }
    //}

    // return the response
    return new Response(body, {
        status: response.status,
        headers: headers
    });
}
