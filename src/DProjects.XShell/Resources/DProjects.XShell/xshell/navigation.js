import {combineUrls} from "./utils/urls.js";
import { base64UrlEncode, base64UrlDecode } from "./utils/base64.js";

// class
export default class Navigation {

    // vars
    _areas = null;
    _bus = null;
    _config = null;
    _container = null;

    _mode = ""; //hash|path
    _hashPrefix = "";
    _appBase = "";

    _stack = [];
    
    // ctor
    constructor( { areas, bus, config, container} ) {
        this._areas = areas;
        this._bus = bus;
        this._config = config;
        this._container = container;
        this._mode = config.get("navigation.mode") || "hash";
        this._hashPrefix = config.get("navigation.hashPrefix");
        this._appBase = this._config.get("app.base");        
    }

    // props
    get mode() { return this._mode; }
    get src() { 
        debugger;
        this._stack[0].href;
        let xpage = this.getXPage();
        return (xpage ? xpage.src : null);
    }
    get stack() {
        return this._stack.map(p => Object.freeze({
            href: p.href,
            params: p.params,
            nav: p.nav
        }))
    }
    
    // init
    async init() {
        if (this._mode == "hash") {
            // hash mode
            window.addEventListener("hashchange", async () => {
                this._stack = this._browserUrlToStack(document.location.hash);
                this._stackToDom();
            });
            // init
            if (document.location.hash) {
                this._stack = this._browserUrlToStack(document.location.hash);
                this._stackToDom();
            } else {
                let defaultArea = this._areas.getDefaultArea();
                this._stackToBrowser([this.parseUrl(defaultArea.home)], { replace: false });
            }
        } else if (this._mode == "path") {
            // path mode
            // todo ...
            //window.addEventListener("popstate", async () => {
            //    this._stack = this._browserUrlToStack(document.location.pathname + document.location.search);
            //    this._syncDomWithStack();
            //});
            // init
            throw new Error("Path mode is not implemented yet");
        }
    }

