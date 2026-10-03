
// class
export default class Services {

    // vars
    _config = null;
    _loader = null;
    _contracts = null;
    _cache = new Map();

    // ctor
    constructor( {config, loader, contracts} ) {
        this._config = config;
        this._loader = loader;
        this._contracts = contracts;
    }

    //methods
    async init() {
        // initialize services based on the config
        const tasks = [];
        for(const serviceName in this._config.xshell.services) {
            const service = this._config.xshell.services[serviceName];
            const serviceContract = this._contracts.getContractById(service.contract);
            tasks.push((async () => {
                const serviceImplementation = await this._loader.load("module:" + service.implementation);
                this.register(serviceName, null, serviceContract, serviceImplementation);
            })());
        }
        await Promise.all(tasks);
    }
    register(name, instance, contract, implementation) {
        if (this._cache.has(name)) throw new Error(`Service already exists: ${name}`);
        this._cache.set(name, { instance, contract, implementation });
    }
    resolve(name) {
        let result = this._cache.get(name);
        if (result == undefined) throw new Error(`Service not found: ${name}`);
        if (result.instance == null && result.implementation) {
            // create instance
            const self = this;
            result.instance = new result.implementation(new Proxy({}, {
                get(target, prop) {
                    // resolve from itself
                    return self.resolve(prop);
                }
            }));
        }
        return result.instance;
    }
    getServices() {
        return this._cache
    }
    getServiceById(id) {
        return this._cache.get(id);
    }

};


