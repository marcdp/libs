import xshell from "./xshell.js";
import Page from "./page.js";

// stylesheet
const stylesheet = new CSSStyleSheet();
stylesheet.replaceSync(`
    :host {display:block;}
`);


// class
class XPage extends HTMLElement {


    //static
    static get observedAttributes() { 
        return ["src", "layout", "module", "loading"]; 
    }


    //fields
    _connected = false;
    _src = "";
    _layout = "";
    _status = "";
    _context = {};

    _loading = "";
    _loadingObserver = null;
    _module = null;
    _page = null;
    

    //ctor
    constructor() {
        super();
        this.attachShadow({ mode: "open" });
        this.shadowRoot.adoptedStyleSheets = [stylesheet];
        this.addEventListener("query-close", () => {
            // query close
            this.queryClose();
        });
        this.addEventListener("command", (event) => {
            // handle command
            this.page?.onCommand(event.detail.command, event.detail.data, { event });
            event.preventDefault();
            event.stopPropagation();
            return false;
        });
        this.addEventListener("click", (event) => {
            const a = event.target.closest("a");
            if (!a || !this.contains(a) ||
                event.defaultPrevented ||
                event.button !== 0 ||        
                event.metaKey || event.ctrlKey || event.shiftKey || event.altKey ||
                a.target === "_blank" ||
                a.hasAttribute("download") ) {
                // normal browser navigation
                return; 
            }
            // handle navigation
            const xpage = a.closest("x-page");
            const href = a.getAttribute("href");
            const item = {
                ...xshell.navigation.parseUrl(href),
                open: "auto",
                page: xpage.page
            };
            xshell.navigation.navigate( item );
            event.preventDefault();
            event.stopPropagation();
            return false;
        }, true);
    }


    //props
    get src() {return this._src;}
    set src(value) {
        let changed = (this._src != value);
        if (!value.startsWith("/") && value.indexOf("://") == -1 && this.src) {
            let aux = this.src;
            aux = aux.substring(0, aux.lastIndexOf("/"));   
            value = aux + "/" + value;
        }
        this._src = value; 
        if (changed){
            if (this._src && this._connected) this.load();
        }
    }
    get status() {return this._status;}
    get page() {return this._page;}
    
    get breadcrumb() {return this._page.breadcrumb; }
    set breadcrumb(value) { this._page.breadcrumb = value; }

    get result() {return this._page?.result || null;}
    set result(value) {this._page.result = value;}

    get layout() { return this._layout;}
    set layout(value) { this._layout = value;}

    get module() { return this._module; }
    set module(value) { this._module = value; }

    get label() {return this._page?.label || "";}
    set label(value) {
        let changed = (this._page.label != value);
        this._page.label = value; 
        if (changed) {
            this.dispatchEvent(new CustomEvent("change"));
        }
    }

    get description() {return this._page?.description || "";}
    set description(value) {
        let changed = (this._page.description != value);
        this._page.description = value; 
        if (changed) {
            this.dispatchEvent(new CustomEvent("change"));
        }
    }

    get icon() {return this._page?.icon || "";}
    set icon(value) {
        let changed = (this._page.icon != value);
        this._page.icon = value; 
        if (changed) {
            this.dispatchEvent(new CustomEvent("change"));
        }
    }
    
    get context() {return this._context;}
    set context(value) { this._context = Object.freeze(value); }

