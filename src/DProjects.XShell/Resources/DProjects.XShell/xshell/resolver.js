
// class
export default class Resolver {


    //vars
    _debug = null;
    _definitions = [];
    _appBasePath = "";

    //ctor
    constructor( {debug, config}) {
        this._debug = debug;
        this._appBasePath = config.app.basePath;
        for(let type in config.xshell.resolver) {
            for(let pattern in config.xshell.resolver[type]) {
                const value = config.xshell.resolver[type][pattern]; 
                this.addDefinition(type + ":" + pattern, value);
            }
        }
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
            url: value.url,
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
            const match = resource.match(definition.regexp);
            if (match) {
                let url = definition.url;
                for(var key in match.groups) {
                    url = url.replaceAll("{" + key + "}", match.groups[key]);
                }
                const path = url;
                if (url.indexOf(":")==-1) {
                    url = (this._appBasePath + url);
                }
                return { definition, url, path };
            }
        }
        this._debug.error(`resolver.resolveDefinition('${resource}'): unable to resolve`);
        return null;
    }
    resolveUrl(resource) {
        if (resource.startsWith("http://") || resource.startsWith("https://") || resource.startsWith("//")) return resource;
        if (resource.startsWith("/")) return (document.location.pathname + resource).replaceAll("//", "/");
        let result = this.resolve(resource);
        if (result) return result.url;
        return null;
    }
};
