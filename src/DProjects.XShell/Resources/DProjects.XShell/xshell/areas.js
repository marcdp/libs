import { findObjectsPath } from "./utils/object.js";

// class
export default class Areas {

    // vars
    _bus = null;
    _areas = [];
    _currentAreaId = null;
    _sources = {};
    _sourceTargets = {};

    // ctor
    constructor({ config, bus }) {
        this._bus = bus;
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
                if (item.default) return item.href || null;
                const href = findDefaultHref(item.children);
                if (href) return href;
            }
            return null;
        };
        for (const area of this._areas) {
            const menus = {};
            for (const moduleId of area.modules) {
                const module = modules.getModuleById(moduleId);
                if (!module) {
                    console.warn(`Area '${area.id}' references unknown module '${moduleId}'`);
                    continue;
                }
                for (const [menuName, menuItems] of Object.entries(module.config.menus || {})) {
                    if (!Array.isArray(menuItems)) continue;
                    menus[menuName] ??= [];
                    menus[menuName].push(...menuItems.map(menuitem => this._cloneMenuitem(menuitem, module, area)));
                }
            }
            for (const items of Object.values(menus)) Object.freeze(items);
            area.menus = Object.freeze(menus);
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
    getMenu(name, areaId = null) {
        const area = areaId ? this.getArea(areaId) : this.getCurrentArea();
        return area?.menus[name] || [];
    }
    getMenuitemBreadcrumb(href, areaId = null) {
        const area = areaId ? this.getArea(areaId) : this.getCurrentArea();
        if (!area || !href) return null;
        // search only the selected area's effective menus
        for (const targetHref of this._getHrefVariants(href)) {
            for (const menu of Object.values(area.menus)) {
                const menuitems = findObjectsPath(menu, "href", targetHref);
                if (menuitems) {
                    return menuitems.map(menuitem => ({
                        label: menuitem.label,
                        href: menuitem.href,
                        ...(menuitem.icon ? { icon: menuitem.icon } : {}),
                        module: menuitem.module,
                        area: menuitem.area
                    }));
                }
            }
        }
        return null;
    }
    registerSource(name, source) {
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
        return area.prefix + path;
    }
    _getHrefVariants(href) {
        const result = [href];
        const hashIndex = href.indexOf("#");
        if (hashIndex !== -1) result.push(href.substring(0, hashIndex));
        const queryIndex = href.indexOf("?");
        if (queryIndex !== -1) result.push(href.substring(0, queryIndex));
        return [...new Set(result)];
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
}
