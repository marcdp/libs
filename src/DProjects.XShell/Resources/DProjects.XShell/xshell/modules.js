
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
                let styleUrl = this._resolver.resolveUrl("style:" + style);
                tasks.push((async() => {
                    let response = await fetch(styleUrl);
                    if (!response.ok) throw new Error(`Error ${response.status}: ${response.statusText}: ${styleUrl}`);
                    let css = await response.text();
                    //alert(styleUrl + "\n" +css)
                    let styleSheet = new CSSStyleSheet();
                    await styleSheet.replace(css);
                    module.styles.push(styleSheet);
                })());
            }    
            // script
            if (moduleConfig.controller) {
                tasks.push((async() => {
                    const moduleClass = await this._loader.load("module:" + moduleConfig.controller);
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
        const tasks = [];
        for (let module of this._modules) {
            tasks.push(module.controller.start());
        }
        await Promise.all(tasks); 
    } 
    async stop() {
        const tasks = [];
        for (let module of this._modules) {
            tasks.push(module.controller.stop());
        }
        await Promise.all(tasks); 
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

