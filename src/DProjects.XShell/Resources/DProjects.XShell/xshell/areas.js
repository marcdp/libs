
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
            const areaName = this.resolveAreaName(src);
            if (this._currentAreaName != areaName) {
                this._currentAreaName = areaName;
                bus.emit("xshell:area:change", { area: areaName } );
            }
        });
        // create areas
        let areas = [];
        const assetsPrefix = config.get("xshell.assetsPrefix");
        for(let areaName of config.getSubKeys("xshell.areas")) {
            let area = config.getAsObject(`xshell.areas.${areaName}`);
            area.id = areaName;
            area.default = area.default || (config.get(`xshell.areaDefault`) == areaName);
            area.label = area.label || areaName;
            area.order = area.order || 0;

            area.prefixes = [];
            area.prefixes.push("/" + areaName + "/");

            areas.push(area);
        }
        // sort areas by default, order, label
        areas.sort( (a,b) => {
            if (a.default != b.default) return a.default ? -1 : 1;
            if (a.order == b.order) return a.label.localeCompare(b.label);
            return (a.order - b.order);
        });
        // freeze areas
        this._areas = Object.freeze(areas);
    }

    // get areas
    getAreas() {
        return this._areas;
    }
    getDefaultArea() {
        return this._areas.find( area => area.default ) || this._areas[0];
    }
    getCurrentArea(){
        return this._areas.find(area => area.name === this._currentAreaName) || this.getDefaultArea();
    }
    resolveAreaName(src) {
        //get area name by src
        // search by prefixes
        for (const area of this._areas) {
            for(const prefix of area.prefixes) {
                if (src.startsWith(prefix)) {
                    return area.name;
                }
            }
        }
        return null;
    }

}

