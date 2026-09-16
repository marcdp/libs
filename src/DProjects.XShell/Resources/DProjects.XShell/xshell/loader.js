
// LoaderException
/*
class LoaderExceptionOld extends Error {
    constructor(message, opts={}) {
        super(message, ( opts.cause ? { cause: opts.cause } : undefined )); // Call the parent constructor (Error)
        this.name = this.constructor.name; // Set the error name
        if (opts.code) this.code = opts.code// Custom error code
        if (opts.details) this.details = opts.details; // Custom property for additional info
        if (opts.errors) this.errors = opts.errors || []; // Array of LoaderExption of {message...}
        if (Error.captureStackTrace) {
            Error.captureStackTrace(this, this.constructor);
        }
    }
}
*/
class ResourceLoadError extends Error {
    constructor(resource, message, opts = {}) {
        super(message, { cause: opts.cause });
        this.name = this.constructor.name;
        this.resource = resource;
        this.src = opts.src;
        this.path = opts.path;
        this.code = opts.code;
    }
}
class LoaderException extends AggregateError {
    constructor(errors, message = "Loader failed", opts = {}) {
        super(errors, message, { cause: opts.cause });
        this.name = this.constructor.name;
        this.code = opts.code ?? "LOADER_FAILED";
        this.details = {
            total: errors.length,
            failed: errors.length,
            succeeded: 0
        };
    }
}

// loaders
const loaders = {
};

// class
export default class Loader {

    //vars
    _bus = null;
    _config = null;
    _debug = null;
    _resolver = null;
    _appBase = null;
    _assetsPrefix = null;
    _navigationMode = null;
    _navigationHashPrefix = null;
    

    _cache = {};
    _registry = [];

    //ctor
    constructor( {bus, config, debug, resolver} ) {
        this._bus = bus;
        this._config = config;
        this._debug = debug;
        this._resolver = resolver;
        this._appBase = new URL(this._config.get("app.base")).pathname;
        this._assetsPrefix = this._config.get("xshell.assetsPrefix");
        this._navigationMode = config.get("navigation.mode");
        this._navigationHashPrefix = config.get("navigation.hashPrefix");
        this._componentLazy = config.get("xshell.component.lazy");
    }

    //props
    get registry() { 
        let result = [];
        for(let item of this._registry){
            result.push({ resource: item.resource, src: item.src, status: item.status });
        }
        return Object.freeze(result);
    }

    //methods
    async load(resources) {
        let isString = typeof(resources) == "string";
        if (resources == undefined) return null;
        if (resources == "" ) return null;
        if (resources == [""] ) return null;;
        if (typeof(resources) == "string") resources = [resources];
        //load resources
        let result = [];
        let srcs = [];
        let paths = [];
        let tasks = [];
        for(let resource of resources) {            
            // resolve definition
            let name = resource.split(":")[1];
            let definitionObject = this._resolver.resolve(resource);
            if (!definitionObject) {
                throw new LoaderException([new Error(`Resource not found: ${resource}`)]);
            }
            let {definition, src, path} = definitionObject;
            srcs.push(src);
            paths.push(path);
            // get or load handler
            let loader = loaders[definition.loader];
            if (!loader) {                
                let loaderUrl = definition.loader;
                if (loaderUrl.indexOf(":")!=-1) {
                    loaderUrl = definition.loader;
                } else if (loaderUrl.indexOf("/")!=-1) {
                    loaderUrl = this._appBase + definition.loader;
                } else {
                    loaderUrl = this._appBase + "/" + this._assetsPrefix + "/xshell/loaders/" + definition.loader + ".js";
                }
                let loaderToUse = new (await import(loaderUrl)).default();
                loaders[definition.loader] = loaderToUse;
                loader = loaderToUse;
            }
            // check cache
            let cacheItem = this._cache[resource];
            if (cacheItem) {
                if (cacheItem.promise) {
                    tasks.push(cacheItem.promise);
                    result.push(null);
                } else {
                    result.push(cacheItem.value);
                }
            } else if (loader == loaders.component && window.customElements.get(name)) {
                result.push(window.customElements.get(name));
            } else {
                // load
                this._debug.log(`loader: load '${resource}' from ${src} ...`);
                let promise = (async () => {
                    let value = null;
                    let registryItem = {resource, definition, src, status: "pending"};
                    this._registry.push(registryItem);                    
                    await this._bus.emit("xshell:loader:resource:fetch", {resource, src});
                    try {
                        value = await loader.load(src, {
                            resourceName: name, 
                            resourcePath:path, 
                            resourceDefinition: definition, 
                            appBase:this._appBase, 
                            navigationMode: this._navigationMode,
                            navigationHashPrefix: this._navigationHashPrefix,
                            componentLazy: this._componentLazy
                        });
                        registryItem.status = "loaded";
                        await this._bus.emit("xshell:loader:resource:loaded", {resource, src});
                    } catch (exception) {
                        registryItem.status = "error";
                        await this._bus.emit("xshell:loader:resource:error", {resource, src});
                        throw exception;
                    }                    
                    return value;
                })();
                if (definition.cache) {
                    this._cache[resource] = {
                        value: null,
                        promise
                    };
                }
                tasks.push(promise);
                result.push(null);
            }            
        }
        //wait until all resources have been settled
        const taskResults = await Promise.allSettled(tasks);
        // process result
        let errors = [];
        for(let i = 0 ; i < taskResults.length; i++) {
            let taskResult = taskResults[i];
            if (taskResult != null) {
                if (taskResult.status === 'fulfilled') {
                    let value = taskResult.value;
                    if (value.cloneNode) {
                        value = value.cloneNode(true);
                    } else if (value.clone) {
                        value = value.clone();
                    }
                    result[i] = value;
                } else if (taskResult.status === 'rejected') {
                    const exception = taskResult.reason instanceof Error ? taskResult.reason : new Error(String(taskResult.reason));
                    const error = new ResourceLoadError(
                        resources[i], 
                        exception.message, 
                        {
                            code: (exception.message.indexOf("Failed to fetch") != -1 ? 404 : 500),
                            src: srcs[i],
                            path: paths[i],
                            cause: exception
                        }
                    );
                    errors.push(error);
                }
            }
        }
        // throw exception if errors
        if (errors.length) {
            throw new LoaderException(errors, "Some resources failed");
        }
        // result
        if (isString) {
            result = result[0];
        }
        //return
        return result;
    }    

    // private methods
    _dispatchEvent(name, detail) {

    }
};

