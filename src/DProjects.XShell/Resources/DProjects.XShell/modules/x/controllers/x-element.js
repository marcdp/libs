import XTemplate, {utils} from "./x-template.js";
import createState from "./create-state.js";
import xshell, { Binds }  from "xshell";


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


// class
class XElement extends HTMLElement {

    //fields
    _xtemplateInstance = null;
    _stateUnproxied = null;
    _state = null;
    _binds = new Binds();

    //work fields
    _connected = false;
    _loaded = false;
    _loadCalled = false;
    _renderTimeoutId = 0;
    _renderCount = 0;
    
    //ctor
    constructor() {
        super();
        //shadowRoot
        let container = (this.settings.useLightDom ? this : this.attachShadow(this.settings.shadowRootOptions));
        //state
        this.state = this.constructor.prototype.initState();
        //instance
        let self = this;
        this._xtemplateInstance = this.constructor.prototype.xtemplate.createInstance(
            (command, event) => {
                //handler
                self.onCommand(command, {event});
            }, 
            () => {
                //invalidate
                self.invalidate();
            }, 
            container
        );
        //init
        this.onCommand("init", {});
    }

    //props
    get page() {
        return xshell.navigation.getXPageFromElement(this);
    }
    get state() {return this._state;}
    set state(value) {
        this._stateUnproxied = value;
        this._state = createState(value, this);
        this.invalidate();
    }    
    get refs() {
        if (!this._refs) {
            this._refs = new Proxy(this.shadowRoot, {
                get: (target, prop) => {
                  return target.querySelector(`[ref="${prop}"]`);
                }
            });
        }
        return this._refs;
    }


    //events
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
    connectedCallback() {
        this._connected = true;
        if (!this._loadCalled) {
            this._loaded = true;
            if (this._propertiesToInitialize) {
                for(let def of this._propertiesToInitialize) {
                    this[def.name] = def.value;
                }
                delete this._propertiesToInitialize;
            }
            this.onCommand("load", {});
        }
        this.render();
    }
    disconnectedCallback() {
        this._connected = false;
        this.onCommand("unload", {});
        this._binds.clear();
    }


    //methods
    onCommand(command, args) {
        //command
    }
    bindEvent(target, event, command) {
        this._binds.bindEvent(target, event, (event)=> {
            if (typeof(command) == "string") {
                this.onCommand(command, {event});
            } else {
                command(event);
            }
        });
    }    
    
    bindTimeout(timeout, command) {
        this._binds.bindTimeout(timeout, (event)=> {
            if (typeof(command) == "string") {
                this.onCommand(command, {event});
            } else {
                command(event);
            }
        });
    }
    bindInterval(timeout, command) {
        this._binds.bindInterval(timeout, (event)=> {
            if (typeof(command) == "string") {
                this.onCommand(command, {event});
            } else {
                command(event);
            }
        });
    }
    stateChange(prop, oldValue, newValue) {
        //a state prop changed
        let attr = camelToKebab(prop);
        if (this.settings.backPropagatedAttributes.indexOf(attr) != -1) {
            if (typeof(newValue) == "boolean") {
                if (newValue) {
                    if (!this.hasAttribute(attr)) this.setAttribute(attr, "");
                } else {
                    if (this.hasAttribute(attr)) this.removeAttribute(attr);
                }
            } else {
                let attrOldValue = this.getAttribute(attr);
                let attrNewValue = newValue ? newValue.toString() : "";
                    if (attrOldValue != attrNewValue) {
                    this.setAttribute(attr, attrNewValue);
                }
            }
        }
    }    
    invalidate() {
        //enqueue a render
        if (this._connected) {
            if (!this._renderTimeoutId) {
                this._renderTimeoutId = window.requestAnimationFrame(() => {
                    this.render();
                });
            }
        }
    }
    preRender() {
        //pre render
    }
    render() {
        //cancel pending render
        if (this._renderTimeoutId) {
            window.cancelAnimationFrame(this._renderTimeoutId);
            this._renderTimeoutId = 0;
        };
        //render
        if (!this.preRender()) {
            this._xtemplateInstance.render(this._state);
            this._renderCount++;
        }
        this.postRender();
    }
    postRender(){
        //post render
    }


    //static
    static async define(name, definition) {
        //settings
        if (!definition.settings) definition.settings = {};
        //compute default observedAttributes from state
        if (!definition.settings.observedAttributes) {
            var observedAttributes = [];
            for(let propName in definition.state) {
                observedAttributes.push(camelToKebab(propName));
            }                
            definition.settings.observedAttributes = observedAttributes;
        }
        //default settings
        let settings = Object.freeze(Object.assign({
            shadowRootOptions: { mode: "open"},
            observedAttributes: [],
            backPropagatedAttributes: [],
            autoGeneratedProperties: true,
            preload: [],
            useLightDom: false
        }, definition.settings));
        //create web component class
        let result = class extends XElement {
            //satatic
            static definition = definition;
            static get observedAttributes() { 
                return settings.observedAttributes || []; 
            }
            //props
            get settings() { return settings ;}
        };
        //url rewriting
        //if (definition.template && (definition.template.indexOf("=\"/") != -1)) {
            //Utils.rewriteTemplateUrls(definition.template, xshell.settings.baseUrl);
        //}
        //xtemplate
        result.prototype.xtemplate = new XTemplate({ 
            template: definition.template || "", 
            styleSheets: definition.styleSheets || definition.style || []
        });
        //style global
        if (definition.styleGlobal){
            let style = document.createElement("style");
            style.innerHTML = definition.styleGlobal;
            document.head.appendChild(style);
        }
        //load dependencies
        let names = [];
        result.prototype.xtemplate.dependencies.forEach((name) => {
            names.push("component:" + name);
        });
        if (settings.preload && settings.preload.length) {
            names.push(...settings.preload);
        }
        await xshell.loader.load(names);
        //state
        let stateAsJson = JSON.stringify({});
        if (!definition.state) definition.state = {};
        stateAsJson = JSON.stringify(definition.state);
        result.prototype.initState = () => {
            return JSON.parse(stateAsJson);
        };
        //methods
        if (definition.methods){
            for(let key in definition.methods) {
                let value = definition.methods[key];
                result.prototype[key] = value;
            }
        }
        //properties
        let state = definition.state;
        if (settings.autoGeneratedProperties) {
            var propNames = [];
            if (Array.isArray(settings.autoGeneratedProperties)) {
                for(let propName of settings.autoGeneratedProperties) {
                    propNames.push(propName);
                }
            } else {
                for(let propName in state) {
                    propNames.push(propName);
                }
            }
            for (let propName of propNames) {
                if (propName == "state") continue;
                Object.defineProperty(result.prototype, propName, {
                    get() {
                        return this.state[propName];
                    },
                    set(value) {
                        this.state[propName] = value;
                    }
                });
            }
        }
        //register
        customElements.define(name, result);
        //return
        return result;
    }
}

//export
export default XElement;

export {utils};