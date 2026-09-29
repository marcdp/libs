// utils
function stripJsonComments(s) { let o = "", i = 0, n = s.length; for (; i < n;) { let c = s[i]; if (c == '"' || c == "'") { let q = c; o += c; i++; while (i < n) { if (s[i] == "\\") { o += s[i++] + s[i++]; } else if (s[i] == q) { o += s[i++]; break; } else { o += s[i++]; } } continue; } if (c == '/' && s[i + 1] == '/') { i += 2; while (i < n && s[i] != "\n" && s[i] != "\r") i++; continue; } if (c == '/' && s[i + 1] == '*') { i += 2; while (i < n && !(s[i] == '*' && s[i + 1] == '/')) i++; i += 2; continue; } o += c; i++; }; return o; }
function combineUrls(t, n) { if (-1 != t.indexOf("?") && (t = t.substring(0, t.indexOf("?"))), -1 != n.indexOf(":")) return n; if (n.startsWith("/")) { if (-1 != t.indexOf("://")) { let i = t.indexOf("/", t.indexOf("://") + 3); return -1 != i && (t = t.substring(0, i)), t + n } return n } if (n.startsWith("./") || "." == n) return t.endsWith("/") ? t = t.substring(0, t.length - 1) : t.length > 0 && (t = t.substring(0, t.lastIndexOf("/"))), t + n.substring(1); if (n.startsWith("../")) { t.endsWith("/") ? t = t.substring(0, t.length - 1) : t.length > 0 && (t = t.substring(0, t.lastIndexOf("/"))); let i = t + "/" + n; if (i.startsWith("/")) { i = new URL(i, window.location.origin).pathname } else i = new URL(i).toString(); return i } return t.endsWith("/") || -1 != t.indexOf("/") && (t = t.substring(0, t.lastIndexOf("/") + 1)), t + n }
function meta(name) { return document.head.querySelector(`meta[name="${name}"]`)?.content; }
function deepFreeze(obj) {if (obj === null || typeof obj !== "object") {return obj;} Object.freeze(obj); for (const value of Object.values(obj)) {deepFreeze(value);} return obj;}
function absolutizePrefixedUrl(key, obj, url) {return typeof obj === "string" ? (obj.startsWith("url:") ? ((obj = obj.substring(4).trim()), (obj.startsWith("/") || obj.startsWith("./") || obj.startsWith("../") || obj === ".") ? combineUrls(url, obj) : obj) : obj) : Array.isArray(obj) ? (obj.forEach((v, i) => obj[i] = absolutizePrefixedUrl(i, v, url)), obj) : obj instanceof Object ? (Object.keys(obj).forEach(k => obj[k] = absolutizePrefixedUrl(k, obj[k], url)), obj) : obj;}
function relativizePaths(key, obj, path) { return typeof obj === "string" ? (obj.startsWith("/") ? ((obj = path + obj), obj.startsWith(document.location.origin) ? obj.substring(document.location.origin.length) : obj) : (obj.startsWith("./") || obj.startsWith("../") || obj === ".") ? ((obj = combineUrls(path + "/", obj)), obj.startsWith(document.location.origin) ? obj.substring(document.location.origin.length) : obj) : obj) : Array.isArray(obj) ? (obj.forEach((v, i) => obj[i] = relativizePaths(i, v, path)), obj) : obj instanceof Object ? (Object.keys(obj).forEach(k => obj[k] = relativizePaths(k, obj[k], path)), obj) : obj; }
function relativizeModulePaths(config, path) {const definitions = config.xshell?.areas?.definitions || {};const prefixes = Object.fromEntries(Object.entries(definitions).filter(([, area]) => Object.hasOwn(area, "prefix")).map(([id, area]) => [id, area.prefix]));relativizePaths("", config, path);for (const [id, prefix] of Object.entries(prefixes)) definitions[id].prefix = prefix;}
async function loadJsonWithComments(url) {const request = await fetch(url);if (!request.ok) throw new Error(`Failed to json file: ${url}`);let json = await request.text();return JSON.parse(stripJsonComments(json));}
 

