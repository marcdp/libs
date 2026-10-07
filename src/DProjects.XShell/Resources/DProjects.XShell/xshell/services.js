
// class
export default class Services {

    // vars
    _config = null;
    _loader = null;
    _contracts = null;
    _items = {};
    _areas = null;
    _creationPath = [];

    // props
    get registry() {
        // project the current mutable records into independent inspection data
        return Object.freeze(Object.entries(this._items).map(([id, item]) => this._projectServiceInfo(id, item)));
    }

    // ctor
    constructor( {config, loader, contracts, areas} ) {
        this._config = config;
        this._loader = loader;
        this._contracts = contracts;
        this._areas = areas;    
    }

    //methods
    async init() {
        // validate contract references before loading any implementations
        const serviceContractItems = {};
        for(const serviceName in this._config.xshell.services) {
            const service = this._config.xshell.services[serviceName];
            const serviceContractItem = this._contracts.getContractItemById(service.contract);
            if (!serviceContractItem) throw new Error(`Service '${serviceName}' references unknown contract '${service.contract}'.`);
            serviceContractItems[serviceName] = serviceContractItem;
        }

        // initialize services based on the config
        const tasks = [];
        for(const serviceName in this._config.xshell.services) {
            const service = this._config.xshell.services[serviceName];
            const serviceContractItem = serviceContractItems[serviceName];
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
        if (Object.isFrozen(this._items)) throw new Error("Service registry is immutable after initialization.");
        if (Object.hasOwn(this._items, name)) throw new Error(`Service already exists: ${name}`);
        this._items[name] = { instance, contractItem, implementationItem, state: instance == null ? "registered" : "created" };
    }
    has(name) {
        // check the registry without constructing a configured service
        return Object.hasOwn(this._items, name);
    }
    resolve(name) {
        const result = this._items[name];
        if (result == undefined) throw new Error(`Service not found: ${name}`); 
        if (result.state === "creating") {
            // report the active constructor path when a service resolves itself recursively
            const cycleStart = this._creationPath.indexOf(name);
            const cycle = [...this._creationPath.slice(cycleStart), name].join(" -> ");
            throw new Error(`Circular service dependency detected: ${cycle}`);
        }
        if (result.instance == null && result.implementationItem) {
            // create and validate the instance lazily while resolving constructor dependencies
            result.state = "creating";
            this._creationPath.push(name);
            try {
                const self = this;
                const instance = new result.implementationItem.class(new Proxy({}, {
                    get(target, prop) {
                        return self.resolve(prop);
                    }
                }));
                this._validateImplementation(name, instance, result.contractItem);
                result.instance = instance;
                result.state = "created";
            } catch (error) {
                result.instance = null;
                result.state = "registered";
                throw error;
            } finally {
                this._creationPath.pop();
            }
        }
        return result.instance;
    }
    getServiceInfo(id) {
        return Object.hasOwn(this._items, id) ? this._projectServiceInfo(id, this._items[id]) : undefined;
    }

    // methods (private)
    _projectServiceInfo(id, item) {
        // expose scalar metadata without retaining references to runtime objects
        const contractId = item.contractItem?.id;
        const contractUrl = item.contractItem?.url;
        const description = item.contractItem?.contract?.description;
        const icon = item.contractItem?.contract?.icon;
        const moduleId = item.implementationItem?.moduleId;
        return Object.freeze({
            id,
            state: item.state,
            contractId: typeof contractId === "string" ? contractId : null,
            contractUrl: typeof contractUrl === "string" ? contractUrl : null,
            description: typeof description === "string" ? description : null,
            icon: typeof icon === "string" ? icon : null,
            moduleId: typeof moduleId === "string" ? moduleId : null,
            implementationName: item.implementationItem?.class?.name ?? null,
            url: item.implementationItem?.url ?? null,
            size: item.implementationItem?.size ?? null,
            time: item.implementationItem?.time ?? null
        });
    }
    _validateImplementation(serviceName, instance, contractItem) {
        // validate the reliable callable surface declared by the contract
        for (const methodName of Object.keys(contractItem?.contract?.methods || {})) {
            if (typeof instance?.[methodName] !== "function") {
                throw new Error(`Service '${serviceName}' does not implement required method '${methodName}'.`);
            }
        }
        // validate declared properties without reading values or invoking accessors
        for (const propertyName of Object.keys(contractItem?.contract?.properties || {})) {
            if (!this._findPropertyDescriptor(instance, propertyName)) {
                throw new Error(`Service '${serviceName}' does not implement required property '${propertyName}'.`);
            }
        }
    }
    _findPropertyDescriptor(instance, propertyName) {
        // walk the instance and prototype chain so getters remain unexecuted
        let current = instance;
        while (current && current !== Object.prototype) {
            const descriptor = Object.getOwnPropertyDescriptor(current, propertyName);
            if (descriptor) return descriptor;
            current = Object.getPrototypeOf(current);
        }
        return undefined;
    }

};