    // methods
    getXPage() {
        // get the first page that is a direct child of the container and not a dialog
        return this.getXPages()[0];
    }
    getXPages() {
        // get pages that are direct children of the container and not dialogs
        return Array.from(this._container.querySelectorAll(":scope > x-page:not([layout='dialog'])"));
    }
    getXPageFromElement(target) {
        // get the page element from a target element by traversing up the DOM tree
        while (target) {
            if (!target.parentNode) {
                target = target.host;
            }
            if (!target) {
                return null;
            }
            if (target.localName == "x-page") return target;
            target = target.parentNode;
        }
        return null;
    }
    buildUrlAbsolute(...params){
        let href = this.buildUrl(...params);
        if (this._mode == "hash") {
            href = this._appBase + "/" + this._hashPrefix + href;
        } else {
            href = this._appBase + href;
        }
        return href;
    }
    buildUrl({
            href,           // url relative to app base
            params = {},    // ws variables to be added as query string parameters
            nav = {         // navigation data to be added as query string parameters (nav.title, nav.icon, nav.breadcrumb)
                title,      // nav.title
                description,// nav.description
                icon,       // nav.icon
                breadcrumb},// nav.breadcrumb
            page,           // current page element for resolving relative urls
        }) {
        // produces real, navigable URLS
        if (!href || typeof href !== "string") throw new Error("buildUrl: href must be a non-empty string");
        if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(href)) return href;
        if (href.startsWith("#!")) href = href.substring(2);
        // resolve relative urls
        if (!href.startsWith("/")) href = combineUrls(page ? page.src : "/", href);
        // params
        for (const [k, v] of Object.entries(params)) {
            href += (href.includes("?") ? "&" : "?") + encodeURIComponent(k) + "=" + encodeURIComponent(v);
        }    
        // nav
        if (nav && (nav.title || nav.description || nav.icon || nav.breadcrumb)) {
            href += (href.includes("?") ? "&" : "?") + "nav=" + base64UrlEncode(JSON.stringify(nav));
        }
        // return
        return href;
    }
    parseUrl(url) {
        if (!url || typeof url !== "string") throw new Error("parseUrl: url must be a non-empty string");
        // remove hash prefix if present (#!)
        if (url.startsWith("#!")) url = url.substring(2);
        // separate path and query
        const [pathPart, queryPart] = url.split("?");
        const result = {
            href: pathPart || "",
            params: {},
            nav: {
                title: null,
                description: null,
                icon: null,
                breadcrumb: null
            }
        };
        if (!queryPart) return result;
        const searchParams = new URLSearchParams(queryPart);
        for (const [key, value] of searchParams.entries()) {
            if (key === "nav") {
                try {
                    const json = base64UrlDecode(value);
                    result.nav = JSON.parse(json);
                } catch {
                    result.nav = {
                        title: null,
                        description: null,
                        icon: null,
                        breadcrumb: null
                    };
                }
            } else {
                result.params[key] = value;
            }
        }
        return result;
    }

    async navigate({
            href,
            params = {},    // ws variables to be added as query string parameters
            nav = {         // navigation data to be added as query string parameters (nav.title, nav.icon, nav.breadcrumb)
                title: null,      // nav.title
                description: null,// nav.description
                icon: null,       // nav.icon
                breadcrumb: null},// nav.breadcrumb
            page,
            outlet,         // target outlet for embed mode (outlet name or element)
            open = "auto",  // auto | top | stack | dialog | embed 
            context = {},   // additional context to be passed to the page (e.g. dialog parameters)
            replace = false
        }) {
        //navigate
        const hrefAbsolute = this.buildUrl({ href, params, nav, page });
        // open
        if (open == "auto") {
            // auto
            const xpage = page.host;
            const xpages = this.getXPages();
            const indexPage = xpages.indexOf(xpage);
            if  (xpage == null) {
                // top
                this._stackToBrowser([ this.parseUrl(hrefAbsolute) ], { replace });
            } else if (indexPage == 0) {
                // top
                this._stackToBrowser([ this.parseUrl(hrefAbsolute) ], { replace });
            } else if (indexPage != -1) {
                // stackpage
                let stack = [...this._stack];
                stack[indexPage] = this.parseUrl(hrefAbsolute);
                this._stackToBrowser(stack, { replace } );                
            } else {
                // dialog or embed                
                xpage.setAttribute("src", hrefAbsolute);
            }
        } else if (open == "top") {
            // top                        
            this._stackToBrowser([ this.parseUrl(hrefAbsolute) ], { replace });

        } else if (open == "stack") {
            // stack
            this._stackToBrowser([...this._stack, this.parseUrl(hrefAbsolute)], { replace });
            
        } else if (open == "dialog") {
            // dialog
            return await this._showDialog({ href: hrefAbsolute, context });

        } else if (open == "embed") {
            // embed
            if (typeof outlet === "string") {
                const xpage = page.host;
                const outletElement = xpage.querySelector(`x-page[outlet="${outlet}"]`);
                if (outletElement) {
                    outletElement.setAttribute("src", hrefAbsolute);
                }
            }
        }
    }


    // private methods
    _stackToBrowser(stack, { replace }) {
        let url = "";
        if (stack.length) {
            // root page
            const root = stack[0];
            url = this.buildUrl({
                href: root.href,
                params: root.params,
                nav: (stack.length == 1 ? root.nav : null)
            })
            // for stack with multiples pages, creates a single nav variable encoded in base64 with the nav data of the root page and a stack array with the href, params and nav of the rest of pages
            if (stack.length > 1) {
                let aux = {};
                aux.title = root.nav.title;
                aux.description = root.nav.description;
                aux.icon = root.nav.icon;
                aux.breadcrumb = root.nav.breadcrumb;
                aux.stack = stack.slice(1);
                url += (url.includes("?") ? "&" : "?") + "nav=" + base64UrlEncode(JSON.stringify(aux));
            }
        }
        if (this._mode === "hash") {
            if (replace) {
                location.replace(this._hashPrefix + url);
            } else {
                location.hash = this._hashPrefix + url;
            }            
        } else if (this._mode === "path") {
            if (replace) {
                history.replaceState(null, "", this._appBase + url);
            } else {
                history.pushState(null, "", this._appBase + url);
            }
        }
    }
    _browserUrlToStack(url) {
        if (!url || typeof url !== "string") {
            throw new Error("_browserUrlToStack: url must be a non-empty string");
        }
        // remove hash prefix if needed
        if (this._mode === "hash") {
            if (url.startsWith(this._hashPrefix)) {
                url = url.substring(this._hashPrefix.length);
            }
        }

        // parse root page normally
        const rootParsed = this.parseUrl(url);
        const stack = [];
        // root page without nav.stack
        const { nav, ...rootWithoutNavStack } = rootParsed;
        const rootItem = {
            href: rootParsed.href,
            params: rootParsed.params,
            nav: {
                title: rootParsed.nav.title,
                description: rootParsed.nav.description,
                icon: rootParsed.nav.icon,
                breadcrumb: rootParsed.nav.breadcrumb
            }
        };
        stack.push(rootItem);
        // if nav.stack exists, decode it
        const navEncoded = new URLSearchParams(url.split("?")[1] || "").get("nav");
        if (navEncoded) {
            try {
                // restore base64
                const base64 = base64UrlDecode(navEncoded);
                const nav = JSON.parse(base64); // array of url strings
                if (nav.title) stack[0].nav.title = nav.title;
                if (nav.description) stack[0].nav.description = nav.description;
                if (nav.icon) stack[0].nav.icon = nav.icon;
                if (nav.breadcrumb) stack[0].nav.breadcrumb = nav.breadcrumb;
                if (nav.stack && Array.isArray(nav.stack)) {
                    // if nav.stack exists, decode it and add it to the stack
                    stack.push(...nav.stack);
                }
                
            } catch (e) {
                console.warn("Invalid nav value", e);
            }
        }
        return stack;
    }
    _stackToDom() {
        //navigate
        let domStack = this.getXPages().map(xpage => { return this.parseUrl(xpage.src); });
        let inc = 0;
        //close the last dialog
        let allXPages = Array.from(this._container.querySelectorAll(":scope > x-page"));
        for (let i = allXPages.length - 1; i >= 0; i--) {
            let xpage = allXPages[i];
            if (xpage.getAttribute("layout") != "dialog") break;
            this._container.removeChild(xpage);            
        }
        //process stack
        for (let i = 0; i < Math.max(this._stack.length, domStack.length); i++) {
            let itemBefore = domStack[i];
            let itemAfter = this._stack[i];
            if (!itemBefore && itemAfter) {
                //add page
                let xpage = document.createElement("x-page");
                xpage.setAttribute("src", this.buildUrl(itemAfter));
                if (i == 0) {
                    xpage.setAttribute("layout", "main");
                    //emit event navigation-start
                    this._bus.emit("xshell:navigation:start", { src: xpage.src });
                } else {
                    xpage.setAttribute("layout", "stack");
                    xpage.addEventListener("close", (event) => {
                        //page close
                        let xpages = this.getXPages();
                        let index = xpages.indexOf(event.target);
                        this._stack.splice(index, 1);
                        this._stackToBrowser(this._stack, { replace: false });
                    });
                }
                xpage.addEventListener("change", (event) => {
                    //page change
                    debugger; // TODO ...
                    let xpages = this.getXPages();
                    if (xpages.indexOf(event.target) == 0) {
                        var label = event.target.label;
                        if (label) document.title = label + " / " + this._config.get("app.label");
                    }
                });
                xpage.addEventListener("replace", (event) => {
                    //page replace
                    debugger; // TODO ...
                    let xpages = this.getXPages();
                    let hashParts = document.location.hash.substring(this._hashPrefix.length).split(this._hashPrefix);
                    let index = xpages.indexOf(event.target);
                    hashParts[index] = event.target.src;
                    history.replaceState(null, "", this._hashPrefix + hashParts.join(this._hashPrefix));
                });
                xpage.addEventListener("load", (event) => {
                    //page load
                    let xpages = this.getXPages();
                    if (xpages.indexOf(event.target) == 0) {
                        var label = event.target.label;
                        if (label) document.title = label + " / " + this._config.get("app.label");
                        this._bus.emit("xshell:navigation:end", { src: event.target.src, id: event.target.page.id});
                    }
                });
                xpage.addEventListener("navigate", (event) => {
                    //page navigation
                    debugger;
                    let xpages = this.getXPages();
                    let index = xpages.indexOf(event.target);
                    if (index != -1) {
                        let hashParts = document.location.hash.substring(this._hashPrefix.length).split(this._hashPrefix);
                        hashParts[index] = event.detail;
                        document.location.hash = this._hashPrefix + hashParts.join(this._hashPrefix);
                    }
                });
                //add page to container
                this._container.appendChild(xpage);
            } else if (itemBefore && !itemAfter) {
                //remove page
                let xpages = this.getXPages();
                let xpage = xpages[i + inc];
                xpage.removePage();
                inc -= 1;
            } else if (itemBefore.href != itemAfter.href) {
                //change page
                let xpages = this.getXPages();
                let xpage = xpages[i];
                xpage.src = this.buildUrl(itemAfter);
                //emit event navigation-start
                if (i == 0) {
                    this._bus.emit("xshell:navigation:start", { src: xpage.src });
                }
            }
        }
    }
    async _showDialog({ href, context }) {
        //show page dialog
        let resolveFunc = null;
        let xpage = document.createElement("x-page");
        xpage.setAttribute("src", href);
        xpage.setAttribute("layout", "dialog");
        xpage.addEventListener("close", (event) => {
            resolveFunc(event.target.result);
            if (event.target.parentNode) {
                event.target.parentNode.removeChild(event.target);
            }
        });
        xpage.context = context || {};
        this._container.appendChild(xpage);
        return new Promise((resolve) => {
            resolveFunc = resolve;
        });
    }
}

