// class
export default class Areas {

    // vars
    _bus = null;
    _areas = [];
    _assetsPrefix = "_assets";
    _currentAreaId = null;
    _sources = {};
    _sourceTargets = {};

    // ctor
    constructor({ config, bus }) {
        this._bus = bus;
        this._assetsPrefix = (config.xshell.assetsPrefix || "_assets").replace(/^\/+|\/+$/g, "");
        const areasConfig = config.xshell.areas || {};
        const definitions = areasConfig.definitions || {};
        const defaultAreaId = areasConfig.default || null;

        // create navigation contexts without accepting a configured home
        const areas = [];
        for (const [areaId, areaConfig] of Object.entries(definitions)) {
            const { home, ...options } = areaConfig;
            areas.push({
                ...options,
                id: areaId,
                label: areaConfig.label || areaId,
                icon: areaConfig.icon || null,
                prefix: this._normalizePrefix(areaConfig.prefix),
                modules: Object.freeze([...(areaConfig.modules || [])]),
                order: areaConfig.order || 0,
                default: areaId === defaultAreaId,
                menus: Object.freeze({}),
                routes: Object.freeze([]),
                home: null
            });
        }
        this._validateAreas(areas, defaultAreaId);
        areas.sort((a, b) => {
            if (a.default !== b.default) return a.default ? -1 : 1;
            if (a.order !== b.order) return a.order - b.order;
            return a.label.localeCompare(b.label);
        });
        this._areas = Object.freeze(areas);
        this._currentAreaId = this.getDefaultArea()?.id || null;

        // navigation determines the current area from its URL
        bus.addEventListener("xshell:navigation:end", evt => {
            const areaId = this.resolveAreaId(evt.detail.src);
            if (areaId) this._setCurrentArea(areaId);
        });
    }

    // methods
    init({ modules }) {
        // compose effective menus after canonical modules exist
        const findDefaultHref = (items) => {
            for (const item of items || []) {
                if (item.default) return item.path || item.href || null;
                const href = findDefaultHref(item.children);
                if (href) return href;
            }
            return null;
        };
        for (const area of this._areas) {
            const menus = {};
            const routes = [];
            for (const moduleId of area.modules) {
                const module = modules.getModuleById(moduleId);
                if (!module) {
                    console.warn(`Area '${area.id}' references unknown module '${moduleId}'`);
                    continue;
                }
                for (const [menuName, menuDefinition] of Object.entries(module.config.menus || {})) {
                    let menuItems;

                    if (Array.isArray(menuDefinition)) {
                        menuItems = menuDefinition;
                    } else if (typeof menuDefinition === "string") {
                        const source = this._sources[menuDefinition];

                        if (!source) {
                            console.warn(`Unknown menu source '${menuDefinition}'`);
                            continue;
                        }

                        menuItems = source.resolve?.() || [];
                    } else {
                        continue;
                    }

                    if (!Array.isArray(menuItems)) {
                        console.warn(`Menu '${menuName}' must resolve to an array`);
                        continue;
                    }

                    menus[menuName] ??= [];
                    menus[menuName].push(...menuItems.map(menuitem => this._cloneMenuitem(menuitem, module, area)));
                }
                for (const [path, href] of Object.entries(module.routes || {})) {
                    routes.push(Object.freeze({ path, href, module: module.id }));
                }
            }
            for (const items of Object.values(menus)) Object.freeze(items);
            area.menus = Object.freeze(menus);
            area.routes = Object.freeze(routes);
            area.home = findDefaultHref(menus.navigation);
            Object.freeze(area);
        }
    }
    getAreas() {
        return this._areas;
    }
    getArea(id) {
        return this._areas.find(area => area.id === id) || null;
    }
    getDefaultArea() {
        return this._areas.find(area => area.default) || this._areas[0] || null;
    }
    getCurrentArea() {
        return this.getArea(this._currentAreaId) || this.getDefaultArea();
    }
    resolveAreaId(href) {
        if (!href) return null;
        // longest matching navigation prefix wins
        const areas = [...this._areas].sort((a, b) => b.prefix.length - a.prefix.length);
        for (const area of areas) {
            if (this._matchesPrefix(href, area.prefix)) return area.id;
        }
        return null;
    }
    resolvePath(path) {
        for (const area of this._areas) {
            for (const menu of Object.values(area.menus)) {
                const item = this._findMenuitemPath(menu, menuitem => menuitem.path === path)?.at(-1);
                if (item) return item;
            }
        }
        return null;
    }
    resolveHref(href, areaId = null) {
        if (!href) return null;
        // search the selected Area or only Areas associated with the target module
        const targetHref = this._getHrefIdentity(href);
        const moduleId = this.getModuleId(targetHref);
        let areas;
        if (areaId) {
            areas = [this.getArea(areaId)].filter(Boolean);
        } else if (moduleId) {
            areas = this._areas.filter(area => area.modules.includes(moduleId));
        } else {
            areas = [this.getCurrentArea()].filter(Boolean);
        }
        for (const area of areas) {
            const areaHref = this._buildAreaHref(targetHref, area);
            for (const menu of Object.values(area.menus)) {
                const menuitems = this._findMenuitemPath(menu, menuitem => this._getHrefIdentity(menuitem.href) === areaHref);
                if (menuitems) return menuitems.at(-1);
            }
        }
        return null;
    }
    getModuleId(href) {
        const parts = href.split("/");
        const assetsIndex = parts.indexOf(this._assetsPrefix);
        return assetsIndex === -1 ? null : parts[assetsIndex + 1] || null;
    }
    getMenu(name, areaId = null) {
        const area = areaId ? this.getArea(areaId) : this.getCurrentArea();
        return area?.menus[name] || [];
    }
    getMenuitemBreadcrumb(href, areaId = null) {
        const area = areaId ? this.getArea(areaId) : this.getCurrentArea();
        if (!area || !href) return null;
        // search only the selected area's effective menus
        const targetHref = this._getHrefIdentity(href);
        for (const menu of Object.values(area.menus)) {
            const menuitems = this._findMenuitemPath(menu, menuitem => this._getHrefIdentity(menuitem.href) === targetHref);
            if (menuitems) {
                return menuitems.map(menuitem => ({
                    label: menuitem.label,
                    href: menuitem.href,
                    path: menuitem.path,
                    ...(menuitem.icon ? { icon: menuitem.icon } : {}),
                    module: menuitem.module,
                    area: menuitem.area
                }));
            }
        }
        return null;
    }
    registerSource(name, source) {
        // register a dynamic menu source
        this._sources[name] = source;
        this._sourceTargets[name] = [];
        // refresh every effective copy backed by this source
        source.refresh = () => {
            let changed = false;
            for (const target of [...this._sourceTargets[name]]) {
                const menuitems = source.resolve?.() || [];
                const children = menuitems.map(child => this._cloneMenuitem(child, target.module, target.area));
                if (JSON.stringify(target.children.slice(target.staticCount)) !== JSON.stringify(children)) {
                    target.children.splice(target.staticCount, target.children.length - target.staticCount, ...children);
                    changed = true;
                }
            }
            return changed;
        };
        for (const event of source.dependsOn || []) {
            this._bus.addEventListener(event, () => {
                if (source.refresh()) this._bus.emit("xshell:menus:change", {});
            });
        }
    }

