import { createPageClassFromJsDefinition } from "./page-js.js"
import { normalizeModuleResourceUrl } from "../utils/rewriteDocumentUrls.js";

//export 
export default class LoaderPageHtml {
    async load(src, context) {
        const request = await fetch(src);
        if (!request.ok) throw new Error(`Error loading page: ${request.status} ${request.statusText}: ${src}`);
        const html = await request.text();
        const doc = (new DOMParser()).parseFromString(html, "text/html");
        // definition
        let definition = {
            meta: {
                title: ""
            },
            state: {},
            template: "",
            style: [],
        }        
        // meta
        definition.meta.title = doc.title;
        const validMetas = ["description", "author", "page.renderEngine", "page.stateEngine", "page.icon"]
        for(let meta of doc.getElementsByTagName('meta')){
            let key = meta.getAttribute('name') || meta.getAttribute('property');
            let value = meta.getAttribute('content');
            if (validMetas.includes(key)) {
                if (key.startsWith("page.")) key = key.substring(key.indexOf(".")+1);
                definition.meta[key] = value;
            }
        }
        // style
        let styles = doc.querySelectorAll("style");
        for(let style of styles) {
            style.parentNode.removeChild(style);
            definition.style.push(style.textContent.trim());
        }
        // state
        let states = doc.querySelectorAll("script[type='application/json']");
        if (states.length > 1) throw new Error(`Error loading page: multiple script[type='application/json'] tags detected: only one allowed: ${src}`);
        if (states.length == 1){
            let state = states[0];
            state.parentNode.removeChild(state);
            definition.state = JSON.parse(state.textContent);
        }
        // script
        let scripts = doc.querySelectorAll("script");
        if (scripts.length > 1) throw new Error(`Error loading page: multiple script tags detected: only one allowed: ${src}`);
        if (scripts.length == 1) {
            let script = scripts[0];
            let scriptSrc = script.getAttribute("src");
            scriptSrc = normalizeModuleResourceUrl(scriptSrc, context.appBase + context.resourceDefinition.modulePath, src);
            script.parentNode.removeChild(script);  
            let module = await import(scriptSrc);
            let moduleDefinition = module.default;
            if (moduleDefinition.script) definition.script = moduleDefinition.script;
            if (moduleDefinition.meta) definition.meta = {...definition.meta, ...moduleDefinition.meta};
            if (moduleDefinition.state) definition.state = {...definition.state, ...moduleDefinition.state};
            if (moduleDefinition.template) definition.template = moduleDefinition.template;
            if (moduleDefinition.style) definition.style = moduleDefinition.style;            
        }
        // template
        definition.template = doc.body.innerHTML;
        // create page l
        const pageClass = createPageClassFromJsDefinition(src, context, definition);
        // return
        return pageClass;
    }
};