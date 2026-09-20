import { findObjectsPath } from "./utils/object.js";

// class
export default class Menus {

    // vars
    _bus = null;
    _areas = null;
    _modules = null;

    // dynamic menu sources registered by modules
    _sources = {};

    // effective menus by area:
    // _cache[areaId][menuName] = [...]
    _cache = {};

    // ctor
    constructor({ bus, areas, modules }) {
        this._bus = bus;
        this._areas = areas;
        this._modules = modules;

        // the effective menus change when the current area changes
        this._bus.addEventListener("xshell:area:change", () => {
            this._bus.emit("xshell:menus:changed", {});
        });
    }

    // init
    init() {
        const cache = {};
        // build the effective menus of every area
        for (const area of this._areas.getAreas()) {
            const areaMenus = {};
            // modules participate in the area in the declared order
            for (const moduleId of area.modules) {
                const module = this._modules.getModuleById(moduleId);
                if (!module) {
                    console.warn(`Area '${area.id}' references unknown module '${moduleId}'`);
                    continue;
                }
                // append the menu contributions of the module
                for (const [menuName, menuItems] of Object.entries(module.config.menus || {})) {
                    if (!Array.isArray(menuItems)) continue;
                    areaMenus[menuName] ??= [];
                    areaMenus[menuName].push(...menuItems.map(menuitem =>
                            this._cloneMenuitem(menuitem, module, area)
                        )
                    );
                }
            }
            cache[area.id] = areaMenus;
        }
        this._cache = cache;
    }

    // menus
    getMenu(name, areaId = null) {
        const area = areaId ? this._areas.getArea(areaId) : this._areas.getCurrentArea();
        if (!area) return [];
        return this._cache[area.id]?.[name] || [];
    }

    getMenuitemBreadcrumb(href, areaId = null) {
        const area = areaId ? this._areas.getArea(areaId) : this._areas.getCurrentArea();
        if (!area || !href) return null;

        const menus = this._cache[area.id] || {};
        const hrefs = this._getHrefVariants(href);

        for (const targetHref of hrefs) {
            for (const menu of Object.values(menus)) {
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

    // sources
    registerSource(name, source) {
        this._sources[name] = source;

        for (const event of source.dependsOn || []) {
            this._bus.addEventListener(event, () => {
                const changed = source.refresh?.();

                if (changed) {
                    this._bus.emit("xshell:menus:changed", {});
                }
            });
        }
    }

    // private
    _cloneMenuitem(menuitem, module, area) {
        const result = {
            label: menuitem.label,
            href: this._buildHref(menuitem.href, module, area),
            icon: menuitem.icon || null,
            module: module.id,
            area: area.id,
            children: []
        };

        // static children
        if (menuitem.children) {
            result.children.push(
                ...menuitem.children.map(child =>
                    this._cloneMenuitem(child, module, area)
                )
            );
        }

        // dynamic children
        if (menuitem.childrenSource) {
            const source = this._sources[menuitem.childrenSource];

            if (!source) {
                console.warn(`Unknown menu source '${menuitem.childrenSource}'`);
            } else {
                source.refresh = () => {
                    const menuitems = source.resolve?.() || [];

                    result.children.length = 0;
                    result.children.push(
                        ...menuitems.map(child =>
                            this._cloneMenuitem(child, module, area)
                        )
                    );

                    return true;
                };

                source.refresh();
            }
        }

        return Object.freeze(result);
    }

    _buildHref(href, module, area) {
        if (!href) return null;
        // absolute/external URL
        if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(href)) {
            return href;
        }
        // module-relative URL:
        // /pages/page.js -> /_assets/<module>/pages/page.js
        const moduleHref = (href.startsWith("/") ? href : "/" + href);
        // area navigation prefix:
        // /_assets/reports/pages/page.js
        // -> /customers/_assets/reports/pages/page.js
        if (area.prefix === "/") {
            return moduleHref;
        }
        // return
        return area.prefix + moduleHref;
    }

    _getHrefVariants(href) {
        const result = [href];
        const hashIndex = href.indexOf("#");
        if (hashIndex !== -1) {
            result.push(href.substring(0, hashIndex));
        }
        const queryIndex = href.indexOf("?");
        if (queryIndex !== -1) {
            result.push(href.substring(0, queryIndex));
        }
        return [...new Set(result)];
    }
}