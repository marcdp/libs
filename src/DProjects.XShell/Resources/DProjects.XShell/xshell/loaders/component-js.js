import Timer from "../timer.js"
import Events from "../events.js"
import xshell from "xshell";


// utils
function kebabToCamel(str) {
    return str.split('-').map((word, index) => index === 0 ? word : word.charAt(0).toUpperCase() + word.slice(1)).join('');
};
function camelToKebab(str) {
    return str.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();                      
}
function isEmptyPlainObject(value) {
    return value && typeof(value) === "object" && !Array.isArray(value) && Object.getPrototypeOf(value) === Object.prototype && Object.keys(value).length === 0;
}
function convertAttributeValue(property, value) {
    switch (property.type) {
        case "boolean":
            return value !== null;
        case "number":
            return value === null ? null : Number(value);
        case "array":
        case "object":
            if (value === null) return null;
            try {
                return JSON.parse(value);
            } catch {
                return value;
            }
        default:
            return value;
    }
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
export async function createComponentClassFromJsDefinition(src, context, definition, contract) {
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
    const properties = contract?.properties || {};
    const internalState = definition.state || {};
    const stateSkeleton = {};
    const propertyAttributeNames = [];
    const reflectedPropertyNames = [];
    const stateMapAttributes = [];
    for (const [propName, property] of Object.entries(properties)) {
        if (property.state === true) {
            stateSkeleton[propName] = property.default;
        }
        if (property.attr === true) {
            propertyAttributeNames.push(camelToKebab(propName));
        }
        if (property.reflect === true) {
            reflectedPropertyNames.push(propName);
        }
        if (property.state === true && property.attr === true && isEmptyPlainObject(property.default)) {
            stateMapAttributes.push({ attributePrefix: camelToKebab(propName) + "-", stateName: propName });
        }
    }
    for (const [stateName, value] of Object.entries(internalState)) {
        if (Object.prototype.hasOwnProperty.call(stateSkeleton, stateName)) {
            throw new Error(`Component '${definition.meta?.name || src}' declares state '${stateName}' in both contract.properties and definition.state.`);
        }
        stateSkeleton[stateName] = value;
        if (isEmptyPlainObject(value)) {
            stateMapAttributes.push({ attributePrefix: camelToKebab(stateName) + "-", stateName });
        }
    }
    // state engine
    const stateEngineXShell = xshell.config.xshell.defaults.component.stateEngine;
    const stateEngineModule = xshell.config.modules[context.resourceDefinition.moduleId].defaults?.component?.stateEngine;
    const stateEngineComponent = definition.meta.stateEngine || stateEngineModule || stateEngineXShell;
    const stateEngineFactoryCreator = await xshell.loader.load("state-engine:" + stateEngineComponent);
    const stateEngineFactory = new stateEngineFactoryCreator(stateSkeleton, context);
    // render engine
    const renderEngineXShell = xshell.config.xshell.defaults.component.renderEngine;
    const renderEngineModule = xshell.config.modules[context.resourceDefinition.moduleId].defaults?.component?.renderEngine;
    const renderEngineComponent = definition.meta.renderEngine || renderEngineModule || renderEngineXShell;
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
        _properties = null;
        _stateChanges = [];
        _disposables = [];
        _reflectingAttributes = new Set();
        // static
        static get observedAttributes() { 
            return propertyAttributeNames;
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
            this._properties = {};
            for (const [propName, property] of Object.entries(properties)) {
                if (property.state !== true) {
                    this._properties[propName] = property.default;
                }
            }
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
            if (stateMapAttributes.length) {
                const mutationObserver = new MutationObserver((mutationsList) => {
                    for (let mutation of mutationsList) {
                        if (mutation.type === "attributes") {
                            const attrName = mutation.attributeName;
                            const stateMapAttribute = stateMapAttributes.find(item => attrName.startsWith(item.attributePrefix));
                            if (stateMapAttribute) {
                                const subPropName = kebabToCamel(attrName.substring(stateMapAttribute.attributePrefix.length));
                                const attrValue = this.getAttribute(attrName);
                                const propValue = this._state[stateMapAttribute.stateName] || {};
                                propValue[subPropName] = attrValue;
                                this._state[stateMapAttribute.stateName] = propValue;
                            }
                        }
                    }
                });
                mutationObserver.observe(this, { attributes: true });
                // init state from attributes
                for (const stateMapAttribute of stateMapAttributes) {
                    for(let attr of this.attributes) {
                        if (attr.name.startsWith(stateMapAttribute.attributePrefix)) {
                            const subPropName = kebabToCamel(attr.name.substring(stateMapAttribute.attributePrefix.length));
                            const propValue = this._state[stateMapAttribute.stateName] || {};
                            propValue[subPropName] = attr.value;
                            this._state[stateMapAttribute.stateName] = propValue;
                        }
                    }
                }
            }
            // load
            this.onCommand("load", {});
        }
        // attributeChangedCallback
        attributeChangedCallback(name, oldValue, newValue) {
            if (this._reflectingAttributes.has(name)) return;
            const propName = kebabToCamel(name);
            const property = properties[propName];
            if (!property || property.attr !== true) return;
            this[propName] = convertAttributeValue(property, newValue);
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
            if (reflectedPropertyNames.includes(prop)) {
                this.reflectPropertyToAttribute(prop, newValue);
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
        // reflectPropertyToAttribute
        reflectPropertyToAttribute(propName, value) {
            const attrName = camelToKebab(propName);
            this._reflectingAttributes.add(attrName);
            try {
                if (value === false || value === null) {
                    this.removeAttribute(attrName);
                } else {
                    this.setAttribute(attrName, value === true ? "" : value);
                }
            } finally {
                this._reflectingAttributes.delete(attrName);
            }
        }
    };
    // add properties
    for (const [propName, property] of Object.entries(properties)) {
        Object.defineProperty(WebComponent.prototype, propName, {
            get() {
                return property.state === true ? this._state[propName] : this._properties[propName];
            },
            set(newValue) {
                if (property.state === true) {
                    this._state[propName] = newValue;
                } else {
                    const oldValue = this._properties[propName];
                    if (oldValue !== newValue) {
                        this._properties[propName] = newValue;
                        if (property.reflect === true) {
                            this.reflectPropertyToAttribute(propName, newValue);
                        }
                    }
                }
            },
            enumerable: true,
            configurable: false
        });
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
        let contract = module.contract;
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
        return await createComponentClassFromJsDefinition(src, context, definition, contract);        
    }
};
