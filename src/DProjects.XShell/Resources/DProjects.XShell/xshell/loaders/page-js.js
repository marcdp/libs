import Page from "../page.js"
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
            throw new Error(`Page '${name}' declares public property '${stateName}' in definition.state, but the contract property is not state-backed.`);
        }
        if (!areDeclarativeValuesEqual(property.default, value)) {
            throw new Error(`Page '${name}' declares different defaults for public property '${stateName}' in contract.properties and definition.state.`);
        }
    }
    return stateSkeleton;
}
function isPlainObject(value) {
    // check that a value is a JSON-style plain object
    const prototype = Object.getPrototypeOf(value);
    return prototype === Object.prototype || prototype === null;
}
function escapeCssString(value) {
    return value
        .replace(/\\/g, "\\\\")
        .replace(/"/g, '\\"')
        .replace(/\n/g, "\\A ");
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


// create page class from js definition
export async function createPageClassFromJsDefinition(src, context, definition, contract) {
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
   
    // state skeleton
    const stateSkeleton = createStateSkeleton(src, definition, contract);
    const propertyAttributeNames = [];
    const reflectedPropertyNames = [];
    const stateMapAttributes = [];
    const stateQsNames = [];
    const stateReflectedQsNames = [];
    let stateContextNames = []; // what we should do with context variables? now they are not implemented
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
        stateQsNames.push(propName);
        if (property.reflect) stateReflectedQsNames.push(propName);
    }
    for (const [stateName, value] of Object.entries(definition.state)) {
        if (Object.prototype.hasOwnProperty.call(contract.properties, stateName)) {
            continue;
        }
        if (isEmptyPlainObject(value)) {
            stateMapAttributes.push({ attributePrefix: camelToKebab(stateName) + "-", stateName });
        }
    }
    // modules
    const moduleConfig = xshell.config.modules[context.resourceDefinition.moduleId];
    // state engine
    const stateEngineModule = moduleConfig.defaults.page.stateEngine;
    const stateEnginePage = definition.meta?.stateEngine || stateEngineModule;
    const stateEngineFactoryCreator = await xshell.loader.load("state-engine:" + stateEnginePage);
    const stateEngineFactory = new stateEngineFactoryCreator(stateSkeleton, context);
    // render engine
    const renderEngineModule = moduleConfig.defaults.page.renderEngine;
    const renderEnginePage = definition.meta?.renderEngine || renderEngineModule;
    const renderEngineFactoryCreator = await xshell.loader.load("render-engine:" + renderEnginePage);
    const templateRenderer = definition.templateRenderer; 
    const renderEngineFactory = new renderEngineFactoryCreator(definition.template, context, templateRenderer);
    // render engine dependencies
    if (renderEngineFactory.dependencies.length) {
        await xshell.loader.load(renderEngineFactory.dependencies);
    }    
    // init 
    renderEngineFactory.init();
    // returns a class that extends base class Page
    const PageClass = class extends Page {
        // vars
        _state = null;
        _stateChanges = [];
        _renderEngine = null;
        _renderPending = false;
        _styleSheets = [];
        _disposables = [];
        // ctor
        constructor({ src, context }) {
            super({ src, context });
            const self = this;
            // meta
            this._label = definition.meta.title || "";
            this._description = definition.meta.description || "";
            this._icon = definition.meta.icon || "";
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
            // stateQsNames
            if (stateQsNames.length) {
                var qs = new URLSearchParams(src.split("?")[1] || "");
                for(let propName of stateQsNames) {
                    const propNameKebabCase = camelToKebab(propName);
                    if (qs.has(propNameKebabCase)) {
                        let value = qs.get(propNameKebabCase);
                        let oldValue = self._state[propName];
                        const propDefinition = definition.state[propName];
                        if (propDefinition.type == "boolean" || typeof(oldValue) == "boolean") {
                            self._state[propName] = (value == "true" || value == "1");
                        } else if (propDefinition.type == "number" || typeof(oldValue) == "number") {
                            if (value !== "" && isNaN(value) == false){
                                self._state[propName] = Number(value);
                            }
                        } else {
                            self._state[propName] = value;
                        }
                    }
                }
            }
            // stateContextNames
            if (stateContextNames.length) {
                for(let propName of stateContextNames) {
                    if (typeof(this._context[propName]) != "undefined") {
                        let value = this._context[propName];
                        self._state[propName] = value;
                    }
                }
            }
            // services provider
            const servicesProvider = new Proxy({}, {
                get: (obj, prop) => {
                    if (prop == "definition") {
                        // page definition
                        return definition;
                    } else if (prop == "state") {
                        // state
                        return self._state;
                    } else if (prop == "context") {
                        // context
                        return self._context;
                    } else if (prop == "timer") {
                        // timer
                        const timer = new Timer((command, ...params) => { self._controller?.[command]?.(...params); });
                        self._disposables.push(timer);
                        return timer;
                    } else if (prop == "events") {
                        // events
                        const events = new Events((command, ...params) => { self._controller?.[command]?.(...params); });
                        self._disposables.push(events);
                        return events;
                    } else if (prop == "page") {
                        // get current page
                        return self;                    
                    } else {
                        // resolve from services
                        return xshell.services.resolve(prop);                    
                    }
                }
            });            
            // author script
            this._controller = definition.controller?.(servicesProvider) ?? {};
            // validate public contract methods
            for (const methodName of Object.keys(contract.methods ?? {})) {
                const method = this._controller[methodName];
                if (typeof(method) !== "function") {
                    throw new Error(`Page '${definition.meta.name}' declares public method '${methodName}' in contract.methods but controller.${methodName} is not a function.`);
                }
            }
            
        }
        // mount/unmount
        async mount({ host }) {
            if (this._unloaded) return;
            // style
            const cssPageSelector = `${host.nodeName.toLowerCase()}[src="${escapeCssString(host.getAttribute("src"))}"]`;
            if (typeof(definition.style) == "string" && definition.style) {
                const cssStyleSheet = new CSSStyleSheet();
                cssStyleSheet.replaceSync(`@scope (${cssPageSelector}) {${definition.style}}`);
                this._styleSheets.push(cssStyleSheet);        
            } else if (Array.isArray(definition.style) && definition.style.length) {
                for(let styleText of definition.style) {
                    const cssStyleSheet = new CSSStyleSheet();
                    cssStyleSheet.replaceSync(`@scope (${cssPageSelector}) {${styleText}}`);
                    this._styleSheets.push(cssStyleSheet);
                }
            }    
            document.adoptedStyleSheets = [...document.adoptedStyleSheets,...this._styleSheets];
            // render engine
            this._renderEngine = renderEngineFactory.create({ host, state: this._state, handler:(command, ...params) => {
                this._controller?.[command]?.(...params);
            }, invalidate: () => { 
                this.invalidate(); 
            } })
            this._renderEngine.mount();
            // mount
            await super.mount({ host });
            // invalidate
            this.invalidate();
        }
        async unmount() {
            if (this._unloaded) return;
            // detach this mount's resources immediately
            const renderEngine = this._renderEngine;
            const styleSheets = this._styleSheets;
            // capture the current render engine and style sheets for cleanup after unmount
            this._renderEngine = null;
            this._styleSheets = [];
            this._renderPending = false;
            // controller may be async
            await super.unmount();
            // clean only resources captured from this mount
            if (renderEngine) {
                renderEngine.unmount();
            }
            if (styleSheets.length) {
                document.adoptedStyleSheets = document.adoptedStyleSheets.filter(stylesheet => !styleSheets.includes(stylesheet));
            }
        }
        async unload() {
            if (this._unloaded) return;
            try {
                await super.unload();
            } finally {
                if (this._renderEngine) {
                    this._renderEngine.unmount();
                    this._renderEngine = null;
                }
                this._renderPending = false;
                if (this._styleSheets.length) {
                    document.adoptedStyleSheets = document.adoptedStyleSheets.filter(stylesheet => !this._styleSheets.includes(stylesheet));
                    this._styleSheets = [];
                }
                for (const disposable of this._disposables) {
                    disposable.dispose();
                }
                this._disposables = [];
                this._stateChanges = [];
                this._controller = null;
            }
        }
        // statechange
        stateChange(prop, oldValue, newValue) {
            // state changed
            this._stateChanges.push({prop, oldValue, newValue});
            // reflect to property to qs if needed
            if (stateReflectedQsNames.includes(prop)) {
                const src = this.src;
                const qsName = camelToKebab(prop);
                const item = xshell.navigation.parseUrl(src);
                if (newValue === false || newValue === null) {
                    self.removeAttribute(attrName);
                    delete item.params[qsName];
                } else if (typeof(newValue) == "boolean" && newValue === true) {
                    item.params[qsName] = "true";
                } else if (typeof(newValue) == "number") {
                    item.params[qsName] = newValue.toString();
                } else {
                    item.params[qsName] = newValue;
                }
                const newUrl = xshell.navigation.buildUrl(item);
                // call navigate, with replace true to avoid creating a new history entry for each state change
                xshell.navigation.navigate({...item, page:this, replace:true});
            }
        }
        // onCommand
        onCommand(command, ...params) {
            const handler = this._controller[command];
            if (typeof(handler) === "function") {
                return handler.apply(this._controller, params);
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
            });
        }
    };
    // add methods
    for (const methodName of Object.keys(contract.methods)) {
        if (methodName in PageClass.prototype) {
            throw new Error(`Page '${definition.meta.name}' cannot expose public method '${methodName}' because it would overwrite a framework or Page method.`);
        }
        Object.defineProperty(PageClass.prototype, methodName, {
            value: function(...args) {
                return this.onCommand(methodName, ...args);
            },
            enumerable: true,
            configurable: false
        });
    }
    return PageClass;
}

//export 
export default class LoaderPageJs {
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
        return await createPageClassFromJsDefinition(src, context, definition, contract);        
    }
};
