// utils
function stripJsonComments(s) { let o = "", i = 0, n = s.length; for (; i < n;) { let c = s[i]; if (c == '"' || c == "'") { let q = c; o += c; i++; while (i < n) { if (s[i] == "\\") { o += s[i++] + s[i++]; } else if (s[i] == q) { o += s[i++]; break; } else { o += s[i++]; } } continue; } if (c == '/' && s[i + 1] == '/') { i += 2; while (i < n && s[i] != "\n" && s[i] != "\r") i++; continue; } if (c == '/' && s[i + 1] == '*') { i += 2; while (i < n && !(s[i] == '*' && s[i + 1] == '/')) i++; i += 2; continue; } o += c; i++; }; return o; }
function combineUrls(t, n) { if (-1 != t.indexOf("?") && (t = t.substring(0, t.indexOf("?"))), -1 != n.indexOf(":")) return n; if (n.startsWith("/")) { if (-1 != t.indexOf("://")) { let i = t.indexOf("/", t.indexOf("://") + 3); return -1 != i && (t = t.substring(0, i)), t + n } return n } if (n.startsWith("./") || "." == n) return t.endsWith("/") ? t = t.substring(0, t.length - 1) : t.length > 0 && (t = t.substring(0, t.lastIndexOf("/"))), t + n.substring(1); if (n.startsWith("../")) { t.endsWith("/") ? t = t.substring(0, t.length - 1) : t.length > 0 && (t = t.substring(0, t.lastIndexOf("/"))); let i = t + "/" + n; if (i.startsWith("/")) { i = new URL(i, window.location.origin).pathname } else i = new URL(i).toString(); return i } return t.endsWith("/") || -1 != t.indexOf("/") && (t = t.substring(0, t.lastIndexOf("/") + 1)), t + n }
function meta(name) { return document.head.querySelector(`meta[name="${name}"]`)?.content; }
function deepFreeze(obj) {if (obj === null || typeof obj !== "object") {return obj;} Object.freeze(obj); for (const value of Object.values(obj)) {deepFreeze(value);} return obj;}
function absolutizePrefixedUrl(key, obj, url) {return typeof obj === "string" ? (obj.startsWith("url:") ? ((obj = obj.substring(4).trim()), (obj.startsWith("/") || obj.startsWith("./") || obj.startsWith("../") || obj === ".") ? combineUrls(url, obj) : obj) : obj) : Array.isArray(obj) ? (obj.forEach((v, i) => obj[i] = absolutizePrefixedUrl(i, v, url)), obj) : obj instanceof Object ? (Object.keys(obj).forEach(k => obj[k] = absolutizePrefixedUrl(k, obj[k], url)), obj) : obj;}
function relativizePaths(key, obj, path) { return typeof obj === "string" ? (obj.startsWith("/") ? ((obj = path + obj), obj.startsWith(document.location.origin) ? obj.substring(document.location.origin.length) : obj) : (obj.startsWith("./") || obj.startsWith("../") || obj === ".") ? ((obj = combineUrls(path + "/", obj)), obj.startsWith(document.location.origin) ? obj.substring(document.location.origin.length) : obj) : obj) : Array.isArray(obj) ? (obj.forEach((v, i) => obj[i] = relativizePaths(i, v, path)), obj) : obj instanceof Object ? (Object.keys(obj).forEach(k => obj[k] = relativizePaths(k, obj[k], path)), obj) : obj; }
function relativizeModulePaths(config, path) {const definitions = config.xshell?.areas?.definitions || {};const prefixes = Object.fromEntries(Object.entries(definitions).filter(([, area]) => Object.hasOwn(area, "prefix")).map(([id, area]) => [id, area.prefix]));relativizePaths("", config, path);for (const [id, prefix] of Object.entries(prefixes)) definitions[id].prefix = prefix;}
async function loadJsonWithComments(url) {const request = await fetch(url);if (!request.ok) throw new Error(`Failed to json file: ${result.url}`);let json = await request.text();return JSON.parse(stripJsonComments(json));}
 

// consts
const appConfigPath = meta("xshell:app.configPath");
const appParams = meta("xshell:app.params");
const appBasePath = document.location.origin + meta("xshell:app.basePath");
const xshellEnvironment = meta("xshell:xshell.environment");
const bootstrapUrl = new URL(document.currentScript.src);
const bootstrapUrlDir = bootstrapUrl.href.substring(0, bootstrapUrl.href.lastIndexOf("/") );

