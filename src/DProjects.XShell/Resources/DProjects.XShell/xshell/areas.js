
// default
export default class Areas {
    
    
    // vars
    _currentAreaName = null;
    _areas = [];


    // ctor
    constructor({ config, bus }) {
        // listen to navigation end to track current area
        bus.addEventListener("xshell:navigation:end", (evt) => {
            const src = evt.detail.src;
            const areaId = this.resolveAreaId(src);
            if (this._currentAreaName != areaId) {
                this._currentAreaName = areaId;
                bus.emit("xshell:area:change", { areaId: areaId } );
            }
        });
        // create areas
        const areasConfig = config.xshell.areas;
        for (const [areaId, areaConfig] of Object.entries(areasConfig.definitions)) {
            const area = {
                id: areaId,
                label: areaConfig.label || areaId,
                icon: areaConfig.icon || null,
                prefix: areaConfig.prefix || "",
                home: areaConfig.home || null,
                modules: areaConfig.modules || [],
                order: areaConfig.order || 0,
                default: areasConfig.default === areaId
            };
            this._areas.push(Object.freeze(area));
        }
        // sort areas by default, order, label
        this._areas.sort( (a,b) => {
            if (a.default != b.default) return a.default ? -1 : 1;
            if (a.order == b.order) return a.label.localeCompare(b.label);
            return (a.order - b.order);
        });
        // freeze areas
        this._areas = Object.freeze(this._areas);
    }

    // get areas
    getAreas() {
        return this._areas;
    }
    getDefaultArea() {
        return this._areas.find( area => area.default ) || this._areas[0];
    }
    getCurrentArea() {
        return this._areas.find(area => area.id === this._currentAreaName) || this.getDefaultArea();
    }
    resolveAreaId(src) {
        // get area id by src
        // search by prefixes
        for (const area of this._areas) {
            for(const prefix of [area.prefix]) {
                if (src.startsWith(prefix)) {
                    return area.id;
                }
            }
        }
        return null;
    }

}

