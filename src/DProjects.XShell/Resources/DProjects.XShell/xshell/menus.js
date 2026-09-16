import { findObjectsPath } from "./utils/object.js";

// const
const MENU_MAIN = "main";

// class
export default class Menus {

    // vars
    _bus = null;
    _config = null;
    _navigation = null;
    _modules = null;
    // sources of menu items registered by modules
    _sources = {};
    // cached menus
    _cache = null;

    // ctor
    constructor( { bus, config, modules, navigation } ) {
        this._bus = bus;
        this._config = config;
        this._modules = modules;
        this._navigation = navigation;
    }

    // method
    init() {
        // build menus from config
        let cache = {};
        for(let key in this._config.config) {
            if (key.startsWith("modules.") && key.indexOf(".menus.") != -1) {
                const keyParts = key.split(".");
                const moduleName = keyParts[1];
                const modulePath = this._modules.getModule(moduleName).path;
                const menuName = keyParts[3];
                const menuArea = keyParts[4] || (menuName == MENU_MAIN ? "main" : "");
                const menuItems = this._config.config[key];
                if (!cache[menuName]) cache[menuName] = [];
                for(let menuitem of menuItems.map(item => this._cloneMenuitem(item, moduleName, modulePath, menuArea, ""))) {
                    cache[menuName].push(menuitem);
                }
            }
        }
        this._cache = cache;
    }
    getMenu(name) {
        // get menu 
        if (name == MENU_MAIN) {
            return this.getMenuMain();
        }
        return this._cache[name];
    }
    getMenuMain() {
        // get main menu (by area)
        let result =  [];
        const breadcrum = this.getMenuitemBreadcrumb(this._navigation.stack[0].href);
        if (breadcrum) {
            const area = breadcrum[0].area;
            for(let i in this._cache[MENU_MAIN]) {
                const menu = this._cache[MENU_MAIN][i];
                if (menu.area == area) result.push(menu);
            }
        }
        return result;
    }
    getMenuitemBreadcrumb(href) {
        // get menuitem breadcrumb
        let hrefs = [];
        hrefs.push(href);
        if (href.indexOf("#")!=-1) {
            href = href.substring(0, href.indexOf("#"));
            hrefs.push(href);
        }
        if (href.indexOf("?")!=-1) {
            href = href.substring(0, href.indexOf("?"));
            hrefs.push(href);
        }
        for(let target_href of hrefs) {
            for(let menuName in this._cache) {
                let menu = this._cache[menuName];
                let menuitems = findObjectsPath(menu, 'href', target_href);
                if (menuitems) {
                    const result = [];
                    for(let i = 0; i < menuitems.length; i++) {
                        let menuitem = menuitems[i];
                        let item = { label: menuitem.label, href: menuitem.href};
                        if (menuitem.icon) item.icon = menuitem.icon;
                        if (menuitem.area) item.area = menuitem.area;
                        result.push(item);
                    }
                    return result;
                }
            }
        }
        return null;
    }

    
    // sources
    registerSource(name, source) {
        // register a menu source, which can provide menu items dynamically, and recompute them on events it depends on
        this._sources[name] = source;
        for(let event of source.dependsOn || []) {
            this._bus.addEventListener(event, () => {
                const changed = source.refresh();
                if (changed) this._bus.emit("xshell:menus:changed", { } );
            });
        }        
    }

    // private methods
    _cloneMenuitem(menuitem, moduleName, modulePath, areaName, hrefPrefix) {
        // clone menu item recursively
        let href = (menuitem.href ? (hrefPrefix + menuitem.href) : null);
        let result = {
            label: menuitem.label,
            href: href,
            icon: menuitem.icon || null,
            module: moduleName,
            area: areaName,
            children: (menuitem.children ? menuitem.children.map( (child) => this._cloneMenuitem(child, moduleName, modulePath, areaName, hrefPrefix) ) : [])
        }
        if (menuitem.childrenSource) {
            // create refresh function for that childrenSource
            const source = this._sources[menuitem.childrenSource];
            const cloneMenuitem = this._cloneMenuitem;
            source.refresh = function() {
                let menuitems = source.resolve();
                result.children.length = 0;
                result.children.push(...menuitems.map( menuitem => cloneMenuitem(menuitem, moduleName, modulePath, areaName, modulePath)));
                return true; //changed
            };
            source.refresh();
        }
        return Object.freeze(result);
    } 
    
}
