import Timer from "../timer.js"
import Events from "../events.js"
import xshell from "xshell";


// utils
function kebabToCamel(str) {
    return str.split('-')
        .map((word, index) => index === 0 ? word : word.charAt(0).toUpperCase() + word.slice(1))
        .join('');
};
function camelToKebab(str) {
    return str
      .replace(/([a-z])([A-Z])/g, '$1-$2') 
      .toLowerCase();                      
}
function findClosestXPage(element) {
    let current = element;
    while (current) {
        if (current.tagName === "X-PAGE") return current;
        if (current.parentNode) {
            current = current.parentNode;
            continue;
        }
        const root = current.getRootNode();
        if (root && root.host) {
            current = root.host;
            continue;
        }
        return null;
    }
    return null;
}


// create page class from js definition
export async function createComponentClassFromJsDefinition(src, context, definition) {
    // stylesheets
    const stylesheets = []
    if (typeof(definition.style) == "string") {
        const stylesheet = new CSSStyleSheet();
        stylesheet.replaceSync(definition.style);
        stylesheets.push(stylesheet);
    } else if (Array.isArray(definition.style)) {
        for(let styleText of definition.style) {
            const stylesheet = new CSSStyleSheet();
            stylesheet.replaceSync(styleText);
            stylesheets.push(stylesheet);
        }
    }
    // state skeleton
    let stateSkeleton = {};
    let stateAttributeNames = []
    let stateReflectedAttributeNames = []
    let stateMapAttributeNames = []
    for(let propName in definition.state) {
        const propDefinition = definition.state[propName];
        if (typeof(propDefinition.value) == "undefined") propDefinition.value = null;
        if (typeof(propDefinition.type) == "undefined") propDefinition.type = "string";
        if (typeof(propDefinition.attr) == "undefined") propDefinition.attr = false;
        if (typeof(propDefinition.prop) == "undefined") propDefinition.prop = (propDefinition.attr ? true : false);
        if (typeof(propDefinition.reflect) == "undefined") propDefinition.reflect = false;
        stateSkeleton[propName] = propDefinition.value; 
        if (propDefinition.attr === true) stateAttributeNames.push(propName);
        if (propDefinition.attr === true && propDefinition.reflect) stateReflectedAttributeNames.push(propName);
        if (propDefinition.value && typeof(propDefinition.value) === "object" && !Array.isArray(propDefinition.value) && Object.keys(propDefinition.value).length === 0) stateMapAttributeNames.push(propName);
    }
    // state engine
    const stateEngineXShell = xshell.config.get(`component.stateEngine`);
    const stateEngineModule = xshell.config.get(`modules.${context.resourceDefinition.module}.component.stateEngine`, stateEngineXShell);
    const stateEngineComponent = definition.meta.stateEngine || stateEngineModule;
    const stateEngineFactoryCreator = await xshell.loader.load("state-engine:" + stateEngineComponent);
    const stateEngineFactory = new stateEngineFactoryCreator(stateSkeleton, definition.state, context);
    // render engine
    const renderEngineXShell = xshell.config.get(`component.renderEngine`);
    const renderEngineModule = xshell.config.get(`modules.${context.resourceDefinition.module}.component.renderEngine`, renderEngineXShell);
    const renderEngineComponent = definition.meta.renderEngine || renderEngineModule;
    const renderEngineFactoryCreator = await xshell.loader.load("render-engine:" + renderEngineComponent);
    const renderEngineFactory = new renderEngineFactoryCreator(definition.template, context);
    // render engine dependencies
    if (renderEngineFactory.dependencies.length) {
        await xshell.loader.load(renderEngineFactory.dependencies);
    }    
    // init 
    renderEngineFactory.init();
    // returns a class that extends base class component
    const WebComponent = class extends HTMLElement {
        // vars
        _state = null;
        _stateChanges = [];
        _disposables = [];
        // static
        static get observedAttributes() { 
            return stateAttributeNames;
        }
        // ctor
        constructor() {
            super();
            const self = this;
            //shadowRoot
            this.attachShadow(definition.meta.shadowRootOptions || {mode: "open"});
            this.shadowRoot.adoptedStyleSheets.push(...stylesheets);
            // state
            this._state = stateEngineFactory.create({
                stateChange(prop, oldValue, newValue) {
                    // state changed
                    self.stateChange(prop, oldValue, newValue);
                }, invalidate(path) {
                    // invalidate
                    self.invalidate(path);
                }
            });
            // services provider
            const servicesProvider = new Proxy({}, {
                get: (obj, prop) => {
                    if (prop == "definition") {
                        // definition of component
                        return definition;
                    } else if (prop == "state") {
                        // state
                        return self._state;
                    } else if (prop == "timer") {
                        // timer helper
                        const timer = new Timer( (command) => {self.onCommand(command);} );
                        self._disposables.push(timer);
                        return timer;
                    } else if (prop == "events") {
                        // events helper
                        const events = new Events( (command) => {self.onCommand(command);} );
                        self._disposables.push(events);
                        return events;
                    } else if (prop == "getPage") {
                        // get current page function
                        return function() {
                            const xpage = findClosestXPage(self);
                            return (xpage ? xpage.page : null);
                        }
                    } else {
                        // resolve from services
                        return xshell.services.resolve(prop);
                    }                    
                }
            });
            // set methods
            const methods = definition.script?.(servicesProvider) ?? {};
            // bind methods to the instance
            Object.assign(this, methods);
            // attribute mutation observer (listen for changes in attributes that starts with state map attribute names, ex: qs-*)
            if (stateMapAttributeNames.length) {
                const mutationObserver = new MutationObserver((mutationsList) => {
                    for (let mutation of mutationsList) {
                        if (mutation.type === "attributes") {
                            const attrName = mutation.attributeName;
                            const propName = stateMapAttributeNames.find(name => attrName.startsWith(name + "-"));
                            if (propName) {
                                const subPropName = kebabToCamel(attrName.substring(propName.length + 1));
                                const attrValue = this.getAttribute(attrName);
                                const propValue = this._state[propName] || {};
                                propValue[subPropName] = attrValue;
                                this._state[propName] = propValue;
                            }
                        }
                    }
                });
                mutationObserver.observe(this, { attributes: true });
                // init state from attributes
                for(let attrName of stateMapAttributeNames) {                    
                    const attrPrefix = attrName + "-";
                    for(let attr of this.attributes) {
                        if (attr.name.startsWith(attrPrefix)) {
                            const propName = kebabToCamel(attrName);
                            const subPropName = kebabToCamel(attr.name.substring(attrPrefix.length));
                            const propValue = this._state[propName] || {};
                            propValue[subPropName] = attr.value;
                            this._state[propName] = propValue;
                        }
                    }
                }
            }
            // load
            this.onCommand("load", {});
        }
        // attributeChangedCallback
        attributeChangedCallback(name, oldValue, newValue) {
            let prop = kebabToCamel(name);
            let oldPropValue = this._state[prop];
            if (typeof(oldPropValue) == "boolean") {
                newValue = (newValue != null);
            } else if (typeof(oldPropValue) == "number") {
                newValue = Number(newValue);
            }
            if (oldPropValue != newValue) {
                this._state[prop] = newValue;
            }
        }
        // connected/disconnected
        connectedCallback() {
            this._renderEngine = renderEngineFactory.create({ host: this.shadowRoot, state: this._state, handler:(command, ...params) => {
                this.onCommand(command, ...params);
            }, invalidate: () => { 
                this.invalidate(); 
            } });
            this._renderEngine.mount();
            this.onCommand("mount", {});
            this.invalidate();
        }
        disconnectedCallback() {
            this.onCommand("unmount", {});
            this.onCommand("unload", {});
            this._renderEngine.unmount();
            for(let disposable of this._disposables) {
                disposable.dispose();
            }
        }
        // stateChange(prop, oldValue, newValue) {
        stateChange(prop, oldValue, newValue) {
            this._stateChanges.push({prop, oldValue, newValue});
            // reflect to attribute if needed
            if (stateReflectedAttributeNames.includes(prop)) {
                const attrName = camelToKebab(prop);
                if (newValue === false || newValue === null) {
                    this.removeAttribute(attrName);
                } else {
                    this.setAttribute(attrName, (newValue === true ? "" : newValue));
                }
            }
        }
        // invalidate
        invalidate(path) {
            if (this._renderPending) return;
            this._renderPending = true;
            requestAnimationFrame(() => {
                this.onCommand("stateChange", {changes: this._stateChanges});
                this._stateChanges = [];
                this._renderPending = false;
                this._renderEngine.render();
            });
        }
        // onCommand
        onCommand(command, params) {
        }
    };
    // add properties
    for(const propName in definition.state) {
        const propDefinition = definition.state[propName];
        if (propDefinition.prop) {
            Object.defineProperty(WebComponent.prototype, propName, {
                get() {
                    return this._state[propName];
                },
                set(newValue) {
                    this._state[propName] = newValue;
                },
                enumerable: true,
                configurable: false
            });
        }
    }
    // register
    if (!window.customElements.get(definition.meta.name)) {
        window.customElements.define(definition.meta.name, WebComponent);
    }
    // return class
    return WebComponent
}

//export 
export default class LoaderComponentJs {
    async load(src, context) {
        // import
        const module = await import(src);
        let definition = module.default;
        // check if its a promise
        if (typeof(definition) === "object" && typeof(definition.then) === "function") {
            definition = await definition;
        }
        // check if its a class
        if (typeof(definition) === "function"  && /^class\s/.test(Function.prototype.toString.call(definition))) {
            return definition;
        }
        // else, asume its a definition object
        if (!definition.meta) definition.meta = {};
        if (!definition.meta.name) {
            let aux = src.split("?")[0];
            aux = aux.substring(aux.lastIndexOf("/")+1).split(".")[0];
            definition.meta.name = aux;
        }
        definition = Object.seal(Object.freeze(definition));
        // create class definition
        return await createComponentClassFromJsDefinition(src, context, definition);        
    }
};