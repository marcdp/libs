import Api from "./api.js";
import Areas from "./areas.js";
import Auth from "./auth.js";
import Binds from "./binds.js";
import Bus from "./bus.js";
import Config from "./config.js";
import Debug from "./debug.js";
import Dialog from "./dialog.js";
import I18n from "./i18n.js";
import Loader from "./loader.js";
import Navigation from "./navigation.js";
import Menus from "./menus.js";
import Modules from "./modules.js";
import Page from "./page.js";
import Resolver from "./resolver.js";
import Runtime from "./runtime.js";
import Services from "./services.js";
import Settings from "./settings.js";
import Storage from "./storage.js";
import Tabs from "./tabs.js";
import UrlRewriter from "./urlRewriter.js";
import XPage from "./x-page.js";

// class
class XShell {

    //fields
    _api = null;
    _areas = null;
    _auth = null;
    _bus = null;
    _config = null;
    _container = null;
    _debug = null;
    _dialog = null;
    _i18n = null;
    _identity = null;
    _loader = null;
    _menus = null;
    _modules = [];
    _navigation = null;
    _resolver = null;
    _runtime = null;
    _settings = null;
    _services = null;
    _storage = null;
    _tabs = null;
    _urlRewriter = null;
    
    //ctor
    constructor() {
    }

    //props
    get api() { return this._api; }
    get areas() { return this._areas; }
    get auth() { return this._auth; }
    get bus() { return this._bus; }
    get config() { return this._config; }
    get container() { return this._container; }
    get debug() { return this._debug; }
    get dialog() { return this._dialog; }
    get i18n() { return this._i18n; }
    get identity() { return this._identity; }
    get loader() { return this._loader; }
    get menus() { return this._menus; }
    get modules() { return this._modules; }
    get navigation() { return this._navigation; }
    get resolver() { return this._resolver; }
    get runtime() { return this._runtime; }
    get settings() { return this._settings; }
    get services() { return this._services; }
    get storage() { return this._storage; }
    get tabs() { return this._tabs; }
    get urlRewriter() { return this._urlRewriter; }

    //methods
    async init(value) {
        // init
        this._bus = new Bus();
        this._debug = new Debug();
        this._config = new Config({ debug: this._debug, bus: this._bus, value: value });
        this._api = new Api( { config: this._config} );
        this._areas = new Areas( { bus: this._bus, config: this._config } );
        this._container = document.body;
        this._resolver = new Resolver( { debug: this._debug, config: this._config } );
        this._loader = new Loader({ bus: this._bus, config: this._config, debug: this._debug, resolver: this._resolver });
        this._auth = new Auth({ config: this._config, loader: this._loader });
        this._i18n = new I18n();
        this._modules = new Modules( { bus: this._bud, config: this._config, loader: this._loader, resolver: this._resolver } );
        this._navigation = new Navigation( { areas: this._areas, bus: this._bus, config: this._config, container: this._container });
        this._settings = new Settings();
        this._storage = new Storage( {config: this.config } );
        this._tabs = new Tabs( { bus: this._bus } );
        this._menus = new Menus( { bus: this._bus, config: this._config, modules: this._modules, navigation: this._navigation } );
        this._services = new Services();
        this._urlRewriter = new UrlRewriter();
        this._dialog = new Dialog( { config: this._config, navigation: this._navigation, i18n: this._i18n } );
        this._runtime = new Runtime();
        // services
        this._services.register("api", this._api);
        this._services.register("areas", this._areas);
        this._services.register("auth", this._auth);
        this._services.register("bus", this._bus);
        this._services.register("config", this._config);
        this._services.register("container", this._container);
        this._services.register("debug", this._debug);
        this._services.register("dialog", this._dialog);
        this._services.register("i18n", this._i18n);
        this._services.register("loader", this._loader);
        this._services.register("menus", this._menus);
        this._services.register("modules", this._modules);
        this._services.register("navigation", this._navigation);
        this._services.register("resolver", this._resolver);
        this._services.register("runtime", this._runtime);
        this._services.register("settings", this._settings);
        this._services.register("services", this._services);
        this._services.register("storage", this._storage);
        this._services.register("tabs", this._tabs);
        this._services.register("urlRewriter", this._urlRewriter);
        // auth
        this._identity = await this.auth.login(this._config);
        this._services.register("identity", this._identity);
        // modules
        await this._modules.init();               
        // navigation
        await this._navigation.init();
        // menus
        await this._menus.init();
    }   
}

// creates a default instance
let xshell = new XShell();

// export default instance
export default xshell;

// export other objects and classes
export { Binds, Page };
