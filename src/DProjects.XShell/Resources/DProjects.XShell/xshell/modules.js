import { loadStyleSheetRecursive } from './utils/stylesheets.js';

// class
export default class Modules {

    // vars
    _bus = null;
    _config = null;
    _loader = null;
    _resolver = null;
    _modules = null;
    _document = null;
    _services = null;
    
    
    // ctor
    constructor( { bus, config, loader, resolver, document, services } ) {
        this._bus = bus;
        this._config = config;
        this._loader = loader;
        this._resolver = resolver;
        this._document = document;
        this._services = services;
        this._modules = [];
    }

    // method
    async init() {
        // init modules instances
        const assetsPrefix = this._config.xshell.assetsPrefix;

        // validate service requirements before loading or starting any module controller
        for (const moduleId of Object.keys(this._config.modules)) {
            const moduleConfig = this._config.modules[moduleId];
            for (const serviceName of moduleConfig.requires || []) {
                if (!this._services.has(serviceName)) throw new Error(`Module '${moduleId}' requires unavailable service '${serviceName}'.`);
            }
        }

        // create modules
        let tasks = [];
        for(var moduleId of Object.keys(this._config.modules)) {
            const moduleConfig = this._config.modules[moduleId];
            // instance
            let module = {
                id: moduleId,
                config: moduleConfig,
                label: moduleConfig.label || moduleId,
                path: "/" + assetsPrefix + "/" + moduleId,
                params: moduleConfig.params,
                routes: moduleConfig.routes || Object.freeze({}),
                styles: [],
                controller: {
                    onCommand: function() {}
                }
            };
            // styles
            const moduleIndexCss = moduleConfig.assetsPath + "/styles/index.css";
            for (const file of moduleConfig.files) {
                if (file.path == moduleIndexCss) {
                    tasks.push((async() => {
                        let styleSheet = await loadStyleSheetRecursive(moduleIndexCss);
                        module.styles.push(styleSheet);
                    })());
                }
            }
            // controller
            const moduleControllerJs = moduleConfig.assetsPath + "/module.js";
            for (const file of moduleConfig.files) {
                if (file.path == moduleControllerJs) {
                    const moduleClass = await this._loader.load("module:" + moduleControllerJs);
                    const servicesProvider = new Proxy({}, {
                        get: (obj, prop) => {
                            if (prop == "moduleAssetsPath") {
                                // module assets path
                                 return moduleConfig.assetsPath;
                            } else if (prop == "moduleConfig") {
                                // module config
                                return { id: moduleId, ...moduleConfig };
                            } else {
                                // resolve from services
                                return this._services.resolve(prop);
                            }                    
                        }
                    });
                    module.controller = new moduleClass(servicesProvider);
                }
            }
            // add module instance
            this._modules.push(module);
        }
        await Promise.all(tasks);

        // dispatch module-load to all instances
        await this.start();

        // freeze modules
        for (let module of this._modules) {
            Object.freeze(module);
        }

        // add styles to document header
        for (let module of this._modules) {
            for (let styleSheet of module.styles) {
                this._document.adoptedStyleSheets.push(styleSheet);
            }
        }
    }

    // start/stop
    async start() {
        const started = [];
        try {
            for (const module of [...this._modules].reverse()) {
                if (module.controller?.start) {
                    await module.controller.start();
                }
                started.push(module);
            }
        } catch (error) {
            for (const module of started.reverse()) {
                try {
                    if (module.controller?.stop) {
                        await module.controller.stop();
                    }
                } catch {
                    // Keep the original startup error.
                }
            }
            throw error;
        }
    } 
    async stop() {
        for (const module of this._modules) {
            if (module.controller?.stop) {
                await module.controller.stop();
            }
        }
    }


    // methods
    resolveModuleId(src) {
        //get module name by src
        if (!src) return null;
        for (let module of this._modules) {
            if (src.startsWith(module.path + "/")) {
                return module.id;
            }
        }
        return null;
    }
    getModuleBySrc(src) {
        //get module by src
        const id = this.resolveModuleId(src);
        return (id ? this.getModuleById(id) : null);
    }
    getModuleById(id) {
        //get module by id
        for (let module of this._modules) {
            if (module.id == id) {
                return module;
            }
        }
    }
    getModules() {
        //get all modules
        return this._modules;
    }

}

