import { generateId } from "./utils/ids.js";
import xshell from "xshell";


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

    _host = null;
    _refs = null;

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

    // lifecycle mehods
    async load() {
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
        // mount
        this._host = host;
        await this.onCommand("mount", {});
    }
    async onCommand(command, params = {}) {
        // on command
    }
    async unmount() {
        // unmount
        this._host = null;
        await this.onCommand("unmount", {});
    }
    async unload() {
        // unload
        await this.onCommand("unload", {});
        this._refs = null;
    }

    // close methods
    close(result) {
        // close
        return this._host.close(result);
    }


}