// consts
const appConfigPath = meta("xshell:app.configPath");
const appParams = meta("xshell:app.params");
const appBasePath = document.location.origin + meta("xshell:app.basePath");
const xshellEnvironment = meta("xshell:xshell.environment");
const xshellTempUrl = meta("xshell:xshell.temp.url");
const bootstrapUrl = new URL(document.currentScript.src);
const bootstrapUrlDir = bootstrapUrl.href.substring(0, bootstrapUrl.href.lastIndexOf("/") );

// methods
function showSpinner (){
    document.addEventListener("DOMContentLoaded", (event) => {
        const stylesheet = new CSSStyleSheet();
        stylesheet.replaceSync(`
            body {padding:0; margin:0;}
            .spinner {
                display:block;width:100%; 
                background:#cccccc;height:.4em;border-radius:.25em;position:absolute;top: 50%;left: 50%;transform: translate(-50%, -50%);width:12em;
                visibility: hidden;
                animation: spinnerShowDiv 0s forwards;
                animation-delay: 200ms;
            }
            .spinner div {display:block;animation: spinnerProgressBar 2s ease-in-out; animation-delay: 200ms;   animation-fill-mode: both; animation-iteration-count: infinite;background:#006CE0;height:.4em;border-radius:.25em;position:absolute;}
            @keyframes spinnerProgressBar {
                0% { left:0; width: 0; }
                50% { left:0; width: 100%;}
                100% {left:100%; width: 0;}
            } 
            @keyframes spinnerShowDiv {
                to {visibility: visible;}
            }
        `);
        document.adoptedStyleSheets = [...document.adoptedStyleSheets, stylesheet];
        const div = document.createElement("DIV")
        div.innerHTML = "<div></div>";
        div.className = "spinner";
        document.body.appendChild(div);
    });
}
function hideSpinner(){
    const spinner = document.querySelector(".spinner");
    if (spinner) spinner.remove();
}
async function loadModuleConfig(url) {
    // load module config from the given URL
    const request = await fetch(url);
    if (!request.ok) throw new Error(`Failed to json file: ${url}`);
    let json = await request.text();
    return JSON.parse(stripJsonComments(json));
}
function getLocalModule(config, configUrl) {
    // identify the single definition owned by this configuration document
    const modules = config?.modules;
    if (!modules || typeof modules !== "object" || Array.isArray(modules)) {
        throw new Error(`Module configuration '${configUrl}' must contain a modules object with exactly one local module definition.`);
    }
    const entries = Object.entries(modules).filter(([, module]) => module && typeof module === "object" && !Array.isArray(module) && !Object.hasOwn(module, "configUrl"));
    if (entries.length !== 1) {
        const ids = entries.map(([id]) => `'${id}'`);
        const suffix = ids.length ? `: ${ids.join(", ")}.` : ".";
        throw new Error(`Module configuration '${configUrl}' must contain exactly one local module definition, but found ${entries.length}${suffix}`);
    }
    return { id: entries[0][0], definition: entries[0][1] };
}
function resolveModuleConfigUrl(configUrl, ownerConfigUrl, moduleId) {
    // resolve reference URLs against the document that declares them
    if (typeof configUrl !== "string" || !configUrl.trim()) {
        throw new Error(`Module reference '${moduleId}' in '${ownerConfigUrl}' must declare a non-empty configUrl.`);
    }
    const value = configUrl.startsWith("url:") ? configUrl.substring(4).trim() : configUrl;
    return new URL(value, ownerConfigUrl).href;
}
function validateModuleReference(moduleId, reference, ownerConfigUrl) {
    // preserve reference contributions while protecting local asset ownership
    if (!reference || typeof reference !== "object" || Array.isArray(reference)) {
        throw new Error(`Module reference '${moduleId}' in '${ownerConfigUrl}' must be an object that declares configUrl.`);
    }
    if (Object.hasOwn(reference, "assetsUrl")) {
        throw new Error(`Module reference '${moduleId}' in '${ownerConfigUrl}' cannot override assetsUrl; it is owned by the local definition.`);
    }
    return resolveModuleConfigUrl(reference.configUrl, ownerConfigUrl, moduleId);
}
function prepareModuleConfig(config, configUrl, assetsPrefix) {
    // validate identity and references before adding normalized runtime fields
    const localModule = getLocalModule(config, configUrl);
    const references = [];
    for (const [moduleId, reference] of Object.entries(config.modules)) {
        if (moduleId === localModule.id) continue;
        reference.configUrl = validateModuleReference(moduleId, reference, configUrl);
        references.push({ id: moduleId, configUrl: reference.configUrl });
    }
    localModule.definition.configUrl = configUrl;
    localModule.definition.assetsUrl = localModule.definition.assetsUrl || "url:./";
    absolutizePrefixedUrl("", config, configUrl);
    relativizeModulePaths(config, "/" + assetsPrefix + "/" + localModule.id);
    return { id: localModule.id, config, configUrl, references };
}
function getDependencyFirstOrder(rootNode, nodesById) {
    // traverse references in declaration order and reject cycles explicitly
    const order = [];
    const states = new Map();
    const path = [];
    const visit = node => {
        const state = states.get(node.id);
        if (state === "visited") return;
        if (state === "visiting") {
            const cycleStart = path.indexOf(node.id);
            const cycle = [...path.slice(cycleStart), node.id].map(id => `'${id}'`).join(" -> ");
            throw new Error(`Module dependency cycle detected: ${cycle}.`);
        }
        states.set(node.id, "visiting");
        path.push(node.id);
        for (const reference of node.references) {
            visit(nodesById.get(reference.id));
        }
        path.pop();
        states.set(node.id, "visited");
        order.push(node);
    };
    visit(rootNode);
    return order;
}
async function discoverModuleConfigs(rootConfig, rootConfigUrl, loadConfig, assetsPrefix) {
    // discover the graph in deterministic breadth-first passes
    const configs = {};
    const nodesById = new Map();
    const nodesByUrl = new Map();
    const moduleUrls = new Map();
    const registerModuleUrl = (moduleId, configUrl) => {
        const existingUrl = moduleUrls.get(moduleId);
        if (existingUrl && existingUrl !== configUrl) {
            throw new Error(`Module '${moduleId}' is referenced with conflicting configUrl values: '${existingUrl}' and '${configUrl}'.`);
        }
        moduleUrls.set(moduleId, configUrl);
    };
    const registerNode = (node, expectedIds) => {
        for (const expectedId of expectedIds) {
            if (expectedId !== node.id) {
                throw new Error(`Module reference '${expectedId}' points to '${node.configUrl}', but that configuration defines local module '${node.id}'.`);
            }
        }
        registerModuleUrl(node.id, node.configUrl);
        const existingNode = nodesById.get(node.id);
        if (existingNode && existingNode.configUrl !== node.configUrl) {
            throw new Error(`Module '${node.id}' is referenced with conflicting configUrl values: '${existingNode.configUrl}' and '${node.configUrl}'.`);
        }
        nodesById.set(node.id, node);
        nodesByUrl.set(node.configUrl, node);
        configs[node.id] = node.config;
    };

    // register the root before following references back to it
    const rootNode = prepareModuleConfig(rootConfig, rootConfigUrl, assetsPrefix);
    registerNode(rootNode, new Set([rootNode.id]));
    let currentNodes = [rootNode];
    while (currentNodes.length) {
        // collect each unresolved URL once while preserving declaration and discovery order
        const pendingByUrl = new Map();
        for (const node of currentNodes) {
            for (const reference of node.references) {
                registerModuleUrl(reference.id, reference.configUrl);
                const loadedNode = nodesByUrl.get(reference.configUrl);
                if (loadedNode) {
                    if (loadedNode.id !== reference.id) {
                        throw new Error(`Module reference '${reference.id}' points to '${reference.configUrl}', but that configuration defines local module '${loadedNode.id}'.`);
                    }
                    continue;
                }
                const pending = pendingByUrl.get(reference.configUrl) || { expectedIds: new Set() };
                pending.expectedIds.add(reference.id);
                pendingByUrl.set(reference.configUrl, pending);
            }
        }
        if (!pendingByUrl.size) break;
        const urls = [...pendingByUrl.keys()];
        const loadedConfigs = await Promise.all(urls.map(url => loadConfig(url)));
        currentNodes = [];
        for (let i = 0; i < urls.length; i++) {
            const url = urls[i];
            const node = prepareModuleConfig(loadedConfigs[i], url, assetsPrefix);
            registerNode(node, pendingByUrl.get(url).expectedIds);
            currentNodes.push(node);
        }
    }
    return { configs, rootNode, nodesById, mergeOrder: getDependencyFirstOrder(rootNode, nodesById) };
}
function mergeConfigs(configs) {
    // merge arrays and objects while allowing later scalar values to override
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
    return configs.reduce((result, config) => merge(result, config), {});
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
    xshellConfig.xshell.environment = xshellEnvironment || xshellConfig.xshell.environment;
    xshellConfig.xshell.configUrl = xshellConfigUrl || xshellConfig.xshell.configUrl;
    xshellConfig.xshell.temp.url = xshellTempUrl || xshellConfig.xshell.temp.url;
    xshellConfig.xshell.assetsUrl = xshellConfig.xshell.assetsUrl || "url:./";
    absolutizePrefixedUrl("", xshellConfig, xshellConfigUrl);    
    relativizeModulePaths(xshellConfig, "/" + assetsPrefix + "/xshell");
    
    // get root module config
    const rootModuleConfig = await rootModuleConfigTask;
    const rootModule = getLocalModule(rootModuleConfig, rootModuleUrl);
    rootModule.definition.params = Object.fromEntries(new URLSearchParams(appParams));
    xshellConfig.app.params = rootModule.definition.params;

    // load canonical modules and determine dependency-first order
    const graph = await discoverModuleConfigs(rootModuleConfig, rootModuleUrl, loadModuleConfig, assetsPrefix);
    const configs = graph.configs;
    configs["xshell"] = xshellConfig;

    // merge configs
    const configsToMerge = [configs["xshell"], ...graph.mergeOrder.map(node => node.config)];
    const configMerged = mergeConfigs(configsToMerge);

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
        resolver.page[`/${assetsPrefix}/${moduleId}/{path}.js`] = resolver.page[`/${assetsPrefix}/${moduleId}/{path}.js`] || { url: `/${assetsPrefix}/${moduleId}/{path}.js`, loader: 'page-js', cache: true, cacheMode: 'path', moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.page[`/${assetsPrefix}/${moduleId}/{path}.html`] = resolver.page[`/${assetsPrefix}/${moduleId}/{path}.html`] || { url: `/${assetsPrefix}/${moduleId}/{path}.js`, loader: 'page-js', cache: true, cacheMode: 'path', moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.module = resolver.module || {};
        resolver.module[`${moduleId}-{name}`] = resolver.module[`${moduleId}-{name}`] || { url: `/${assetsPrefix}/${moduleId}/${moduleId}-{name}.js`, loader: 'module-js', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.module[`/${assetsPrefix}/${moduleId}/{path}.js`] = resolver.module[`/${assetsPrefix}/${moduleId}/{path}.js`] || { url: `/${assetsPrefix}/${moduleId}/{path}.js`, loader: 'module-js', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.style = resolver.style || {};
        resolver.style[`/${assetsPrefix}/${moduleId}/{path}.css`] = resolver.style[`/${assetsPrefix}/${moduleId}/{path}.css`] || { url: `/${assetsPrefix}/${moduleId}/{path}.css`, loader: 'style-css', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};
        resolver.string = resolver.string || {};
        resolver.string[`/${assetsPrefix}/${moduleId}/{path}`] = resolver.string[`/${assetsPrefix}/${moduleId}/{path}`] || { url: `/${assetsPrefix}/${moduleId}/{path}`, loader: 'string', cache: true, moduleId: moduleId, modulePath: `/${assetsPrefix}/${moduleId}`};    }

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

    // show spinner
    showSpinner();

    // load config
    let config = await loadConfig();
    
    // installServiceWorker
    if (!await installServiceWorker(config)){
        return;
    }    

    // import xshell ES6 module
    console.log("bootstrap: loading xshell ...");
    const xshellUrl = config.xshell.resolver.module.xshell.url;
    const xshellModule = await import(xshellUrl);
    let xshell = xshellModule.default;
    
    // init xshell
    await xshell.init(deepFreeze(config));

    // hide spinner
    hideSpinner();
}

// exec bootstrap
bootstrap();

