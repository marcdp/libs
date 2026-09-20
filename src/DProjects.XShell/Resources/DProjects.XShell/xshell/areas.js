// default
export default class Areas {

    // vars
    _bus = null;
    _areas = [];
    _currentAreaId = null;

    // ctor
    constructor({ config, bus }) {
        this._bus = bus;

        const areasConfig = config.xshell.areas || {};
        const definitions = areasConfig.definitions || {};
        const defaultAreaId = areasConfig.default || null;

        // create areas
        const areas = [];

        for (const [areaId, areaConfig] of Object.entries(definitions)) {
            const prefix = this._normalizePrefix(areaConfig.prefix);

            areas.push(Object.freeze({
                ...areaConfig,
                id: areaId,
                label: areaConfig.label || areaId,
                icon: areaConfig.icon || null,
                prefix,
                home: areaConfig.home || null,
                modules: Object.freeze([...(areaConfig.modules || [])]),
                order: areaConfig.order || 0,
                default: areaId === defaultAreaId
            }));
        }

        // validate
        this._validateAreas(areas, defaultAreaId);

        // sort for presentation
        areas.sort((a, b) => {
            if (a.default !== b.default) return a.default ? -1 : 1;
            if (a.order !== b.order) return a.order - b.order;
            return a.label.localeCompare(b.label);
        });

        this._areas = Object.freeze(areas);

        // current area initially defaults to configured default
        this._currentAreaId = this.getDefaultArea()?.id || null;

        // navigation determines current area from URL prefix
        bus.addEventListener("xshell:navigation:end", evt => {
            const areaId = this.resolveAreaId(evt.detail.src);
            if (areaId) this._setCurrentArea(areaId);
        });
    }

    // areas
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

    // resolve area from navigation URL
    resolveAreaId(href) {
        if (!href) return null;

        // longest prefix first, so /admin/tools wins over /admin
        const areas = [...this._areas]
            .sort((a, b) => b.prefix.length - a.prefix.length);

        for (const area of areas) {
            if (this._matchesPrefix(href, area.prefix)) {
                return area.id;
            }
        }

        return null;
    }
    

    // private
    _setCurrentArea(areaId) {
        if (this._currentAreaId === areaId) return;

        const area = this.getArea(areaId);
        if (!area) throw new Error(`Unknown area '${areaId}'`);

        this._currentAreaId = areaId;
        this._bus.emit("xshell:area:change", { areaId });
    }

    _normalizePrefix(prefix) {
        prefix = prefix || "/";

        if (!prefix.startsWith("/")) prefix = "/" + prefix;
        if (prefix.length > 1 && prefix.endsWith("/")) prefix = prefix.slice(0, -1);

        return prefix;
    }

    _matchesPrefix(href, prefix) {
        if (prefix === "/") return href.startsWith("/");

        return href === prefix ||
               href.startsWith(prefix + "/") ||
               href.startsWith(prefix + "?");
    }

    _validateAreas(areas, defaultAreaId) {
        if (defaultAreaId && !areas.some(area => area.id === defaultAreaId)) {
            throw new Error(`Default area '${defaultAreaId}' is not defined`);
        }

        const prefixes = new Set();

        for (const area of areas) {
            if (prefixes.has(area.prefix)) {
                throw new Error(`Duplicate area prefix '${area.prefix}'`);
            }

            prefixes.add(area.prefix);
        }
    }
}