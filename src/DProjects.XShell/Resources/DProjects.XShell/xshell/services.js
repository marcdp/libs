
// class
export default class Services {

    // vars
    _cache = new Map();

    // ctor
    constructor() {
    }

    //methods
    register(name, service) {
        if (this._cache.has(name)) throw new Error(`Service already exists: ${name}`);
        this._cache.set(name, service);
    }
    resolve(name) {
        let result = this._cache.get(name);
        if (result == undefined) throw new Error(`Service not found: ${name}`);
        return result;
    }

};