    // methods (private)
    _cloneMenuitem(menuitem, module, area) {
        const result = {
            label: menuitem.label,
            href: this._buildAreaHref(menuitem.href, area),
            path: this._buildAreaHref(menuitem.path, area),
            icon: menuitem.icon || null,
            module: module.id,
            default: menuitem.default || false,
            area: area.id,
            children: []
        };
        // clone static and dynamic children for this area
        if (menuitem.children) result.children.push(...menuitem.children.map(child => this._cloneMenuitem(child, module, area)));
        if (menuitem.childrenSource) {
            const source = this._sources[menuitem.childrenSource];
            if (!source) {
                console.warn(`Unknown menu source '${menuitem.childrenSource}'`);
            } else {
                this._sourceTargets[menuitem.childrenSource].push({ children: result.children, staticCount: result.children.length, module, area });
                result.children.push(...(source.resolve?.() || []).map(child => this._cloneMenuitem(child, module, area)));
            }
        } else {
            Object.freeze(result.children);
        }
        return Object.freeze(result);
    }
    _buildAreaHref(href, area) {
        if (!href) return null;
        if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(href)) return href;
        const path = href.startsWith("/") ? href : "/" + href;
        if (area.prefix && (path === area.prefix || path.startsWith(area.prefix + "/"))) return path;
        return area.prefix + path;
    }
    _getHrefIdentity(href) {
        if (!href) return href;
        const queryIndex = href.indexOf("?");
        const hashIndex = href.indexOf("#");
        const suffixIndexes = [queryIndex, hashIndex].filter(index => index !== -1);
        return suffixIndexes.length ? href.substring(0, Math.min(...suffixIndexes)) : href;
    }
    _normalizePrefix(prefix) {
        if (!prefix || prefix === "/") return "";
        return "/" + prefix.replace(/^\/+|\/+$/g, "");
    }
    _setCurrentArea(areaId) {
        if (this._currentAreaId === areaId) return;
        const area = this.getArea(areaId);
        if (!area) throw new Error(`Unknown area '${areaId}'`);
        this._currentAreaId = areaId;
        this._bus.emit("xshell:area:change", { areaId });
    }
    _matchesPrefix(href, prefix) {
        if (prefix === "") return href.startsWith("/");
        return href === prefix || href.startsWith(prefix + "/") || href.startsWith(prefix + "?");
    }
    _validateAreas(areas, defaultAreaId) {
        if (defaultAreaId && !areas.some(area => area.id === defaultAreaId)) throw new Error(`Default area '${defaultAreaId}' is not defined`);
        const prefixes = new Set();
        for (const area of areas) {
            if (prefixes.has(area.prefix)) throw new Error(`Duplicate area prefix '${area.prefix}'`);
            prefixes.add(area.prefix);
        }
    }
    _findMenuitemPath(items, predicate, parents = []) {
        for (const item of items || []) {
            const path = [...parents, item];
            if (predicate(item)) return path;
            const found = this._findMenuitemPath(item.children, predicate, path);
            if (found) return found;
        }
        return null;
    }
}
