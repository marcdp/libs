
// class
export default class Services {

    // vars
    _config = null;
    _loader = null;
    _contracts = null;
    _items = {};
    _areas = null;

    // ctor
    constructor( {config, loader, contracts, areas} ) {
        this._config = config;
        this._loader = loader;
        this._contracts = contracts;
        this._areas = areas;    
    }

    //methods
    async init() {
        // initialize services based on the config
        const tasks = [];
        for(const serviceName in this._config.xshell.services) {
            const service = this._config.xshell.services[serviceName];
            const serviceContractItem = this._contracts.getContractItemById(service.contract);
            tasks.push((async () => {
                const moduleId = this._areas.getModuleId(service.implementation);
                const start = performance.now();
                const serviceImplementation = await this._loader.load("module:" + service.implementation);
                let size = 0
                for (const module of Object.values(this._config.modules)) {
                    for(const moduleFile of module.files) {
                        if (moduleFile.path === service.implementation) {
                            size += moduleFile.size;
                        }
                    }
                }
                this.register(serviceName, null, { 
                    contractItem: serviceContractItem, 
                    implementationItem: {
                        class: serviceImplementation,
                        moduleId: moduleId,
                        size: size,
                        time: performance.now() - start,
                        url: this._config.app.basePath + service.implementation
                    }
                });
            })());
        }
        await Promise.all(tasks);
        this._items = Object.freeze(this._items);
    }
    register(name, instance, { contractItem, implementationItem } =  {}) {
        if (this._items[name]) throw new Error(`Service already exists: ${name}`);
        this._items[name] = { instance, contractItem, implementationItem };
    }
    resolve(name) {
        let result = this._items[name];
        if (result == undefined) throw new Error(`Service not found: ${name}`); 
        if (result.instance == null && result.implementation) {
            // create instance (resolve recursively)
            const self = this;
            result.instance = new result.implementationItem.class(new Proxy({}, {
                get(target, prop) {
                    return self.resolve(prop);
                }
            }));
        }
        return result.instance;
    }
    getServiceItems() {
        return this._items;
    }
    getServiceItemById(id) {
        return this._items[id];
    }

};


