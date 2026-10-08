import Timer from "../timer.js"
import Events from "../events.js"
import xshell from "../xshell.js";
import validateComponentContract from "../validation/component.contract.js";
import validateComponent from "../validation/component.js";
import { processStyle } from "../utils/style.js";
import { deepFreeze } from "../utils/object.js";

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
function createStateSkeleton(src, implementation, contract) {
    // build state with contract defaults as the canonical values for public properties
    const stateSkeleton = {};
    const properties = contract.properties || {};
    const state = implementation.state || {};
    const name = implementation.meta?.name || src;
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
            throw new Error(`Component '${name}' declares public property '${stateName}' in implementation.state, but the contract property is not state-backed.`);
        }
        if (!areDeclarativeValuesEqual(property.default, value)) {
            throw new Error(`Component '${name}' declares different defaults for public property '${stateName}' in contract.properties and implementation.state.`);
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
            if (value == "false") return false;
            if (value == "0") return false;
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
function validateSlots(slots, contract, componentId) {
    const contractSlots = contract.slots || {};
    for (const slotName of slots) {
        if (!Object.prototype.hasOwnProperty.call(contractSlots, slotName)) {
            const displayName = slotName || "(default)";
            throw new Error(`Component '${componentId}' template declares slot '${displayName}', but it is not declared in contract.slots.`);
        }
    }
}


// create web component from js implementation
export async function createComponentClassFromJsDefinition(src, context, implementation, contract) {
    // defaults
    if (!contract) contract = {};
    if (!contract.properties) contract.properties = {};
    if (!contract.events) contract.events = {};
    if (!contract.slots) contract.slots = {};
    if (!contract.methods) contract.methods = {};
    if (!implementation.meta) implementation.meta = {};
    if (!implementation.meta.renderEngine) implementation.meta.renderEngine = xshell.config.modules[context.resourceDefinition.moduleId].defaults.component.renderEngine;
    if (!implementation.meta.stateEngine) implementation.meta.stateEngine = xshell.config.modules[context.resourceDefinition.moduleId].defaults.component.stateEngine;
    if (!implementation.dependencies) implementation.dependencies = {};
    if (!implementation.state) implementation.state = {};
    if (!implementation.style) implementation.style = "";
    if (!implementation.template) implementation.template = "";
    if (!implementation.controller) implementation.controller = () => ({});
    // validate contract
    if (contract) {
        await validateComponentContract(src, contract);
    }
    // validateComponent implementation against contract
    if (implementation) {
        await validateComponent(src, implementation);
    }
    // freeze and seal the implementation and contract to prevent further modifications
    implementation = Object.seal(Object.freeze(implementation));
    contract = Object.seal(Object.freeze(contract));
    // process definition CSS once before creating Component instances
    const style = implementation.style ? await processStyle({ src, context, css: implementation.style }) : "";
    // stylesheets
    const stylesheets = []
    if (style) {
        const stylesheet = new CSSStyleSheet();
        stylesheet.replaceSync(style);
        stylesheets.push(stylesheet);
    }
    // state skeleton
    const stateSkeleton = createStateSkeleton(src, implementation, contract);
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
    for (const [stateName, value] of Object.entries(implementation.state)) {
        if (Object.prototype.hasOwnProperty.call(contract.properties, stateName)) {
            continue;
        }
        if (isEmptyPlainObject(value)) {
            stateMapAttributes.push({ attributePrefix: camelToKebab(stateName) + "-", stateName });
        }
    }
    // state engine
    const stateEngineFactoryCreator = await xshell.loader.load("state-engine:" + implementation.meta.stateEngine);
    const stateEngineFactory = new stateEngineFactoryCreator(stateSkeleton, context);
    // render engine
    const renderEngineFactoryCreator = await xshell.loader.load("render-engine:" + implementation.meta.renderEngine);
    const renderEngineFactory = new renderEngineFactoryCreator(implementation.template, context, implementation.templateRenderer);
    // validate compiled template slots through the render-engine factory contract
    validateSlots(renderEngineFactory.slots, contract, implementation.meta.id || "unknown");
    // load render engine dependencies
    if (renderEngineFactory.dependencies.length) {
        await xshell.loader.load(renderEngineFactory.dependencies);
    }    
    // load component dependencies
    let dependencies = {};
    if (implementation.dependencies && Object.keys(implementation.dependencies).length) {
        dependencies = await xshell.loader.load(implementation.dependencies);
    }
    // init 
    await renderEngineFactory.init();
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
        _mutationObserver = null;
        _renderPending = false;
        _renderCount = 0;
        _commandsPending = [];
        _unloaded = false;
        // static
        static get observedAttributes() { 
            return propertyAttributeNames;
        }
        static get isXShellComponent() {
            return true;
        }
        static get contract() { 
            return contract;
        }
        // ctor
        constructor() {
            super();
            const self = this;
            //shadowRoot
            this.attachShadow(implementation.meta.shadowRootOptions || {mode: "open"});
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
                    if (prop == "implementation") {
                        // implementation of component
                        return implementation;
                    } else if (prop == "contract") {
                        // contract of component
                        return contract;
                    } else if (prop == "state") {
                        // state
                        return self._state;
                    } else if (prop == "timer") {
                        // timer helper
                        const timer = new Timer((command, ...params) => { 
                            self._controller[command](...params); 
                        });
                        self._disposables.push(timer);
                        return timer;
                    } else if (prop == "events") {
                        // events helper
                        const events = new Events((command, ...params) => { 
                            self._controller[command](...params); 
                        });
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
                    } else if (prop == "dependencies") {
                        // get dependencies
                        return dependencies;
                    } else if (prop == "commands") {
                        // get commands
                        return {
                            enqueue: (command, ...params) => { self._commandsPending.push( { command, params: [...params] }); }
                        };
                    } else {
                        // resolve from services
                        return xshell.services.resolve(prop);
                    }                    
                }
            });
            // controller
            this._controller = implementation.controller(servicesProvider) ?? {};
            // validate public contract methods
            for (const methodName of Object.keys(contract.methods ?? {})) {
                const method = this._controller[methodName];
                if (typeof(method) !== "function") {
                    throw new Error(`Component '${implementation.meta.id}' declares public method '${methodName}' in contract.methods but controller.${methodName} is not a function.`);
                }
            }
            // attribute mutation observer (listen for changes in attributes that start with state map attribute names)
            if (stateMapAttributes.length) {
                this._mutationObserver = new MutationObserver((mutationsList) => {
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
                this._mutationObserver.observe(this, { attributes: true });
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
            const property = contract.properties[propName];
            if (!property || property.attribute !== true) return;
            this[propName] = convertAttributeValue(property, newValue);
        }
        // connected/disconnected
        connectedCallback() {
            if (this._unloaded) return;
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
            if (this._unloaded) return;
            this._unmount();
        }
        // unload
        async unload() {
            if (this._unloaded) return;
            this._unloaded = true;
            let result;
            try {
                try {
                    this._unmount();
                } finally {
                    result = await this.onCommand("unload", {});
                }
            } finally {
                this._mutationObserver?.disconnect();
                this._mutationObserver = null;
                for (const disposable of this._disposables) {
                    disposable.dispose();
                }
                this._disposables = [];
            }
            return result;
        }
        // methods (private)
        _unmount() {
            const renderEngine = this._renderEngine;
            if (!renderEngine) return;
            this._renderEngine = null;
            this._renderPending = false;
            try {
                this.onCommand("unmount", {});
            } finally {
                renderEngine.unmount();
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
            const renderEngine = this._renderEngine;
            if (!renderEngine) return;
            if (this._renderPending) return;
            this._renderPending = true;
            requestAnimationFrame(() => {
                if (this._renderEngine !== renderEngine) return;
                this.onCommand("stateChange", {changes: this._stateChanges});
                this._stateChanges = [];
                this._renderPending = false;
                renderEngine.render();
                while (this._commandsPending.length > 0) {
                    const cmd = this._commandsPending.shift();
                    this.onCommand(cmd.command, ...cmd.params);
                }
            });
        }
        // invoke controller
        onCommand(command, ...params) {
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
            throw new Error(`Component '${implementation.meta.id}' cannot expose public method '${methodName}' because it would overwrite a framework or Web Component method.`);
        }
        Object.defineProperty(WebComponent.prototype, methodName, {
            value: function(...args) {
                return this.onCommand(methodName, ...args);
            },
            enumerable: true,
            configurable: false
        });
    }
    // register
    const id = implementation.meta.id;
    if (window.customElements.get(id)) {
        throw new Error(`Component '${id}' from '${src}' cannot be registered because that id is already registered.`);
    }
    window.customElements.define(id, WebComponent);
    // return class
    return WebComponent
}

//export 
export default class LoaderComponentJs {
    async load(src, context) {
        // import
        const module = await import(src);
        let implementation = module.default;
        let contract = module.contract;
        // check if its a promise
        if (typeof(implementation) === "object" && typeof(implementation.then) === "function") {
            implementation = await implementation;
        }
        // check if its a class
        if (typeof(implementation) === "function"  && /^class\s/.test(Function.prototype.toString.call(implementation))) {
            return implementation;
        }
        // else, asume its a implementation object
        if (!implementation.meta) implementation.meta = {};
        if (!implementation.meta.id) {
            let aux = src.split("?")[0];
            aux = aux.substring(aux.lastIndexOf("/")+1).split(".")[0];
            implementation.meta.id = aux;
        }
        // create class implementation
        return await createComponentClassFromJsDefinition(src, context, implementation, contract);        
    }
};
