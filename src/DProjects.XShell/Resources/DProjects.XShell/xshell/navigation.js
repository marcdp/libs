import {combineUrls} from "./utils/urls.js";
import { base64UrlEncode, base64UrlDecode } from "./utils/base64.js";
import { compileRoute } from "./utils/route.js";

// consts
export const HASH_PREFIX = "#!";

// class
export default class Navigation {

    // vars
    _areas = null;
    _bus = null;
    _config = null;
    _container = null;

    _mode = ""; 
    _appBasePath = "";
    _compiledRoutes = new Map();

    _stack = [];
    _stackToDomTask = Promise.resolve();
    
    // ctor
    constructor( { areas, bus, config, container} ) {
        this._areas = areas;
        this._bus = bus;
        this._config = config;
        this._container = container;
        this._mode = config.xshell.navigation.mode;
        this._appBasePath = config.app.basePath;
        if (this._appBasePath == "/") this._appBasePath = "";
    }

    // props
    get mode() { return this._mode; }
    get src() { 
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
                await this._stackToDom();
            });
            // init
            if (document.location.hash) {
                this._stack = this._browserUrlToStack(document.location.hash);
                await this._stackToDom();
            } else {
                let defaultArea = this._areas.getDefaultArea();
                if (!defaultArea?.home) throw new Error("Default area has no navigation item marked default");
                await this._stackToBrowser([this.parseUrl(defaultArea.home)], { replace: false });
            }
        } else if (this._mode == "path") {
            // path mode
            window.addEventListener("popstate", async () => {
                let url = document.location.pathname + document.location.search;
                if (url.startsWith(this._appBasePath)) url = url.substring(this._appBasePath.length);
                this._stack = this._browserUrlToStack(url + document.location.hash);
                await this._stackToDom();
            });
            // init
            let url = document.location.pathname + document.location.search;
            if (url.startsWith(this._appBasePath)) url = url.substring(this._appBasePath.length);
            if (url != "" && url != "/") {
                this._stack = this._browserUrlToStack(url + document.location.hash);
                await this._stackToDom();
            } else {
                let defaultArea = this._areas.getDefaultArea();
                if (!defaultArea?.home) throw new Error("Default area has no navigation item marked default");
                url = this.parseUrl(defaultArea.home);
                await this._stackToBrowser([url], { replace: false });
            }
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
    replacePageQuery(page, changes) {
        // patch one Page query and replace the browser URL when the Page is in the stack
        if (!changes || typeof(changes) !== "object" || Array.isArray(changes)) {
            throw new TypeError("Navigation.replacePageQuery: changes must be an object.");
        }
        const xpage = page?.host || null;
        const xpages = xpage ? this.getXPages() : [];
        const index = xpages.indexOf(xpage);
        if (index < 0 || index >= this._stack.length) {
            return this._replacePageSrcQuery(page?.src || null, changes);
        }
        const item = this._stack[index];
        const params = { ...(item.params || {}) };
        this._applyQueryChanges(params, changes);
        const stack = [...this._stack];
        stack[index] = { ...item, params };
        this._stackToBrowser(stack, { replace: true });
        return this._buildUrlFinal(stack[index]);
    }
    replacePageHash(page, hash) {
        // todo ...

    }
    buildUrlAbsolute(params){
        let href = this._buildUrlPublic(params);
        if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(href)) return href;
        if (this._mode == "hash") {
            href = this._appBasePath + "/" + HASH_PREFIX + href;
        } else {
            href = this._appBasePath + href;
        }
        return href;
    }
    buildUrl({
            href,           // url relative to app base
            params = {},    // ws variables to be added as query string parameters
            nav = {              // navigation data to be added as query string parameters (nav.title, nav.icon, nav.breadcrumb)
                title: null,      // nav.title
                description: null,// nav.description
                icon: null,       // nav.icon
                breadcrumb: null},// nav.breadcrumb
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
            href = this._appendQueryParameter(href, encodeURIComponent(k), encodeURIComponent(v));
        }    
        // nav
        if (nav && (nav.title || nav.description || nav.icon || nav.breadcrumb)) {
            const navJson = JSON.stringify(nav, (key, value) => {
                if (value !== null) return value;
                return undefined;
            });
            href = this._appendQueryParameter(href, "nav", base64UrlEncode(navJson));
        }
        // return
        return href;
    }
    parseUrl(url) {
        if (!url || typeof url !== "string") throw new Error("parseUrl: url must be a non-empty string");
        // remove hash prefix if present (#!)
        if (url.startsWith("#!")) url = url.substring(2);
        // separate path, query and fragment while retaining the fragment as part of the logical href
        const hashIndex = url.indexOf("#");
        const fragment = hashIndex === -1 ? "" : url.substring(hashIndex);
        const source = hashIndex === -1 ? url : url.substring(0, hashIndex);
        const queryIndex = source.indexOf("?");
        const pathPart = queryIndex === -1 ? source : source.substring(0, queryIndex);
        const queryPart = queryIndex === -1 ? "" : source.substring(queryIndex + 1);
        const result = {
            href: (pathPart || "") + fragment,
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
            params = {},    // variables to be added as query string parameters
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
        const hrefAbsolute = this.buildUrl({ href: this._resolveCanonicalHref(href, page), params, nav, page });
        // open
        if (open == "auto") {
            // auto
            const xpage = page.host;
            const xpages = this.getXPages();
            const indexPage = xpages.indexOf(xpage);
            if  (xpage == null) {
                // top
                await this._stackToBrowser([ this.parseUrl(hrefAbsolute) ], { replace });
            } else if (indexPage == 0) {
                // top
                await this._stackToBrowser([ this.parseUrl(hrefAbsolute) ], { replace });
            } else if (indexPage != -1) {
                // stackpage
                let stack = [...this._stack];
                stack[indexPage] = this.parseUrl(hrefAbsolute);
                await this._stackToBrowser(stack, { replace });
            } else {
                // dialog or embed 
                const hrefFinal = this._buildUrlFinal(this.parseUrl(hrefAbsolute));
                xpage.setAttribute("src", hrefFinal);
            }
        } else if (open == "top") {
            // top                        
            await this._stackToBrowser([ this.parseUrl(hrefAbsolute) ], { replace });

        } else if (open == "stack") {
            // stack
            await this._stackToBrowser([...this._stack, this.parseUrl(hrefAbsolute)], { replace });
            
        } else if (open == "dialog") {
            // dialog
            const hrefFinal = this._buildUrlFinal(this.parseUrl(hrefAbsolute));
            return await this._showDialog({ href: hrefFinal, context });

        } else if (open == "embed") {
            // embed
            if (typeof outlet === "string") {
                const xpage = page.host;
                const outletElement = xpage.querySelector(`x-page[outlet="${outlet}"]`);
                if (outletElement) {
                    const hrefFinal = this._buildUrlFinal(this.parseUrl(hrefAbsolute));
                    outletElement.setAttribute("src", hrefFinal);
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
            url = this._buildUrlPublic({
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
                const navJson = JSON.stringify(aux, (key, value) => {
                    if (value !== null) return value;
                    return undefined;
                });
                url += (url.includes("?") ? "&" : "?") + "nav=" + base64UrlEncode(navJson);
            }
        }
        this._stack = stack;
        if (this._mode === "hash") {
            if (replace) {
                location.replace(HASH_PREFIX + url);
            } else {
                location.hash = HASH_PREFIX + url;
            }            
        } else if (this._mode === "path") {
            if (replace) {
                history.replaceState(null, "", this._appBasePath + url);
            } else {
                history.pushState(null, "", this._appBasePath + url);
            }
        }
        // keep the live Page elements and the logical stack aligned without reloading href-identical Pages
        return this._stackToDom();
    }
    _browserUrlToStack(url) {
        if (!url || typeof url !== "string") {
            throw new Error("_browserUrlToStack: url must be a non-empty string");
        }
        // remove hash prefix if needed
        if (this._mode === "hash") {
            if (url.startsWith(HASH_PREFIX)) {
                url = url.substring(HASH_PREFIX.length);
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
        // serialize reconciliations so a pending Page unload cannot be selected again
        const task = this._stackToDomTask.then(() => this._reconcileStackToDom());
        this._stackToDomTask = task.then(() => {}, () => {});
        return task;
    }
    async _reconcileStackToDom() {
        //navigate
        const xpages = this.getXPages();
        let domStack = xpages.map(xpage => { return this.parseUrl(xpage.src); });
        // close open dialogs through their final Page destruction path
        let allXPages = Array.from(this._container.querySelectorAll(":scope > x-page"));
        for (let i = allXPages.length - 1; i >= 0; i--) {
            let xpage = allXPages[i];
            if (xpage.getAttribute("layout") != "dialog") break;
            xpage.close();
        }
        //process stack
        for (let i = 0; i < Math.max(this._stack.length, domStack.length); i++) {
            let itemBefore = domStack[i];
            let itemAfter = this._stack[i];
            if (!itemBefore && itemAfter) {
                //add page
                let xpage = document.createElement("x-page");
                const hrefFinal = this._buildUrlFinal(itemAfter);
                xpage.setAttribute("src", hrefFinal);
                if (i == 0) {
                    xpage.setAttribute("layout", "main");
                    //emit event navigation-start
                    this._bus.emit("xshell:navigation:start", { src: xpage.src });
                } else {
                    xpage.setAttribute("layout", "stack");
                    xpage.addEventListener("close", async (event) => {
                        //page close
                        let xpages = this.getXPages();
                        let index = xpages.indexOf(event.target);
                        this._stack.splice(index, 1);
                        await this._stackToBrowser(this._stack, { replace: false });
                    });
                }
                xpage.addEventListener("change", (event) => {
                    //page change
                    let xpages = this.getXPages();
                    if (xpages.indexOf(event.target) == 0) {
                        var label = event.target.label;
                        if (label) document.title = label + " / " + this._config.app.label;
                    }
                });
                xpage.addEventListener("load", (event) => {
                    //page load
                    let xpages = this.getXPages();
                    if (xpages.indexOf(event.target) == 0) {
                        var label = event.target.label;
                        if (label) document.title = label + " / " + this._config.app.label;
                        this._bus.emit("xshell:navigation:end", { src: event.target.src, id: event.target.page.id});
                    }
                });
                //add page to container
                this._container.appendChild(xpage);
            } else if (itemBefore && !itemAfter) {
                //remove page
                // use the original host for this position and await its final cleanup
                await xpages[i].removePage();
            } else if (itemBefore.href != itemAfter.href) {
                //change page
                const hrefFinal = itemAfter ? this._buildUrlFinal(itemAfter) : null;
                const itemFinal = hrefFinal ? this.parseUrl(hrefFinal) : null;
                if (itemBefore.href != itemFinal.href) {
                    let xpages = this.getXPages();
                    let xpage = xpages[i];
                    xpage.src = hrefFinal;
                    //emit event navigation-start
                    if (i == 0) {
                        this._bus.emit("xshell:navigation:start", { src: xpage.src });
                    }
                }
            }
        }
    }
    async _showDialog({ href, context }) {
        //show page dialog
        let xpage = document.createElement("x-page");
        const hrefFinal = this._buildUrlFinal(this.parseUrl(href));
        xpage.setAttribute("src", hrefFinal);
        xpage.setAttribute("layout", "dialog");
        xpage.context = context || {};
        return new Promise((resolve, reject) => {
            let closing = false;
            xpage.addEventListener("close", async () => {
                if (closing) return;
                closing = true;
                try {
                    // preserve the result before final Page destruction clears the Page reference
                    const result = xpage.result;
                    await xpage.removePage();
                    resolve(result);
                } catch (error) {
                    reject(error);
                }
            });
            this._container.appendChild(xpage);
        });
    }
    _buildUrlFinal(item) {
        const { identity, suffix } = this._splitHref(item.href);
        const menuitem = this._areas.resolvePath(identity);
        if (menuitem) {
            return this.buildUrl({
                ...item,
                href: menuitem.href + suffix
            });
        }
        const area = this._areas.getArea?.(this._areas.resolveAreaId?.(identity));
        if (area) {
            const routePath = this._removeAreaPrefix(identity, area);
            const match = this._matchAreaRoutes(routePath, area);
            if (match) return this._buildRouteHref(match, item, area);
        }
        return this.buildUrl({
            ...item,
            href: identity + suffix
        });
    }
    _buildUrlPublic(params) {
        const target = this._resolvePublicTarget(params.href, params.params || {}, params.page);
        return this.buildUrl({ ...params, href: target.href, params: target.params });
    }
    _resolvePublicHref(href, page) {
        return this._resolvePublicTarget(href, {}, page).href;
    }
    _resolvePublicTarget(href, params, page) {
        // leave external targets under native browser control
        if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(href)) return { href, params };
        const canonicalHref = this._resolveCanonicalHref(href, page);
        const { identity, suffix } = this._splitHref(canonicalHref);
        const areaId = this._areas.resolveAreaId?.(identity);
        const area = this._areas.getArea?.(areaId);
        const menuitem = area ? this._areas.resolveHref?.(identity, area.id) : null;
        if (menuitem) return { href: (menuitem.path || identity) + suffix, params };
        for (const routeArea of this._getPublicRouteAreas(href, page)) {
            const routeTarget = this._resolvePublicRouteTarget(canonicalHref, params, routeArea);
            if (routeTarget) return routeTarget;
        }
        return { href: canonicalHref, params };
    }
    _resolveCanonicalHref(href, page) {
        // resolve relative targets and apply the originating navigation context
        if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(href)) return href;
        if (href.startsWith("#!")) href = href.substring(2);
        if (!href.startsWith("/")) href = combineUrls(page ? page.src : "/", href);
        const { identity, suffix } = this._splitHref(href);
        const targetArea = this._areas.getArea?.(this._areas.resolveAreaId?.(identity));
        if (targetArea?.prefix) return identity + suffix;
        const pageAreaId = page?.src ? this._areas.resolveAreaId?.(page.src) : null;
        const menuitem = this._areas.resolveHref?.(identity, pageAreaId) || this._areas.resolveHref?.(identity);
        if (menuitem) return menuitem.href + suffix;
        const area = page ? this._areas.getArea?.(pageAreaId) : this._areas.getCurrentArea?.() || this._areas.getDefaultArea?.();
        if (!area) return href;
        const canonicalHref = this._applyAreaPrefix(identity, area);
        return canonicalHref + suffix;
    }
    _compileRoute(path) {
        return compileRoute(path);
    }
    _matchAreaRoutes(path, area) {
        return this._matchRoutes(path, area?.routes || []);
    }
    _matchRoutes(path, routes) {
        if (typeof path !== "string") throw new TypeError("matchRoutes: path must be a string");
        const pathname = this._splitHref(path).identity;
        for (const route of routes || []) {
            const compiled = this._getCompiledRoute(route.path);
            const match = compiled.matcher.exec(pathname);
            if (!match) continue;
            const params = {};
            for (let index = 0; index < compiled.parameters.length; index++) {
                try {
                    params[compiled.parameters[index]] = decodeURIComponent(match[index + 1]);
                } catch (error) {
                    throw new Error(`Invalid encoded route parameter in path '${path}'.`, { cause: error });
                }
            }
            return { route, params };
        }
        return null;
    }
    _getCompiledRoute(path) {
        let compiled = this._compiledRoutes.get(path);
        if (!compiled) {
            compiled = this._compileRoute(path);
            this._compiledRoutes.set(path, compiled);
        }
        return compiled;
    }
    _buildRouteHref(match, item, area) {
        // merge the route target and incoming URL while keeping path parameters authoritative
        const target = this.parseUrl(match.route.href);
        const targetParts = this._splitHref(target.href);
        const inputParts = this._splitHref(item.href);
        const targetIdentity = this._applyAreaPrefix(targetParts.identity, area);
        const params = { ...target.params, ...match.params };
        for (const [key, value] of Object.entries(item.params || {})) {
            if (!Object.hasOwn(match.params, key)) params[key] = value;
        }
        return this.buildUrl({
            ...item,
            href: targetIdentity + (inputParts.suffix || targetParts.suffix),
            params
        });
    }
    _getPublicRouteAreas(href, page) {
        const areas = [];
        const add = area => {
            if (area && !areas.includes(area)) areas.push(area);
        };
        const identity = this._splitHref(href).identity;
        add(this._areas.getArea?.(this._areas.resolveAreaId?.(identity)));
        if (page?.src) add(this._areas.getArea?.(this._areas.resolveAreaId?.(page.src)));
        const moduleId = this._areas.getModuleId?.(identity);
        if (moduleId) {
            for (const area of this._areas.getAreas?.() || []) {
                if (area.modules.includes(moduleId)) add(area);
            }
        }
        add(this._areas.getCurrentArea?.());
        add(this._areas.getDefaultArea?.());
        return areas;
    }
    _resolvePublicRouteTarget(canonicalHref, suppliedParams, area) {
        const canonical = this.parseUrl(canonicalHref);
        const canonicalParts = this._splitHref(canonical.href);
        const canonicalArea = this._areas.getArea?.(this._areas.resolveAreaId?.(canonicalParts.identity));
        const canonicalIdentity = this._removeAreaPrefix(canonicalParts.identity, canonicalArea);
        const canonicalParams = { ...canonical.params, ...suppliedParams };
        for (const route of area.routes || []) {
            const target = this.parseUrl(route.href);
            const targetIdentity = this._splitHref(target.href).identity;
            if (targetIdentity !== canonicalIdentity) continue;
            let applicable = true;
            for (const [key, value] of Object.entries(target.params)) {
                if (!Object.hasOwn(canonicalParams, key) || String(canonicalParams[key]) !== value) {
                    applicable = false;
                    break;
                }
            }
            if (!applicable) continue;
            const compiled = this._getCompiledRoute(route.path);
            for (const parameter of compiled.parameters) {
                const value = canonicalParams[parameter];
                if (!Object.hasOwn(canonicalParams, parameter) || value === null || typeof value === "undefined" || String(value) === "") {
                    applicable = false;
                    break;
                }
            }
            if (!applicable) continue;
            const routePath = "/" + compiled.segments.map(segment => Object.hasOwn(segment, "parameter")
                ? encodeURIComponent(String(canonicalParams[segment.parameter]))
                : segment.literal).join("/");
            const remainingParams = { ...canonicalParams };
            for (const key of Object.keys(target.params)) delete remainingParams[key];
            for (const parameter of compiled.parameters) delete remainingParams[parameter];
            const publicPath = this._applyAreaPrefix(routePath, area);
            return { href: publicPath + canonicalParts.suffix, params: remainingParams };
        }
        return null;
    }
    _removeAreaPrefix(path, area) {
        // derive the Area-relative identity without modifying route declarations
        if (!area?.prefix) return path;
        if (path === area.prefix) return "/";
        return path.startsWith(area.prefix + "/") ? path.substring(area.prefix.length) : path;
    }
    _applyAreaPrefix(path, area) {
        // apply the public navigation context exactly once
        if (!area?.prefix || path === area.prefix || path.startsWith(area.prefix + "/")) return path;
        return area.prefix + path;
    }
    _splitHref(href) {
        const queryIndex = href.indexOf("?");
        const hashIndex = href.indexOf("#");
        const suffixIndexes = [queryIndex, hashIndex].filter(index => index !== -1);
        const suffixIndex = suffixIndexes.length ? Math.min(...suffixIndexes) : href.length;
        return { identity: href.substring(0, suffixIndex), suffix: href.substring(suffixIndex) };
    }
    _appendQueryParameter(href, key, value) {
        const hashIndex = href.indexOf("#");
        const fragment = hashIndex === -1 ? "" : href.substring(hashIndex);
        const source = hashIndex === -1 ? href : href.substring(0, hashIndex);
        return source + (source.includes("?") ? "&" : "?") + key + "=" + value + fragment;
    }
    _applyQueryChanges(params, changes) {
        // apply query patches while preserving unrelated parameters
        for (const [key, value] of Object.entries(changes)) {
            if (typeof(value) === "undefined") {
                continue;
            }
            if (value === null) {
                delete params[key];
                continue;
            }
            if (typeof(value) === "number" && !Number.isFinite(value)) {
                throw new TypeError(`Navigation.replacePageQuery: query parameter '${key}' must be finite.`);
            }
            if (!["string", "number", "boolean"].includes(typeof(value))) {
                throw new TypeError(`Navigation.replacePageQuery: query parameter '${key}' must be a scalar, null, or undefined.`);
            }
            params[key] = String(value);
        }
    }
    _replacePageSrcQuery(src, changes) {
        // patch a dialog or embedded Page source without touching browser navigation
        if (typeof(src) !== "string") {
            return null;
        }
        const hashIndex = src.indexOf("#");
        const hash = hashIndex < 0 ? "" : src.substring(hashIndex);
        const source = hashIndex < 0 ? src : src.substring(0, hashIndex);
        const queryIndex = source.indexOf("?");
        const href = queryIndex < 0 ? source : source.substring(0, queryIndex);
        const query = new URLSearchParams(queryIndex < 0 ? "" : source.substring(queryIndex + 1));
        const params = Object.fromEntries(query.entries());
        this._applyQueryChanges(params, changes);
        const serialized = new URLSearchParams(params).toString();
        return href + (serialized ? `?${serialized}` : "") + hash;
    }
}

