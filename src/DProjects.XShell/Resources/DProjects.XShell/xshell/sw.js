// state
let state = null;
let stateLoadPromise = null;
let registrationQueue = Promise.resolve();

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
function getState() {
    if (state) return Promise.resolve(state);
    stateLoadPromise ??= loadDBState("state").then(persistedState => {
        state = persistedState || { rules: [] };
        return state;
    });
    return stateLoadPromise;
}
function normalizeRule(rule) {
    if (!rule || typeof rule.src !== "string" || !rule.src.trim() || typeof rule.dst !== "string" || !rule.dst.trim()) {
        throw new Error("Service Worker mappings require non-empty src and dst values.");
    }
    const src = new URL(rule.src, self.location.origin);
    src.search = "";
    src.hash = "";
    src.pathname = src.pathname.replace(/\/+$/, "") || "/";
    return { ...rule, src: src.href };
}
async function registerMappings(rules) {
    if (!Array.isArray(rules)) throw new Error("Service Worker mappings must be an array.");

    // build and validate the complete candidate registry before changing durable or in-memory state
    const currentState = await getState();
    const candidateRules = currentState.rules.map(normalizeRule);
    const rulesBySrc = new Map(candidateRules.map(rule => [rule.src, rule]));
    for (const incomingRule of rules.map(normalizeRule)) {
        const registeredRule = rulesBySrc.get(incomingRule.src);
        if (registeredRule) {
            if (registeredRule.dst !== incomingRule.dst) {
                throw new Error(
                    `Service Worker mapping '${incomingRule.src}' is already registered to '${registeredRule.dst}' ` +
                    `and cannot be remapped to '${incomingRule.dst}'.`
                );
            }
            continue;
        }
        candidateRules.push(incomingRule);
        rulesBySrc.set(incomingRule.src, incomingRule);
    }

    const candidateState = { ...currentState, rules: candidateRules };
    await saveDBState("state", candidateState);
    state = candidateState;
}
function enqueueMappingRegistration(rules) {
    const registration = registrationQueue.then(() => registerMappings(rules));
    registrationQueue = registration.catch(() => {});
    return registration;
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
        const port = event.ports?.[0];
        if (event.data?.type !== "registerMappings") {
            port?.postMessage({ type: "error", message: `Unsupported Service Worker message '${event.data?.type}'.` });
            return;
        }
        try {
            await enqueueMappingRegistration(event.data.payload?.rules);
            port?.postMessage({ type: "registered" });
        } catch (error) {
            port?.postMessage({ type: "error", message: error?.message || String(error) });
        }
    })());
});
self.addEventListener("fetch", (event) => {
    if (event.request.method !== "GET" && event.request.method !== "HEAD") {
        return;
    }
    event.respondWith((async () => {
        await registrationQueue;
        await getState();
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
            if (!ruleSrcUrl || srcUrl.pathname.length > ruleSrcUrl.pathname.length) {
                rule = targetRule;
                ruleSrcUrl = srcUrl;
            }
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
