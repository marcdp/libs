
// class
export default class Services {

    // vars
    _config = null;
    _loader = null;
    _cache = new Map();

    // ctor
    constructor( {config, loader} ) {
        this._config = config;
        this._loader = loader;
    }

    //methods
    async init() {
        // initialize services based on the config
        for(const serviceName in this._config.xshell.services) {
            const servicePath = this._config.xshell.services[serviceName];
            //const { default: Service } = await import(servicePath);
            //this.register(serviceName, new Service());
        }
    }
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


