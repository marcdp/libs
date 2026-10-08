
// class
export default class Resolver {


    //vars
    _debug = null;
    _definitions = [];
    _appBasePath = "";
    _appBaseUrl = "";

    //ctor
    constructor( {debug, config}) {
        this._debug = debug;
        this._appBasePath = config.app.basePath;
        this._appBaseUrl = config.app.baseUrl;
        for(let type in config.xshell.resolver) {
            for(let pattern in config.xshell.resolver[type]) {
                const value = config.xshell.resolver[type][pattern]; 
                this.addDefinition(type + ":" + pattern, value);
            }
        }
        // sort definitions by definition.resource alphabetically
        this._definitions.sort((a, b) => a.resource.localeCompare(b.resource));
    }

    // props
    get registry() {
        // expose rule metadata without sharing mutable definitions or regular expressions
        return Object.freeze(this._definitions.map(definition => Object.freeze({
            resource: definition.resource,
            src: definition.src,
            loader: definition.loader,
            cache: definition.cache,
            cacheMode: definition.cacheMode
        })));
    }

    //methods
    addDefinition(resource, value) {
        //regexp
        resource  = resource.replaceAll("/", "\\/");
        resource  = resource.replaceAll(".", "\\.");
        let regexp = "^";
        let k = 0;
        let i = resource.indexOf("{"), j = resource.indexOf("}");
        while (i != -1) {
            regexp += resource.substring(k, i);
            regexp += "(?<" + resource.substring(i + 1, j) + ">.+)";
            k = j + 1;
            i = resource.indexOf("{", j), j = resource.indexOf("}", i);
        }
        regexp += resource.substring(k) + "$";
        //add definition
        let definition = {
            resource,
            src: value.src,
            regexp: new RegExp(regexp), 
            ...value
        };
        this._definitions.push(definition);
    }
    has(resource) {        
        for(let i = 0; i < this._definitions.length ; i++) {
            let definition = this._definitions[i];
            let match = resource.match(definition.regexp);
            if (match) {
                return true;
            }
        }
        return false;
    }
    resolve(resource) {                
        if (resource.indexOf("#") != -1) resource = resource.split("#")[0];
        if (resource.indexOf("?") != -1) resource = resource.split("?")[0];        
        for(let i = 0; i < this._definitions.length ; i++) {
            const definition = this._definitions[i];
            if (definition.resource === resource) {
                const isAbsoluteUrl = /^[A-Za-z][A-Za-z0-9+.-]*:/.test(definition.src);
                let url = isAbsoluteUrl ? definition.src : new URL(definition.src.replace(/^\/+/, ""), this._appBaseUrl).href;
                let path = isAbsoluteUrl ? null : definition.src;
                return { definition, url, path };
            }
        }
        for(let i = this._definitions.length - 1; i >= 0 ; i--) {
            const definition = this._definitions[i];
            const match = resource.match(definition.regexp);
            if (match) {
                const isAbsoluteUrl = /^[A-Za-z][A-Za-z0-9+.-]*:/.test(definition.src);
                let url = isAbsoluteUrl ? definition.src : new URL(definition.src.replace(/^\/+/, ""), this._appBaseUrl).href.replace("%7B","{").replace("%7D","}");
                let path = isAbsoluteUrl ? null : definition.src;
                for(var key in match.groups) {
                    url = url.replaceAll("{" + key + "}", match.groups[key]);
                    if (path !== null) path = path.replaceAll("{" + key + "}", match.groups[key]);
                }                
                return { definition, url, path };
            }
        }
        console.error(`resolver.resolveDefinition('${resource}'): unable to resolve`);
        return null;
    }
};
