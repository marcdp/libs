let requests = [];
let returnPageClass = false;
let failNextLoad = false;
let loadGate = null;
let releaseLoadGate = null;
let loadWaiters = [];

function notifyLoadWaiters() {
    // resolve observers whose expected number of loads has been reached
    const pendingWaiters = loadWaiters;
    loadWaiters = [];
    for (const waiter of pendingWaiters) {
        if (requests.length >= waiter.count) {
            waiter.resolve();
        } else {
            loadWaiters.push(waiter);
        }
    }
}

export function reset() {
    requests = [];
    returnPageClass = false;
    failNextLoad = false;
    loadGate = null;
    releaseLoadGate = null;
    loadWaiters = [];
}
export function getRequests() {
    return [...requests];
}
export function usePageClasses() {
    returnPageClass = true;
}
export function failOnce() {
    failNextLoad = true;
}
export function deferLoads() {
    loadGate = new Promise(resolve => releaseLoadGate = resolve);
}
export function releaseLoads() {
    releaseLoadGate?.();
    loadGate = null;
    releaseLoadGate = null;
}
export function waitForLoadCount(count) {
    if (requests.length >= count) {
        return Promise.resolve();
    }
    return new Promise(resolve => loadWaiters.push({ count, resolve }));
}

export default class CountingLoader {
    async load(src) {
        // capture the concrete URL received by the resource-specific loader
        requests.push(src);
        notifyLoadWaiters();
        const currentLoadGate = loadGate;
        if (currentLoadGate) {
            await currentLoadGate;
        }
        if (failNextLoad) {
            failNextLoad = false;
            throw new Error("Simulated load failure");
        }
        if (returnPageClass) {
            return class PageImplementation {
                static get contract() { return { description: "Test page" }; }

                constructor({ src }) {
                    this.src = src;
                }
            };
        }
        return { src, requestNumber: requests.length };
    }
}
