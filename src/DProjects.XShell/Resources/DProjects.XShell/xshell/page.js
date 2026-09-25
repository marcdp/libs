import { generateId } from "./utils/ids.js";
import xshell from "./xshell.js";


// class
export default class Page {


    //vars
    _id = null;
    _src = null;
    _context = null;
    _label = null;
    _description = null;
    _breadcrumb = null;
    _icon = null;
    _result = null;
    _controller = null;

    _host = null;
    _refs = null;
    _loaded = false;
    _unloaded = false;

    //ctor
    constructor({ src, context }) {
        this._src = src;
        this._context = context;
        this._id = generateId("page");
    }


    //props
    get host() { return this._host; }

    get id() { return this._id; }
    set id(value) { this._id = value; }

    get src() { return this._src; }

    get label() { return this._label; }
    set label(value) { 
        this._label = value; 
        if (this._breadcrumb && this._breadcrumb.length > 0) this._breadcrumb[this._breadcrumb.length - 1].label = value;
    }

    get description() { return this._description; }
    set description(value) { 
        this._description = value; 
        if (this._breadcrumb && this._breadcrumb.length > 0) this._breadcrumb[this._breadcrumb.length - 1].description = value;
    }

    get breadcrumb() { return this._breadcrumb; }
    set breadcrumb(value) { this._breadcrumb = value; }

    get icon() { return this._icon; }
    set icon(value) { 
        this._icon = value; 
        if (this._breadcrumb && this._breadcrumb.length > 0) this._breadcrumb[this._breadcrumb.length - 1].icon = value;
    }

    get result() { return this._result; }
    set result(value) { this._result = value; }

    get refs() {
        if (!this._refs) {
            this._refs = new Proxy(this._host, {
                get: (target, prop) => {
                  return target.querySelector(`[ref="${prop}"]`);
                }
            });
        }
        return this._refs;
    }

    // lifecycle methods
    async load() {
        if (this._loaded || this._unloaded) return;
        this._loaded = true;
        // call load command
        const url = new URL(this._src, document.baseURI);
        const params = {
            query: Object.fromEntries(url.searchParams.entries()),
            path: url.pathname,
            hash: url.hash
        };
        await this.onCommand("load", params);
        // bus event
        xshell.bus.emit("xshell:page:load", { src: this._src, id: this._id });
    }
    async mount({ host, renderEngine }) {
        if (this._unloaded) return;
        // mount
        this._host = host;
        await this.onCommand("mount", {});
    }
    async onCommand(command, params = {}) {
        const handler = this._controller[command];
        if (typeof(handler) === "function") {
            return await handler.call(this._controller, params);
        }
    }
    async unmount() {
        if (this._unloaded) return;
        // unmount
        await this.onCommand("unmount", {});
        this._host = null;
    }
    async unload() {
        if (this._unloaded) return;
        this._unloaded = true;
        // unload
        await this.onCommand("unload", {});
        this._host = null;
        this._refs = null;
    }

    // close methods
    close(result) {
        // close
        return this._host.close(result);
    }


}
