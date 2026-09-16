
// class
export default class Storage {

    // scopes
    static SCOPE_MEMORY = "memory";  // memory
    static SCOPE_PERSISTENT = "persistent" // localStorage
    static SCOPE_TAB = "tab"; //  sessionStorage
    static SCOPE_APP = "app";   // service worker (in memory map)

    // sample options:
    // { 
    //  scope: "memory", "persisten", "tab", "app",
    //  namespace: "mymodule"
    //  ttl: number (seconds)
    // }

    // vars
    _memScope = new Map();

    
    // ctor
    constructor( { config} ) {
    }

    // methods
    async get(key) {
        debugger;
    }
    async set(key, value, options) {
        debugger;
    }
    async remove(key, options) {
        debugger;
    }
    async has(key, options) {
        debugger;
    }
    async clear(options) {
        debugger;
    }
    async keys(options) {
        debugger;
    }

}

