// utils
function stripJsonComments(s) { let o = "", i = 0, n = s.length; for (; i < n;) { let c = s[i]; if (c == '"' || c == "'") { let q = c; o += c; i++; while (i < n) { if (s[i] == "\\") { o += s[i++] + s[i++]; } else if (s[i] == q) { o += s[i++]; break; } else { o += s[i++]; } } continue; } if (c == '/' && s[i + 1] == '/') { i += 2; while (i < n && s[i] != "\n" && s[i] != "\r") i++; continue; } if (c == '/' && s[i + 1] == '*') { i += 2; while (i < n && !(s[i] == '*' && s[i + 1] == '/')) i++; i += 2; continue; } o += c; i++; }; return o; }
function combineUrls(t, n) { if (-1 != t.indexOf("?") && (t = t.substring(0, t.indexOf("?"))), -1 != n.indexOf(":")) return n; if (n.startsWith("/")) { if (-1 != t.indexOf("://")) { let i = t.indexOf("/", t.indexOf("://") + 3); return -1 != i && (t = t.substring(0, i)), t + n } return n } if (n.startsWith("./") || "." == n) return t.endsWith("/") ? t = t.substring(0, t.length - 1) : t.length > 0 && (t = t.substring(0, t.lastIndexOf("/"))), t + n.substring(1); if (n.startsWith("../")) { t.endsWith("/") ? t = t.substring(0, t.length - 1) : t.length > 0 && (t = t.substring(0, t.lastIndexOf("/"))); let i = t + "/" + n; if (i.startsWith("/")) { i = new URL(i, window.location.origin).pathname } else i = new URL(i).toString(); return i } return t.endsWith("/") || -1 != t.indexOf("/") && (t = t.substring(0, t.lastIndexOf("/") + 1)), t + n }
function meta(name) { return document.head.querySelector(`meta[name="${name}"]`)?.content; }
async function loadJsonWithComments(url) {const request = await fetch(url);if (!request.ok) throw new Error(`Failed to json file: ${result.url}`);let json = await request.text();return JSON.parse(stripJsonComments(json));}
function absolutizePrefixedUrl(key, obj, url) {
    if (typeof (obj) == "string") {
        if (obj.startsWith("url:")) {
            obj = obj.substring(4).trim();
            if (obj.startsWith("/") || obj.startsWith("./") || obj.startsWith("../") || obj == ".") {
                obj = combineUrls(url, obj);
            }
        }
    } else if (Array.isArray(obj)) {
        for (let i = 0; i < obj.length; i++) {
            obj[i] = absolutizePrefixedUrl(i, obj[i], url);
        }
    } else if (obj instanceof Object) {
        for (let subkey in obj) {
            obj[subkey] = absolutizePrefixedUrl(subkey, obj[subkey], url);
        }
    }
    return obj;
}
function relativizePaths(key, obj, path) {
    if (typeof (obj) == "string") {
        if (obj.startsWith("/")) {
            obj = path + obj;
            if (obj.startsWith(document.location.origin)) {
                obj = obj.substring(document.location.origin.length);
            }
        } else if (obj.startsWith("./") || obj.startsWith("../") || obj == ".") {
            obj = combineUrls(path + "/", obj);
            if (obj.startsWith(document.location.origin)) {
                obj = obj.substring(document.location.origin.length);
            }
        }
    } else if (Array.isArray(obj)) {
        for (let i = 0; i < obj.length; i++) {
            obj[i] = relativizePaths(i, obj[i], path);
        }
    } else if (obj instanceof Object) {
        for (let subkey in obj) {
            obj[subkey] = relativizePaths(subkey, obj[subkey], path);
        }
    }
    return obj;
}

