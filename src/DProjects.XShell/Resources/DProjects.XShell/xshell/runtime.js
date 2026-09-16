
// class
export default class Runtime {

    // vars
    _loadTime = null;

    // ctor
    constructor(handler) {
        this._loadTime = performance.now();
    }   

    // props
    get loadTime() { return this._loadTime; }
    get uptimeMs() {  return performance.now() - this._loadTime;}
    
}
