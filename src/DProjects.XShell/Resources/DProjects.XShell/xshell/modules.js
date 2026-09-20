
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
                styles: [],
                controller: {
                    onCommand: function() {}
                }
            };
            // styles
            for (let style of moduleConfig.styles || []) {
                let styleUrl = this._resolver.resolveUrl(style);
                tasks.push((async() => {
                    let moduleStyleSheet = await this._loader.load("style:" + styleUrl);
                    module.styles.push(moduleStyleSheet);
                })());
            }    
            // script
            if (moduleConfig.script) {
                tasks.push((async() => {
                    const moduleClass = await this._loader.load("module:" + moduleConfig.script);
                    const servicesProvider = new Proxy({}, {
                        get: (obj, prop) => {
                            if (prop == "definition") {
                                // definition of component
                                return definition;
                            } else if (prop == "params") {
                                // module params
                                return moduleConfig.params;
                            } else if (prop == "timer") {
                                // timer helper
                                return new Timer( (command) => {self.onCommand(command);} );
                            } else {
                                // resolve from services
                                return this._services.resolve(prop);
                            }                    
                        }
                    });
                    module.controller = new moduleClass(servicesProvider);
                })());
            }
            this._modules.push(module);
        }
        await Promise.all(tasks);

        // dispatch module-load to all instances
        tasks = [];
        for (let module of this._modules) {
            tasks.push(module.controller.start());
        }
        await Promise.all(tasks); 

        // add styles to document header
        for (let module of this._modules) {
            for (let styleSheet of module.styles) {
                this._document.adoptedStyleSheets.push(styleSheet);
            }
        }
    }


    // modules
    resolveModuleId(src) {
        //get module name by src
        if (!src) debugger;
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

