
// LoaderException
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
    _debug = null;
    _resolver = null;
    _appBasePath = null;
    _assetsPrefix = null;
    _navigationMode = null;
    _navigationHashPrefix = null;
    

    _cache = {};
    _registry = [];

    //ctor
    constructor( {bus, config, debug, resolver} ) {
        this._bus = bus;
        this._debug = debug;
        this._resolver = resolver;
        this._appBasePath = config.app.basePath;
        this._assetsPrefix = config.xshell.assetsPrefix;
        this._navigationMode = config.xshell.navigation.mode;
        this._navigationHashPrefix = config.xshell.navigation.hashPrefix;
        this._componentLazy = config.xshell.ui.component.lazy;
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
        let urls = [];
        let paths = [];
        let tasks = [];
        for(let resource of resources) {            
            // resolve definition
            let name = resource.split(":")[1];
            let definitionObject = this._resolver.resolve(resource);
            if (!definitionObject) {
                throw new LoaderException([new Error(`Resource not found: ${resource}`)]);
            }
            let {definition, url, path} = definitionObject;
            urls.push(url);
            paths.push(path);
            // get or load handler
            let loader = loaders[definition.loader];
            if (!loader) {                
                let loaderUrl = definition.loader;
                if (loaderUrl.indexOf(":")!=-1) {
                    loaderUrl = definition.loader;
                } else if (loaderUrl.indexOf("/")!=-1) {
                    loaderUrl = this._appBasePath + definition.loader;
                } else {
                    loaderUrl = this._appBasePath + "/" + this._assetsPrefix + "/xshell/loaders/" + definition.loader + ".js";
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
                this._debug.log(`loader: load '${resource}' from ${url} ...`);
                let promise = (async () => {
                    let value = null;
                    let registryItem = {resource, definition, url, status: "pending"};
                    this._registry.push(registryItem);                    
                    await this._bus.emit("xshell:loader:resource:fetch", {resource, url});
                    try {
                        value = await loader.load(url, {
                            resourceName: name, 
                            resourcePath:path, 
                            resourceDefinition: definition, 
                            appBasePath:this._appBasePath, 
                            navigationMode: this._navigationMode,
                            navigationHashPrefix: this._navigationHashPrefix,
                            componentLazy: this._componentLazy
                        });
                        registryItem.status = "loaded";
                        await this._bus.emit("xshell:loader:resource:loaded", {resource, url});
                    } catch (exception) {
                        registryItem.status = "error";
                        await this._bus.emit("xshell:loader:resource:error", {resource, url});
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
                    debugger
                    const exception = taskResult.reason instanceof Error ? taskResult.reason : new Error(String(taskResult.reason));
                    const error = new ResourceLoadError(
                        resources[i], 
                        exception.message, 
                        {
                            code: (exception.message.indexOf("Failed to fetch") != -1 ? 404 : 500),
                            url: urls[i],
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
            let message = errors.map(error => error.message + " (" + error.code + ")" + (error.path ? " at " + error.path : "") + (error.line ? " at line " + error.line : "") + (error.cause ? " Caused by: " + error.cause : "")).join("; ");
            for (const error of errors) {
                console.error(error);
            }
            throw new LoaderException(errors, "Some resources failed: " + message);
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