    //events
    attributeChangedCallback(name, oldValue, newValue) {
        if (name == "src") this.src = newValue;
        if (name == "layout") this.layout = newValue;
        if (name == "module") this.module = newValue;
        if (name == "loading") this.loading = newValue;
    }
    connectedCallback() {
        this._connected = true;
        // load now, or on the first activation
        if (this.loading == "lazy") {
            // intersection observer            
            const onIntersection = (entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        if (this.loading == "lazy" && this._status == "") {
                            this.load();
                        }
                    }
                });
            };
            // set up the IntersectionObserver
            this._loadingObserver = new IntersectionObserver(onIntersection, {rootMargin: "10px"}); // start loading just before it comes into view
            this._loadingObserver.observe(this); // start observing the element

        } else if (this._src) {
            // load
            this.load();
        }
    }
    disconnectedCallback() {
        //destroy previous page
        this.unload();
        this._connected = false;
    }


    //methods    
    async load() {
        xshell.debug.log(`x-page: load '${this.src} ...`);
        let src = this.src;
        if (src.indexOf("error") != -1) {
            return;
        }
        // reset
        this._status = "loading";
        // set layout as loading (if exists)
        let layoutElement = this.shadowRoot.firstChild;
        if (layoutElement) {
            layoutElement.setAttribute("status", "loading");
        }
        // layout
        let layoutName = this._layout;
        if (!layoutName) layoutName = "embed";
        let layout = xshell.config.get("page.layout." + layoutName);
        let layoutClass = await xshell.loader.load("layout:" + layout);
        if (layoutElement == null || layoutElement.localName != layout) {
            if (layoutElement) layoutElement.remove();
            layoutElement = document.createElement(layout);
            layoutElement.innerHTML = "<slot></slot>";
            this.shadowRoot.appendChild(layoutElement);
            layoutElement.setAttribute("status", "loading");
        }        
        this._layout = layoutName;
        // load module 
        let moduleName = await xshell.modules.resolveModuleName(src);
        this.setAttribute("module", moduleName ?? "");
        // fetch page
        let page = null;
        try {
            const pageClass = await xshell.loader.load("page:" + src);
            page = new pageClass( { src, context: this._context } );
        } catch(exception) {
            page = new Page({ src });
            page.onCommand = (command, params) => {
                if (command == "mount") {
                    debugger;
                    this.showError(exception);
                }
            }
        }        
        //delay
        if (false && (src.indexOf("dialog")!=-1 || document.body.querySelectorAll(":scope > x-page").length > 1)) {
            await new Promise(r => setTimeout(r, 1000111));                
        }        
        // unmount previous page
        await this.unmount();
        // unload previous page
        await this.unload();
        // set new page
        this._page = page;
        // search params
        let searchParams = new URLSearchParams(src.indexOf("?") != -1 ? src.substring(src.indexOf("?") + 1).split("#")[0] : "");
        // nav
        let nav = null;
        if (searchParams.get("nav")) {
            nav = JSON.parse(atob(searchParams.get("nav").replace(/_/g, "/").replace(/-/g, "+")));
            nav.breadcrumb = nav.breadcrumb || [];
            nav.breadcrumb.push({ label: page.label, href: src });
            searchParams.delete("nav");
        }
        // breadcrumb
        let breadcrumb = xshell.menus.getMenuitemBreadcrumb(src.split("?")[0]);
        if (nav && nav.breadcrumb) {
            breadcrumb = nav.breadcrumb;
        }
        page.breadcrumb = breadcrumb;
        // title
        let label = page.label;
        if (nav && nav.title) {
            label = nav.title;
            if (breadcrumb && breadcrumb.length > 0) breadcrumb[breadcrumb.length - 1].label = label;
        }
        if (!label) {
            if (breadcrumb && breadcrumb.length > 0) label = breadcrumb[breadcrumb.length - 1].label;
        }
        if (this._layout == "dialog" && this._context && this._context.title){
            label = this._context.title;
        }
        page.label = label;
        // icon
        let icon = page.icon;
        if (!icon && breadcrumb && breadcrumb.length > 0) icon = breadcrumb[breadcrumb.length - 1].icon;
        if (nav && nav.icon) {
            icon = nav.icon;
            if (breadcrumb && breadcrumb.length > 0) breadcrumb[breadcrumb.length - 1].icon = icon;
        }
        if (!icon) {
            if (breadcrumb && breadcrumb.length > 0) icon = breadcrumb[breadcrumb.length - 1].icon;
        }
        page.icon = icon;        
        // set as loaded
        if (this._status == "loading") {
            this._status = "loaded"; 
        }
        // remove loading
        if (layoutElement) layoutElement.removeAttribute("status");
        // call load on page
        await this._page.load();
        // call mount on page
        await this._page.mount( { host: this });
        // raise load event
        this.dispatchEvent(new CustomEvent("load"));
    }
    async unmount() {
        // unmount
        if (this._page) {
            await this._page.unmount();
        }
    }
    async unload() {
        // unload
        if (this._page) {
            await this._page.unload();
        }
    }
    async queryClose() {
        let allowClose = true;
        // ... query close page (ask page for permissions to close) ...
        if (allowClose) this.close();
    }
    async close(result) {
        //close page
        if (result != undefined) this._page.result = result;
        this.dispatchEvent(new CustomEvent("close", { composed: true }));
    }
    async removePage() {
        //remove dom element
        let layoutElement = this.shadowRoot.firstChild;
        let removeHandler = ()=> {
            this.remove();
        };
        if (layoutElement) {
            removeHandler = layoutElement.onCommand("unload") || removeHandler;
        }
        //remove DOM node
        removeHandler();
    }
    async showError({code, message, src, module, stack, errors}) {
        // show error
        let name = xshell.config.get("xshell.component.error");
        const errorCoomponentClass = await xshell.loader.load("component:" + name);
        const errorComponent = new errorCoomponentClass();
        errorComponent.code = code;
        errorComponent.message = message;
        errorComponent.src = src;
        errorComponent.module = module;
        errorComponent.errors = errors;
        errorComponent.stack = stack;
        this.replaceChildren(errorComponent);
    }
}

//define web component
customElements.define('x-page', XPage);

//export 
export default XPage;