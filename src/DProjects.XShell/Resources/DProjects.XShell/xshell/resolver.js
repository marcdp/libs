
// class
export default class Resolver {


    //vars
    _debug = null;
    _definitions = [];
    

    //ctor
    constructor( {debug, config}) {
        this._debug = debug;
        for(let key of config.getKeys("resolver")) {
            this.addDefinition(key.substring(key.indexOf(".") + 1), config.get(key));
        }
    }


    //methods
    addDefinition(key, src) {
        let type = key.split(":")[0];
        let pattern = key.split(":")[1];
        let resource = type + ":" + pattern;
        //regexp
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
        // attributes
        let attributes= {};
        if (src.includes(";")) {
            let parts = src.split(";");
            src = parts[0].trim();
            for(let a = 1; a < parts.length; a++) {
                let attr = parts[a].trim();
                if (attr.includes("=")){
                    let attrName = attr.split("=")[0].trim();
                    let attrValue = attr.split("=")[1].trim();
                    if (attrValue=="true") attrValue = true;
                    if (attrValue=="false") attrValue = false;
                    attributes[attrName] = attrValue;
                }
            }
        }
        //add definition
        let definition = {
            resource,
            src,
            regexp: new RegExp(regexp), 
            ...attributes
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
                let src = definition.src;
                for(var key in match.groups) {
                    src = src.replaceAll("{" + key + "}", match.groups[key]);
                }
                const path = src;
                if (src.indexOf(":")==-1) {
                    src = (document.location.pathname + src).replaceAll("//", "/");
                }
                return { definition, src, path };
            }
        }
        this._debug.error(`resolver.resolveDefinition('${resource}'): unable to resolve`);
        return null;
    }
    resolveUrl(resource) {
        if (resource.startsWith("http://") || resource.startsWith("https://") || resource.startsWith("//")) return resource;
        if (resource.startsWith("/")) return (document.location.pathname + resource).replaceAll("//", "/");
        let result = this.resolve(resource);
        if (result) return result.src;
        return null;
    }
};