// consts
const configUrl = meta("xshell.app_config_url");
const swUrl = meta("xshell.sw_url");
const appUrl = document.location.origin + document.location.pathname;
const appBaseUrl = document.location.origin + meta("xshell.app_base_url");
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
    const rootModuleUrl = new URL(configUrl, document.baseURI).href;    
    const rootModuleConfigTask = loadJsonWithComments(rootModuleUrl);
    
    // wait untils both files are readed
    await Promise.all([xshellConfigTask, rootModuleConfigTask]);
    
    // get xshell config
    const xshellConfig = await xshellConfigTask;
    const assetsPrefix = xshellConfig.xshell.assetsPrefix;
    relativizePaths("", xshellConfig, "/" + assetsPrefix + "/xshell");
    
    // get root module config
    const rootModuleConfig = await rootModuleConfigTask;
    const rootModule = Object.values(rootModuleConfig.modules)[0];
    const rootModuleName = Object.keys(rootModuleConfig.modules)[0];
    rootModule.src = rootModuleUrl;
    absolutizePrefixedUrl("", rootModuleConfig, rootModuleUrl);    
    relativizePaths("", rootModuleConfig, "/" + assetsPrefix + "/modules/" + rootModuleName);
    
    // load referenced modules
    const registered = {}
    registered[rootModuleUrl] = {
        url: rootModuleUrl,
        config: rootModuleConfig
    }
    while (true) {        
        for (let registeredItem of Object.values(registered)) {
            if (registeredItem.config && registeredItem.config.modules) {
                for (let moduleConfig of Object.values(registeredItem.config.modules)) {
                    if (moduleConfig.imports) {
                        for(let importItem of Object.values(moduleConfig.imports)) {
                            const itemUrl = importItem.url;
                            if (!registered[itemUrl]) {
                                registered[itemUrl] = { 
                                    url: itemUrl,
                                    task: loadModuleConfig(itemUrl) 
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
                const registeredModuleName = Object.keys(registeredItem.config.modules)[0];
                registeredModule.src = registeredItem.url;
                absolutizePrefixedUrl("", registeredItem.config, registeredItem.url);    
                relativizePaths("", registeredItem.config, "/" + assetsPrefix + "/modules/" + registeredModuleName);
                delete registeredItem.task;
            }
        }
    }

    // merge configs
    const configs = [xshellConfig, ...Object.values(registered).map(item => item.config)];
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

    // default resolvers for modules
    for(const moduleId in configMerged.modules)   {
        const module = configMerged.modules[moduleId];
        const resolver = configMerged.xshell.resolver;
        resolver.icon = resolver.icon || {};
        resolver.icon[`${moduleId}-{name}`] = resolver.icon[`${moduleId}-{name}`] || { path: `/${assetsPrefix}/${moduleId}/icons/{name}.svg`, loader: 'icon-svg', cache: true, module: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`}
        resolver.layout = resolver.layout || {};
        resolver.layout[`${moduleId}-layout-{name}`] = resolver.layout[`${moduleId}-layout-{name}`] || {path: `/${assetsPrefix}/${moduleId}/layouts/${moduleId}-layout-{name}.svg`, loader: 'component-js', cache: true, module: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.component = resolver.component || {};
        resolver.component[`${moduleId}-{name}`] = resolver.component[`${moduleId}-{name}`] || { path: `/${assetsPrefix}/${moduleId}/components/${moduleId}-{name}.svg`, loader: 'component-js', cache: true, module: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.page = resolver.page || {};
        resolver.page[`/${assetsPrefix}/${moduleId}/{path}.js`] = resolver.page[`/${assetsPrefix}/${moduleId}/{path}.js`] || { path: `/${assetsPrefix}/${moduleId}/{path}.js`, loader: 'page-js', cache: true, module: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.page[`/${assetsPrefix}/${moduleId}/{path}.html`] = resolver.page[`/${assetsPrefix}/${moduleId}/{path}.html`] || { path: `/${assetsPrefix}/${moduleId}/{path}.html`, loader: 'page-html',cache: true,module: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.page[`/${assetsPrefix}/${moduleId}/{path}.md`] = resolver.page[`/${assetsPrefix}/${moduleId}/{path}.md`] || { path: `/${assetsPrefix}/${moduleId}/{path}.md`, loader: 'page-md', cache: true, module: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.module = resolver.module || {};
        resolver.module[`${moduleId}-{name}`] = resolver.module[`${moduleId}-{name}`] || { path: `/${assetsPrefix}/${moduleId}/modules/${moduleId}-{name}.js`, loader: 'module-js', cache: true, module: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
    }

    // console
    console.log("Config:", configMerged);

}

async function installServiceWorker(config) {

    // install service worker
    console.log("bootstrap: installing service worker ...");
    const bootstrapUrlRaw = bootstrapUrl.toString();
    //const realSwUrl = bootstrapUrlRaw.substring(0, bootstrapUrlRaw.lastIndexOf("/")) + "/sw.js";
    const reg = await navigator.serviceWorker.register(swUrl, {
        scope: appBaseUrl + "/"
    });

    // creates rules to send to service worker
    const xshellVersion = config["xshell.version"];
    const assetsPrefix = config["xshell.assetsPrefix"];
    let rules = [];
    rules.push({ src: combineUrls(appUrl, "./" + assetsPrefix + "/xshell"), dst: bootstrapUrlDir, version: xshellVersion, name:"xshell", exceptions:[bootstrapUrlDir + "/xshell.jsonc"]});
    for(var key in config) {
        if (key.startsWith("modules.") && key.endsWith(".src")) {
            const moduleName = key.split(".")[1];
            const moduleSrc = config[key];
            const moduleSrcDir = moduleSrc.substring(0, moduleSrc.lastIndexOf("/"));
            const moduleVersion = config[`modules.${moduleName}.version`];
            rules.push({ src: combineUrls(appUrl, "./" + assetsPrefix + "/" + moduleName), dst: moduleSrcDir, version: moduleVersion, name: moduleName, exceptions: [moduleSrc]});
        }
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
async function loadXShell(config) {
    // load/import XShell
    console.log("bootstrap: loading xshell ...");
    return (await import("xshell")).default;
}
async function bootstrap() {
    
    let config = await loadConfig();
    
    // load xshell config
    //config = await loadXShellConfig(config);
    //config = await loadModulesConfigModern(config);
    
    // load app config
    //config = await loadAppConfig(config);
    // load modules config
    //config = await loadModulesConfig(config);

    // installServiceWorker
    if (!await installServiceWorker(config)){
        return;
    }    
    // create importmap
    let imports = {};
    for (let key in config) {
        if (key.startsWith("resolver.import:")) {
            let importName = key.substring(key.indexOf(":") + 1);
            let importSrc = config[key].split(";")[0].trim();
            imports[importName] = (importSrc.indexOf(":") != -1 ? importSrc : appBaseUrl + importSrc);
        }
    }
    debugger;
    const importMap = document.createElement("script");
    importMap.type = "importmap";
    importMap.textContent = JSON.stringify({ imports }, null, 2);
    document.head.appendChild(importMap);
    // load xshell
    let xshell = await loadXShell(config);
    // init xshell
    await xshell.init(config);
}

// exec bootstrap
bootstrap();

