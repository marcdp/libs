import Timer from "../timer.js"
import Events from "../events.js"
import xshell from "../xshell.js";
import validateComponentContract from "../validation/component.js";

// utils
function kebabToCamel(str) {
    // convert kebab-case to camelCase
    return str.split('-').map((word, index) => index === 0 ? word : word.charAt(0).toUpperCase() + word.slice(1)).join('');
};
function camelToKebab(str) {
    // convert camelCase to kebab-case
    return str.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();                      
}
function isEmptyPlainObject(value) {
    // check if the value is an empty plain object
    return value && typeof(value) === "object" && !Array.isArray(value) && Object.getPrototypeOf(value) === Object.prototype && Object.keys(value).length === 0;
}
function areDeclarativeValuesEqual(left, right) {
    // compare declarative values by structure and value
    if (left === right) {
        return true;
    }
    if (left === null || right === null || typeof(left) !== "object" || typeof(right) !== "object") {
        return false;
    }
    if (Array.isArray(left) || Array.isArray(right)) {
        if (!Array.isArray(left) || !Array.isArray(right) || left.length !== right.length) {
            return false;
        }
        return left.every((value, index) => areDeclarativeValuesEqual(value, right[index]));
    }
    if (!isPlainObject(left) || !isPlainObject(right)) {
        return false;
    }
    const leftKeys = Object.keys(left);
    const rightKeys = Object.keys(right);
    if (leftKeys.length !== rightKeys.length) {
        return false;
    }
    return leftKeys.every(key => Object.prototype.hasOwnProperty.call(right, key) && areDeclarativeValuesEqual(left[key], right[key]));
}
function createStateSkeleton(src, definition, contract) {
    // build state with contract defaults as the canonical values for public properties
    const stateSkeleton = {};
    const properties = contract.properties || {};
    const state = definition.state || {};
    const name = definition.meta?.name || src;
    for (const [propName, property] of Object.entries(properties)) {
        if (property.state === true) {
            stateSkeleton[propName] = property.default;
        }
    }
    for (const [stateName, value] of Object.entries(state)) {
        const property = properties[stateName];
        if (!property) {
            stateSkeleton[stateName] = value;
            continue;
        }
        if (property.state !== true) {
            throw new Error(`Component '${name}' declares public property '${stateName}' in definition.state, but the contract property is not state-backed.`);
        }
        if (!areDeclarativeValuesEqual(property.default, value)) {
            throw new Error(`Component '${name}' declares different defaults for public property '${stateName}' in contract.properties and definition.state.`);
        }
    }
    return stateSkeleton;
}
function isPlainObject(value) {
    // check that a value is a JSON-style plain object
    const prototype = Object.getPrototypeOf(value);
    return prototype === Object.prototype || prototype === null;
}
function convertAttributeValue(property, value) {
    // convert attribute value based on property type
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
    // Find the closest ancestor X-PAGE element, if any.
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
function validateSlots(definition, contract) {
    const template = document.createElement("template");
    template.innerHTML = definition.template;

    const slots = contract.slots || {};
    const componentName = definition.meta?.name || "unknown";

    for (const slot of template.content.querySelectorAll("slot")) {
        const slotName = slot.getAttribute("name") || "";

        if (!Object.prototype.hasOwnProperty.call(slots, slotName)) {
            const displayName = slotName || "(default)";
            debugger
            throw new Error(`Component '${componentName}' template declares slot '${displayName}', but it is not declared in contract.slots.`);
        }
    }
}

// create page class from js definition
export async function createComponentClassFromJsDefinition(src, context, definition, contract) {
    // defaults
    if (!contract) contract = {};
    if (!contract.properties) contract.properties = {};
    if (!contract.events) contract.events = {};
    if (!contract.slots) contract.slots = {};
    if (!contract.methods) contract.methods = {};
    if (!definition.state) definition.state = {};
    if (!definition.style) definition.style = "";
    if (!definition.template) definition.template = "";
    if (!definition.controller) definition.controller = () => ({});
    definition = Object.seal(Object.freeze(definition));
    contract = Object.seal(Object.freeze(contract));
    // validate contract
    if (contract) {
        await validateComponentContract(src, contract);
    }
    // validate slots
    if (contract) {
        validateSlots(definition, contract);
    }
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
    const stateSkeleton = createStateSkeleton(src, definition, contract);
    const propertyAttributeNames = [];
    const reflectedPropertyNames = [];
    const stateMapAttributes = [];
    for (const [propName, property] of Object.entries(contract.properties)) {
        if (property.attribute === true) {
            propertyAttributeNames.push(camelToKebab(propName));
        }
        if (property.reflect === true) {
            reflectedPropertyNames.push(propName);
        }
        if (property.state === true && property.attribute === true && isEmptyPlainObject(property.default)) {
            stateMapAttributes.push({ attributePrefix: camelToKebab(propName) + "-", stateName: propName });
        }
    }
    for (const [stateName, value] of Object.entries(definition.state)) {
        if (Object.prototype.hasOwnProperty.call(contract.properties, stateName)) {
            continue;
        }
        if (isEmptyPlainObject(value)) {
            stateMapAttributes.push({ attributePrefix: camelToKebab(stateName) + "-", stateName });
        }
    }
    // state engine
    const stateEngineModule = xshell.config.modules[context.resourceDefinition.moduleId].defaults.component.stateEngine;
    const stateEngineComponent = definition.meta.stateEngine || stateEngineModule;
    const stateEngineFactoryCreator = await xshell.loader.load("state-engine:" + stateEngineComponent);
    const stateEngineFactory = new stateEngineFactoryCreator(stateSkeleton, context);
    // render engine
    const renderEngineModule = xshell.config.modules[context.resourceDefinition.moduleId].defaults.component.renderEngine;
    const renderEngineComponent = definition.meta.renderEngine || renderEngineModule;
    const renderEngineFactoryCreator = await xshell.loader.load("render-engine:" + renderEngineComponent);
    const renderEngineFactory = new renderEngineFactoryCreator(definition.template, context, definition.templateRenderer);
    // render engine dependencies
    if (renderEngineFactory.dependencies.length) {
        await xshell.loader.load(renderEngineFactory.dependencies);
    }    
    // init 
    renderEngineFactory.init();
    // controller dispatcher
    const invokeController = Symbol("invokeController");
    // returns a class that extends base class component
    const WebComponent = class extends HTMLElement {
        // vars
        _state = null;
        _properties = null;
        _stateChanges = [];
        _disposables = [];
        _reflectingAttributes = new Set();
        _controller = null;
        _renderEngine = null;
        _renderPending = false;
        _unloaded = false;
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
            for (const [propName, property] of Object.entries(contract.properties)) {
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
                    } else if (prop == "contract") {
                        // contract of component
                        return contract;
                    } else if (prop == "state") {
                        // state
                        return self._state;
                    } else if (prop == "timer") {
                        // timer helper
                        const timer = new Timer((command, ...params) => { self[invokeController](command, ...params); });
                        self._disposables.push(timer);
                        return timer;
                    } else if (prop == "events") {
                        // events helper
                        const events = new Events((command, ...params) => { self[invokeController](command, ...params); });
                        self._disposables.push(events);
                        return events;
                    } else if (prop == "moduleConfig") {
                        // module configuration
                        return xshell.config.modules[context.resourceDefinition.moduleId];
                    } else if (prop == "module") {
                        // module 
                        return xshell.modules.getModuleById(context.resourceDefinition.moduleId);
                    } else if (prop == "host") {
                        // get current web component
                        return self;
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
            // author script
            this._controller = definition.controller(servicesProvider) ?? {};
            // validate public contract methods
            for (const methodName of Object.keys(contract.methods ?? {})) {
                const method = this._controller[methodName];
                if (typeof(method) !== "function") {
                    throw new Error(`Component '${definition.meta.name}' declares public method '${methodName}' in contract.methods but controller.${methodName} is not a function.`);
                }
            }
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
            this[invokeController]("load", {});
        }        
        // get host element
        get host() {
            return this._host;
        }
        // attributeChangedCallback
        attributeChangedCallback(name, oldValue, newValue) {
            if (this._reflectingAttributes.has(name)) return;
            const propName = kebabToCamel(name);
            const property = contract.properties[propName];
            if (!property || property.attribute !== true) return;
            this[propName] = convertAttributeValue(property, newValue);
        }
        // connected/disconnected
        connectedCallback() {
            if (this._unloaded) return;
            this._renderEngine = renderEngineFactory.create({ host: this.shadowRoot, state: this._state, handler:(command, ...params) => {
                this[invokeController](command, ...params);
            }, invalidate: () => { 
                this.invalidate(); 
            } });
            this._renderEngine.mount();
            this[invokeController]("mount", {});
            this.invalidate();
        }
        disconnectedCallback() {
            if (this._unloaded) return;
            this[invokeController]("unmount", {});
            if (this._renderEngine) {
                this._renderEngine.unmount();
                this._renderEngine = null;
            }
            this._renderPending = false;
        }
        // unload
        async unload() {
            if (this._unloaded) return;
            this._unloaded = true;
            let result;
            try {
                result = await this[invokeController]("unload", {});
            } finally {
                if (this._renderEngine) {
                    this._renderEngine.unmount();
                    this._renderEngine = null;
                }
                this._renderPending = false;
                for (const disposable of this._disposables) {
                    disposable.dispose();
                }
                this._disposables = [];
            }
            return result;
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
            const renderEngine = this._renderEngine;
            if (!renderEngine) return;
            if (this._renderPending) return;
            this._renderPending = true;
            requestAnimationFrame(() => {
                if (this._renderEngine !== renderEngine) return;
                this[invokeController]("stateChange", {changes: this._stateChanges});
                this._stateChanges = [];
                this._renderPending = false;
                renderEngine.render();
            });
        }
        // invoke controller
        [invokeController](command, ...params) {
            const handler = this._controller[command];
            if (typeof(handler) === "function") {
                return handler.apply(this._controller, params);
            }
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
    for (const [propName, property] of Object.entries(contract.properties)) {
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
    // add methods
    for (const methodName of Object.keys(contract.methods)) {
        if (methodName in WebComponent.prototype) {
            throw new Error(`Component '${definition.meta.name}' cannot expose public method '${methodName}' because it would overwrite a framework or Web Component method.`);
        }
        Object.defineProperty(WebComponent.prototype, methodName, {
            value: function(...args) {
                return this[invokeController](methodName, ...args);
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
        // create class definition
        return await createComponentClassFromJsDefinition(src, context, definition, contract);        
    }
};