// methods
async function loadModuleConfig(url) {
    // load module config from the given URL
    const request = await fetch(url);
    if (!request.ok) throw new Error(`Failed to json file: ${url}`);
    let json = await request.text();
    var module = JSON.parse(stripJsonComments(json));
    // absolutize all values starting with "url:"
    for (let key in module) {
        let value = module[key];
        if (typeof value === "string" && value.startsWith("url:")) {
            module[key] = new URL(value.substring(4), url).href;
        }
    }
    return module;
}
async function loadConfig() {
    // load config
    console.log("bootstrap: loading config ...");
    
    // xshell.json
    const xshellConfigUrl = bootstrapUrlDir + "/xshell.jsonc";
    const xshellConfigTask = loadJsonWithComments(xshellConfigUrl);
    
    // root module
    const rootModuleUrl = new URL(appConfigPath, document.baseURI).href;    
    const rootModuleConfigTask = loadJsonWithComments(rootModuleUrl);
    
    // wait untils both files are readed
    await Promise.all([xshellConfigTask, rootModuleConfigTask]);
    
    // get xshell config
    const xshellConfig = await xshellConfigTask;
    const assetsPrefix = xshellConfig.xshell.assetsPrefix;
    xshellConfig.app.basePath = appBasePath;
    xshellConfig.xshell.environment = xshellEnvironment;
    xshellConfig.xshell.configUrl = xshellConfigUrl;
    xshellConfig.xshell.assetsUrl = xshellConfig.xshell.assetsUrl || "url:./";
    absolutizePrefixedUrl("", xshellConfig, xshellConfigUrl);    
    relativizeModulePaths(xshellConfig, "/" + assetsPrefix + "/xshell");
    
    // get root module config
    const rootModuleConfig = await rootModuleConfigTask;
    const rootModule = Object.values(rootModuleConfig.modules)[0];
    const rootModuleId = Object.keys(rootModuleConfig.modules)[0];
    rootModule.configUrl = rootModuleUrl;
    rootModule.assetsUrl = rootModule.assetsUrl || "url:./";
    rootModule.params = Object.fromEntries(new URLSearchParams(appParams));
    xshellConfig.app.params = rootModule.params;
    absolutizePrefixedUrl("", rootModuleConfig, rootModuleUrl);    
    relativizeModulePaths(rootModuleConfig, "/" + assetsPrefix + "/" + rootModuleId);
    
    // load referenced modules
    const registered = {}
    registered[rootModuleUrl] = {
        configUrl: rootModuleUrl,
        config: rootModuleConfig
    }
    while (true) {        
        for (let registeredItem of Object.values(registered)) {
            if (registeredItem.config && registeredItem.config.modules) {
                for (let moduleConfig of Object.values(registeredItem.config.modules)) {
                    if (moduleConfig.imports) {
                        for(let importItem of Object.values(moduleConfig.imports)) {
                            let importItemUrl = importItem.configUrl;
                            let importItemParams = importItem.params;
                            if (!registered[importItemUrl]) {
                                registered[importItemUrl] = { 
                                    configUrl: importItemUrl,
                                    params: importItemParams,
                                    task: loadModuleConfig(importItemUrl) 
                                };
                            }
                        }
                    }
                }
            }
        }
        // check if there are any pending module configurations to be loaded
        let pending = Object.values(registered).some(item => item.task && !item.config);
        if (pending == 0) break;
        // wait until all pending dependencies are loaded
        for (let registeredItem of Object.values(registered)) {
            if (registeredItem.task && !registeredItem.config) {
                registeredItem.config = await registeredItem.task;
                const registeredModule = Object.values(registeredItem.config.modules)[0];
                const registeredModuleId = Object.keys(registeredItem.config.modules)[0];
                registeredModule.configUrl = registeredItem.configUrl;
                registeredModule.assetsUrl = registeredModule.assetsUrl || "url:./";
                registeredModule.params = registeredItem.params;
                absolutizePrefixedUrl("", registeredItem.config, registeredItem.configUrl);    
                relativizeModulePaths(registeredItem.config, "/" + assetsPrefix + "/" + registeredModuleId);
                delete registeredItem.task;
            }
        }
    }

    // merge configs
    const configs = [xshellConfig, ...Object.values(registered).reverse().map(item => item.config)];
    const isObject = value => value !== null && typeof value === "object" && !Array.isArray(value);
    const merge = (target, source) => {
        for (const [key, value] of Object.entries(source)) {
            if (Array.isArray(value)) {
                target[key] = Array.isArray(target[key]) ? [...target[key], ...value] : [...value];
            } else if (isObject(value)) {
                target[key] = merge(isObject(target[key]) ? target[key] : {}, value);
            } else {
                target[key] = value;
            }
        }
        return target;
    };
    const configMerged =  configs.reduce((result, config) => merge(result, config), {});

    // default contract for modules
    for(const moduleId in configMerged.modules)   {
        const module = configMerged.modules[moduleId];
        if (!module.contract) module.contract = {};
        if (!module.contract.events) module.contract.events = {};
        if (!module.contract.intents) module.contract.intents = {};
        if (!module.contract.actions) module.contract.actions = {};
    }
    // default resolvers for modules
    for(const moduleId in configMerged.modules)   {
        const module = configMerged.modules[moduleId];
        const resolver = configMerged.xshell.resolver;
        resolver.icon = resolver.icon || {};
        resolver.icon[`${moduleId}-{name}`] = resolver.icon[`${moduleId}-{name}`] || { url: `/${assetsPrefix}/${moduleId}/icons/{name}.svg`, loader: 'icon-svg', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`}
        resolver.layout = resolver.layout || {};
        resolver.layout[`${moduleId}-layout-{name}`] = resolver.layout[`${moduleId}-layout-{name}`] || {url: `/${assetsPrefix}/${moduleId}/layouts/${moduleId}-layout-{name}.js`, loader: 'component-js', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.component = resolver.component || {};
        resolver.component[`${moduleId}-{name}`] = resolver.component[`${moduleId}-{name}`] || { url: `/${assetsPrefix}/${moduleId}/components/${moduleId}-{name}.js`, loader: 'component-js', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.page = resolver.page || {};
        resolver.page[`/${assetsPrefix}/${moduleId}/{path}.js`] = resolver.page[`/${assetsPrefix}/${moduleId}/{path}.js`] || { url: `/${assetsPrefix}/${moduleId}/{path}.js`, loader: 'page-js', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.page[`/${assetsPrefix}/${moduleId}/{path}.html`] = resolver.page[`/${assetsPrefix}/${moduleId}/{path}.html`] || { url: `/${assetsPrefix}/${moduleId}/{path}.html`, loader: 'page-html',cache: true,moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.page[`/${assetsPrefix}/${moduleId}/{path}.md`] = resolver.page[`/${assetsPrefix}/${moduleId}/{path}.md`] || { url: `/${assetsPrefix}/${moduleId}/{path}.md`, loader: 'page-md', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.module = resolver.module || {};
        resolver.module[`${moduleId}-{name}`] = resolver.module[`${moduleId}-{name}`] || { url: `/${assetsPrefix}/${moduleId}/${moduleId}-{name}.js`, loader: 'module-js', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.module[`/${assetsPrefix}/${moduleId}/{path}.js`] = resolver.module[`/${assetsPrefix}/${moduleId}/{path}.js`] || { url: `/${assetsPrefix}/${moduleId}/{path}.js`, loader: 'module-js', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.style = resolver.style || {};
        resolver.style[`/${assetsPrefix}/${moduleId}/{path}.css`] = resolver.style[`/${assetsPrefix}/${moduleId}/{path}.css`] || { url: `/${assetsPrefix}/${moduleId}/{path}.css`, loader: 'style-css', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
    }

    // console
    console.log("Config:", configMerged);

    // return
    return configMerged;
}


async function installServiceWorker(config) {

    // install service worker
    console.log("bootstrap: installing service worker ...");
    const bootstrapUrlRaw = bootstrapUrl.toString();
    const reg = await navigator.serviceWorker.register(appBasePath + "/sw.js", {
        scope: appBasePath + "/"
    });

    // creates rules to send to service worker
    const xshellVersion = config.xshell.version;
    const assetsPrefix = config.xshell.assetsPrefix;
    let rules = [];
    rules.push({ 
        src: combineUrls( (appBasePath ? appBasePath + "/" : ""), "./" + assetsPrefix + "/xshell"), 
        dst: config.xshell.assetsUrl, 
        version: xshellVersion, 
        name:"xshell", 
        exceptions:[config.xshell.configUrl]});
    for(var moduleId of Object.keys(config.modules)) {
        const module = config.modules[moduleId];
        rules.push({ 
            src: combineUrls((appBasePath ? appBasePath + "/" : ""), "./" + assetsPrefix + "/" + moduleId), 
            dst: module.assetsUrl, 
            version: module.version, 
            name: moduleId, 
            exceptions: [module.configUrl]
        });
    }

    // wait for ready
    console.log("bootstrap: waiting for ready ...");
    await navigator.serviceWorker.ready;
    
    // ensure page is controlled by service worker
    if (!navigator.serviceWorker.controller) {
        console.log("bootstrap: page is not controlled ... forcing reload");
        location.reload();
        return false;
    }

    // send init message to service
    console.log("bootstrap: send init message to service worker ...");
    await new Promise((resolve, reject) => {
        const channel = new MessageChannel();
        channel.port1.onmessage = (event) => {
            resolve(event.data);
            channel.port1.close();
        };
        // safety timeout
        setTimeout(() => {
            reject(new Error("Service Worker did not reply in time"));
            channel.port1.close();
        }, 5000);
        // send init + transfer reply port
        reg.active.postMessage({ type: "init", payload: {rules: rules} }, [channel.port2]);
    });

    // log
    console.log("bootstrap: service worker ready to receive requests");

    // return
    return true;
}

async function bootstrap() {
    
    // load config
    let config = await loadConfig();
    
    // installServiceWorker
    if (!await installServiceWorker(config)){
        return;
    }    

    // import xshell ES6 module
    console.log("bootstrap: loading xshell ...");
    const xshellUrl = config.xshell.resolver.import.xshell.url;
    const xshellModule = await import(xshellUrl);
    let xshell = xshellModule.default;
    
    // init xshell
    await xshell.init(deepFreeze(config));
}

// exec bootstrap
bootstrap();

