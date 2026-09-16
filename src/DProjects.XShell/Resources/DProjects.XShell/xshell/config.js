
// class
export default class Config {

    //vars
    _config = {};
    
    //ctor
    constructor({ debug, bus, value }) {
        this._debug = debug;
        this._bus = bus;
        for (let key in value) {
            this._config[key] = value[key];
        }
        Object.freeze(this._config);
    }

    //props
    get config() { return this._config; }

    //methods
    has(key) {
        let result = this._config[key];
        if (typeof (result) != "undefined") return true;
        return false;
    }
    get(key, defaultValue) {
        let result = this._config[key];
        if (typeof (result) != "undefined") return result;
        if (typeof (defaultValue) == "undefined") {
            this._debug.warn(`config.get('${key}') configuration key is undefined`);
        }
        return defaultValue;
    }
    getKeys(prefix) {
        let result = [];
        for (let key in this._config) {
            if (key.startsWith(prefix + ".")) {
                result.push(key);
            };
        }
        return result;
    }
    getSubKeys(prefix) {
        let result = [];
        for (let key in this._config) {
            if (key.startsWith(prefix + ".")) {
                let subKey = key.substring(prefix.length + 1).split(".")[0];
                if (result.indexOf(subKey)==-1) {
                    result.push(subKey);
                }
            };
        }
        return result;
    }
    getAsObject(prefix) {
        let result = {};
        for (let key in this._config) {
            if (key.startsWith(prefix + ".")) {
                result[key.substring(prefix.length + 1)] = this._config[key];
            }
        }
        return result;
    }
    getAsObjects(prefix) {
        let result = [];
        let subKeys = this.getSubKeys(prefix);
        for (let subKey of subKeys) {
            let value = {};
            for (let key in this._config) {
                let aux = prefix + "." + subKey;
                if (key == aux) {
                    value.name = this._config[key];
                } else if (key.startsWith(aux + ".")) {
                    value[key.substring(aux.length+1)] = this._config[key];
                }
            }
            result.push(value);
        }
        return result;
    }
    
};


