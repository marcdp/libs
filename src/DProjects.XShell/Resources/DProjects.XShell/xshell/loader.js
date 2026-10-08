
// LoaderException
class ResourceLoadError extends Error {
    constructor(resource, message, opts = {}) {
        super(message, { cause: opts.cause });
        this.name = this.constructor.name;
        this.resource = resource;
        this.path = opts.path;
        this.url = opts.url;
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
    _resolver = null;
    _appBasePath = null;
    _xshellAssetsPath = null;
    _navigationMode = null;
    _navigationHashPrefix = null;
    
    _cache = {};
    _registry = [];

    //ctor
    constructor( {bus, config, resolver} ) {
        this._bus = bus;
        this._resolver = resolver;
        this._appBasePath = config.app.basePath;
        this._xshellAssetsPath = config.xshell.assetsPath;
        this._navigationMode = config.xshell.navigation.mode;
        this._navigationHashPrefix = config.xshell.navigation.hashPrefix;
        this._componentLazy = config.xshell.ui.component.lazy;
    }

    //props
    get registry() { 
        // project load metadata without exposing the loaded value or mutable records
        return Object.freeze(this._registry.map(item => Object.freeze({
            resource: item.resource, 
            url: item.url, 
            path: item.path, 
            status: item.status, 
            time: item.time, 
            moduleId: item.moduleId,
            description: item.description || ""
        })));
    }

    //methods
    async load(resources) {
        const resourcesOriginal = resources;
        const isString = typeof(resources) == "string";
        if (resources == undefined) return null;
        if (resources == "" ) return null;
        if (typeof(resources) == "string") resources = [resources];
        const isArray = Array.isArray(resources);
        const isObject = typeof(resources) == "object" && !isArray;
        if (isObject) resources = Object.values(resources);
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
            const cacheKey = this.getCacheKey(resource, definition);
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
                    loaderUrl = this._appBasePath + this._xshellAssetsPath + "/loaders/" + definition.loader + ".js";
                }
                let loaderToUse = new (await import(loaderUrl)).default();
                loaders[definition.loader] = loaderToUse;
                loader = loaderToUse;
            }
            // check cache
            let cacheItem = this._cache[cacheKey];
            if (cacheItem) {
                tasks.push({index: result.length, promise: cacheItem.promise ?? Promise.resolve(cacheItem.value)});
                result.push(null);
            } else if (loader == loaders.component && window.customElements.get(name)) {
                result.push(window.customElements.get(name));
            } else {
                // load
                console.log(`loader: load '${resource}' from ${url} ...`);
                let promise = (async () => {
                    let value = null;
                    let registryItem = {resource, 
                        definition, 
                        url,
                        path,
                        status: "pending", 
                        time: null, 
                        moduleId: definition.moduleId};
                    this._registry.push(registryItem);
                    this._bus.emit("xshell:loader:resource:fetch", {resource, url, path, moduleId: definition.moduleId});
                    const context = {
                        resourceName: name,
                        resourcePath: path,
                        resourceDefinition: definition,
                        appBasePath: this._appBasePath,
                        navigationMode: this._navigationMode,
                        navigationHashPrefix: this._navigationHashPrefix,
                        componentLazy: this._componentLazy
                    };
                    const start = performance.now();
                    try {
                        value = await loader.load(url, context);
                        const end = performance.now();
                        const time = end - start;
                        registryItem.status = "loaded";
                        registryItem.time = time;
                        registryItem.value = value;
                        // retain only descriptive text needed for component and page inspection
                        if (resource.startsWith("component:") || resource.startsWith("page:")) {
                            const description = value?.contract?.description;
                            registryItem.description = typeof description === "string" ? description : "";
                        }
                        this._bus.emit("xshell:loader:resource:loaded", {resource, url, path, time});
                    } catch (exception) {
                        const end = performance.now();
                        const time = end - start;
                        registryItem.time = time;
                        registryItem.status = "error";
                        this._bus.emit("xshell:loader:resource:error", {resource, url, path, time});
                        throw exception;
                    }
                    return value;
                })();
                if (definition.cache) {
                    // initialize cache item for this resource
                    cacheItem = {
                        value: null,
                        promise: null
                    };
                    cacheItem.promise = promise.then(value => {
                        if (this._cache[cacheKey] === cacheItem) {
                            cacheItem.value = value;
                            cacheItem.promise = null;
                        }
                        return value;
                    }, exception => {
                        if (this._cache[cacheKey] === cacheItem) {
                            delete this._cache[cacheKey];
                        }
                        throw exception;
                    });
                    this._cache[cacheKey] = cacheItem;
                    promise = cacheItem.promise;
                }
                tasks.push({index: result.length, promise});
                result.push(null);
            }            
        }
        //wait until all resources have been settled
        const taskResults = await Promise.allSettled(tasks.map(task => task.promise));
        // process result
        let errors = [];
        for(let i = 0 ; i < taskResults.length; i++) {
            let taskResult = taskResults[i];
            let resultIndex = tasks[i].index;
            if (taskResult != null) {
                if (taskResult.status === 'fulfilled') {
                    let value = taskResult.value;
                    if (value.cloneNode) {
                        value = value.cloneNode(true);
                    } else if (value.clone) {
                        value = value.clone();
                    }
                    result[resultIndex] = value;
                } else if (taskResult.status === 'rejected') {
                    const exception = taskResult.reason instanceof Error ? taskResult.reason : new Error(String(taskResult.reason));
                    const error = new ResourceLoadError(
                        resources[resultIndex],
                        exception.message, 
                        {
                            code: (exception.message.indexOf("Failed to fetch") != -1 ? 404 : 500),
                            url: urls[resultIndex],
                            path: paths[resultIndex],
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
        } else if (isObject) {
            result = Object.fromEntries(Object.keys(resourcesOriginal).map((key, index) => [key, result[index]]));
        }
        //return
        return result;
    }    

    // private
    removeQuery(resource) {
        // remove only the URL query while preserving the logical resource scheme and fragment
        const queryIndex = resource.indexOf("?");
        const fragmentIndex = resource.indexOf("#");
        if (queryIndex === -1 || (fragmentIndex !== -1 && queryIndex > fragmentIndex)) {
            return resource;
        }
        return resource.substring(0, queryIndex) + (fragmentIndex === -1 ? "" : resource.substring(fragmentIndex));
    }
    getCacheKey(resource, definition) {
        // select cache identity without changing the resource passed through resolution and loading
        const cacheMode = definition.cacheMode ?? "full";
        switch (cacheMode) {
            case "full":
                return resource;
            case "path":
                return this.removeQuery(resource);
            default:
                throw new Error(`Unsupported cache mode '${cacheMode}'.`);
        }
    }
};

