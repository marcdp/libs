
// class
export default class Modules {

    // vars
    _bus = null;
    _config = null;
    _loader = null;
    _resolver = null;
    _modules = null;
    
    // ctor
    constructor( { bus, config, loader, resolver } ) {
        this._bus = bus;
        this._config = config;
        this._loader = loader;
        this._resolver = resolver;

    }

    // method
    async init() {
        let tasks = [];
        this._modules = [];
        const assetsPrefix = this._config.get("xshell.assetsPrefix");
        for (let module of this._config.getAsObjects("modules")) {
            // load stylesheets
            let styles = this._config.get("modules." + module.name + ".styles", []);
            for (let style of styles) {
                let styleUrl = this._resolver.resolveUrl(style);
                tasks.push(this._loadStyleSheet(styleUrl));
            }
            // init module
            let instance = {
                name: module.name,
                label: module.label || module.name,
                path: "/" + assetsPrefix + "/" + module.name,
                controller: {
                    onCommand: function() {}
                }
            };
            if (module.handler) {
                tasks.push((async() => {
                    let moduleClass = await this._loader.load("module:" + module.handler);
                    instance.controller = new moduleClass();
                })());
            }
            this._modules.push(instance);
        }
        await Promise.all(tasks);
        // dispatch module-load
        tasks = [];
        for (let instance of this._modules) {
            tasks.push(instance.controller.onCommand("load", { 
                name: instance.name,
                path: instance.path,
                ... this._config.getAsObject("modules." + instance.name + ".params")
            }));
        }
        await Promise.all(tasks); 
    }

    // modules
    resolveModuleName(src) {
        //get module name by src
        if (!src) debugger;
        for (let module of this._modules) {
            if (src.startsWith(module.path + "/")) {
                return module.name;
            }
        }
        return null;
    }
    getModuleBySrc(src) {
        //get module by src
        const name = this.resolveModuleName(src);
        return (name ? this.getModule(name) : null);
    }
    getModule(name) {
        //get module by name
        for (let module of this._modules) {
            if (module.name == name) {
                return module;
            }
        }
    }
    getModules() {
        //get all modules
        return this._modules;
    }

    //styles
    async _loadStyleSheet(src) {
        //load stylesheet
        let resolve = null;
        let link = document.createElement("link");
        link.setAttribute("rel", "stylesheet");
        link.setAttribute("href", src);
        link.addEventListener("load", () => {
            resolve();
        });
        document.head.appendChild(link);
        return new Promise((resolv) => {
            resolve = resolv;
        });
    }
    
}

