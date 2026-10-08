

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
        // compile pattern
        let regexp = "^";
        let index = 0;
        let previousTokenWasPlaceholder = false;
        const placeholders = new Set();
        // parse literal text and placeholders without exposing regular expression syntax
        while (index < resource.length) {
            const literalStart = index;
            while (index < resource.length && resource[index] !== "{" && resource[index] !== "}") {
                index++;
            }
            if (index > literalStart) {
                regexp += resource.substring(literalStart, index).replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
                previousTokenWasPlaceholder = false;
            }
            if (index === resource.length) {
                break;
            }
            if (resource[index] === "}") {
                throw new Error(`Invalid resolver pattern '${resource}': unmatched '}'.`);
            }
            if (previousTokenWasPlaceholder) {
                throw new Error(`Invalid resolver pattern '${resource}': adjacent placeholders are not supported.`);
            }
            // validate the placeholder before compiling its named capture
            const end = resource.indexOf("}", index + 1);
            if (end === -1) {
                throw new Error(`Invalid resolver pattern '${resource}': unmatched '{'.`);
            }
            const name = resource.substring(index + 1, end);
            if (name.length === 0) {
                throw new Error(`Invalid resolver pattern '${resource}': empty placeholder name.`);
            }
            if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(name)) {
                throw new Error(`Invalid resolver pattern '${resource}': invalid placeholder name '${name}'.`);
            }
            if (placeholders.has(name)) {
                throw new Error(`Invalid resolver pattern '${resource}': duplicate placeholder '${name}'.`);
            }
            placeholders.add(name);
            regexp += `(?<${name}>.+?)`;
            index = end + 1;
            previousTokenWasPlaceholder = true;
        }
        const regexpCompiled = new RegExp(regexp + "$");
        //add definition
        let definition = {
            resource,
            src: value.src,
            regexp: regexpCompiled,
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
                let url = isAbsoluteUrl ? definition.src : new URL(definition.src.replace(/^\/+/, ""), this._appBaseUrl).href.replaceAll("%7B", "{").replaceAll("%7D", "}");
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
