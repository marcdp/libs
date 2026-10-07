// utils
function parseJsonc(source) { let stripped = "", inString = false; for (let i = 0; i < source.length; i++) { const char = source[i]; if (inString) { stripped += char; if (char === "\\") stripped += source[++i] ?? ""; else if (char === '"') inString = false; } else if (char === '"') { inString = true; stripped += char; } else if (char === "/" && source[i + 1] === "/") { stripped += "  "; i++; while (i + 1 < source.length && source[i + 1] !== "\n" && source[i + 1] !== "\r") { stripped += " "; i++; } } else if (char === "/" && source[i + 1] === "*") { stripped += "  "; i++; while (i + 1 < source.length && !(source[i + 1] === "*" && source[i + 2] === "/")) { stripped += source[i + 1] === "\n" || source[i + 1] === "\r" ? source[i + 1] : " "; i++; } if (i + 2 >= source.length) throw new SyntaxError("Unterminated JSONC block comment."); stripped += "  "; i += 2; } else stripped += char; } let normalized = ""; inString = false; for (let i = 0; i < stripped.length; i++) { const char = stripped[i]; if (inString) { normalized += char; if (char === "\\") normalized += stripped[++i] ?? ""; else if (char === '"') inString = false; } else if (char === '"') { inString = true; normalized += char; } else if (char === ",") { let next = i + 1; while (next < stripped.length && /\s/.test(stripped[next])) next++; normalized += stripped[next] === "}" || stripped[next] === "]" ? " " : char; } else normalized += char; } return JSON.parse(normalized); }
async function loadJsonWithComments(url) {const request = await fetch(url);if (!request.ok) throw new Error(`Failed to json file: ${url}`);let json = await request.text();return parseJsonc(json);}
function meta(name) { return document.head.querySelector(`meta[name="${name}"]`)?.content; }
function deepFreeze(obj) {if (obj === null || typeof obj !== "object") {return obj;} Object.freeze(obj); for (const value of Object.values(obj)) {deepFreeze(value);} return obj;}
function resolveAppUrl(value) { return new URL(value.substring(4).trim().replace(/^\/+/, ""), appBaseUrl).href; }
function normalizeAssetsBase(value, configUrl) {
    if (typeof value !== "string" || !value.trim()) throw new Error("xshell.assetsBase must be a non-empty URL or path.");
    if (!value.startsWith("app:") && !/^https?:\/\//i.test(value)) {
        throw new Error(`xshell.assetsBase must use app: or an absolute HTTP(S) URL: '${value}'.`);
    }
    const resolved = absolutizePrefixedUrl("assetsBase", value, configUrl);
    const url = new URL(resolved, appBaseUrl);
    const appPath = new URL(appBaseUrl).pathname.replace(/\/+$/, "");
    if (url.origin !== document.location.origin || (appPath && url.pathname !== appPath && !url.pathname.startsWith(appPath + "/")) || url.search || url.hash) {
        throw new Error(`xshell.assetsBase must resolve within the application base URL: '${value}'.`);
    }
    const assetsBase = url.pathname.substring(appPath.length).replace(/\/+$/, "");
    if (!assetsBase) throw new Error("xshell.assetsBase must identify a namespace below the application base URL.");
    return assetsBase;
}
function absolutizePrefixedUrl(key, value, physicalUrl) {
    if (typeof value !== "string") return value;
    if (value.startsWith("app:")) return resolveAppUrl(value);
    if (value.startsWith("url:")) return new URL(value.substring(4).trim(), physicalUrl).href;
    return value;
}
function restoreTemplateBraces(normalized, authored) {
    return authored.includes("{") || authored.includes("}") ? normalized.replace(/%7B/gi, "{").replace(/%7D/gi, "}") : normalized;
}
function relativizePaths(key, value, assetsPath, declaringPath = "/module.jsonc", physicalUrl = "") {
    if (Array.isArray(value)) return value.map(item => relativizePaths(key, item, assetsPath, declaringPath, physicalUrl));
    if (value && typeof value === "object") {
        for (const [name, item] of Object.entries(value)) value[name] = relativizePaths(name, item, assetsPath, declaringPath, physicalUrl);
        return value;
    }
    if (typeof value !== "string") return value;
    if (!["url", "href", "path", "navigation", "icon", "controller", "implementation", "route", "configUrl"].includes(key)) return value;
    const escaped = absolutizePrefixedUrl(key, value, physicalUrl);
    if (escaped !== value) return restoreTemplateBraces(escaped, value);
    if (/^[a-z][a-z0-9+.-]*:/i.test(value)) return value;
    if (!value.includes("/") && value !== "." && value !== "..") return value;

    // normalize under a logical sentinel so traversal is rejected before asset materialization
    const root = "/__xshell_module_root__";
    const logicalUrl = value.startsWith("/") ?
        new URL(root + value, "https://module.invalid") :
        new URL(value, "https://module.invalid" + root + declaringPath);
    if (!logicalUrl.pathname.startsWith(root + "/")) {
        throw new Error(`Module URL '${value}' in '${physicalUrl || declaringPath}' escapes the module root.`);
    }
    // preserve resolver template placeholders after URL.pathname encodes their braces
    return restoreTemplateBraces(assetsPath + logicalUrl.pathname.substring(root.length) + logicalUrl.search + logicalUrl.hash, value);
}
function relativizeModulePaths(config, assetsPath, declaringPath = "/module.jsonc", physicalUrl = "") {
    // normalize only configuration fields that represent authored resource URLs
    if (config.app?.icon) config.app.icon = relativizePaths("icon", config.app.icon, assetsPath, declaringPath, physicalUrl);
    for (const module of Object.values(config.modules || {})) {
        for (const key of ["icon", "controller"]) {
            if (module[key]) module[key] = relativizePaths(key, module[key], assetsPath, declaringPath, physicalUrl);
        }
        for (const key of Object.keys(module.routes || {})) {
            module.routes[key] = relativizePaths("route", module.routes[key], assetsPath, declaringPath, physicalUrl);
        }
        if (module.menus) relativizePaths("menus", module.menus, assetsPath, declaringPath, physicalUrl);
    }
    const xshell = config.xshell;
    if (!xshell) return;
    for (const group of Object.values(xshell.resolver || {})) {
        for (const rule of Object.values(group)) {
            if (typeof rule?.url === "string") rule.url = relativizePaths("url", rule.url, assetsPath, declaringPath, physicalUrl);
        }
    }
    for (const service of Object.values(xshell.services || {})) {
        if (typeof service?.implementation === "string") service.implementation = relativizePaths("implementation", service.implementation, assetsPath, declaringPath, physicalUrl);
    }
    for (const group of Object.values(xshell.ui || {})) {
        for (const [key, value] of Object.entries(group || {})) {
            if (typeof value === "string") group[key] = relativizePaths("url", value, assetsPath, declaringPath, physicalUrl);
        }
    }
    for (const area of Object.values(xshell.areas?.definitions || {})) {
        if (typeof area.icon === "string") area.icon = relativizePaths("icon", area.icon, assetsPath, declaringPath, physicalUrl);
    }
    if (typeof xshell.temp?.url === "string") xshell.temp.url = relativizePaths("url", xshell.temp.url, assetsPath, declaringPath, physicalUrl);
}
 
// consts
const appConfigPath = meta("xshell:app.configPath");
const appParams = meta("xshell:app.params");
const appBasePath = document.location.origin + meta("xshell:app.basePath");
const appBaseUrl = appBasePath.replace(/\/+$/, "") + "/";
const xshellEnvironment = meta("xshell:xshell.environment");
const xshellTempUrl = meta("xshell:xshell.temp.url");
const bootstrapUrl = new URL(document.currentScript.src);
const bootstrapUrlDir = bootstrapUrl.href.substring(0, bootstrapUrl.href.lastIndexOf("/") );

// methods
function showSpinner() {
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
function resolveModuleConfigUrl(configUrl, ownerConfigUrl, ownerAssetsUrl, ownerAssetsPath, ownerDeclaringPath, moduleId) {
    // keep the authored effective URL separate from the source URL used before the Service Worker starts
    if (typeof configUrl !== "string" || !configUrl.trim()) {
        throw new Error(`Module reference '${moduleId}' in '${ownerConfigUrl}' must declare a non-empty configUrl.`);
    }
    const effectiveUrl = relativizePaths("configUrl", configUrl, ownerAssetsPath, ownerDeclaringPath, ownerConfigUrl);
    if (effectiveUrl === configUrl && !/^[a-z][a-z0-9+.-]*:/i.test(configUrl)) {
        throw new Error(`Module reference '${moduleId}' in '${ownerConfigUrl}' has an unsupported configUrl '${configUrl}'. Use /, ./, ../, app:, url:, or an absolute URL.`);
    }
    const loadUrl = effectiveUrl.startsWith(ownerAssetsPath + "/") ? new URL(effectiveUrl.substring(ownerAssetsPath.length + 1), ownerAssetsUrl).href : effectiveUrl;
    return { configUrl: effectiveUrl, loadUrl };
}
function validateModuleReference(moduleId, reference, ownerConfigUrl, ownerAssetsUrl, ownerAssetsPath, ownerDeclaringPath) {
    // preserve reference contributions while protecting local asset ownership
    if (!reference || typeof reference !== "object" || Array.isArray(reference)) {
        throw new Error(`Module reference '${moduleId}' in '${ownerConfigUrl}' must be an object that declares configUrl.`);
    }
    if (Object.hasOwn(reference, "assetsUrl")) {
        throw new Error(`Module reference '${moduleId}' in '${ownerConfigUrl}' cannot override assetsUrl; it is owned by the local definition.`);
    }
    return resolveModuleConfigUrl(reference.configUrl, ownerConfigUrl, ownerAssetsUrl, ownerAssetsPath, ownerDeclaringPath, moduleId);
}
function prepareModuleConfig(config, configUrl, assetsBase) {
    // validate identity and references before adding normalized runtime fields
    const localModule = getLocalModule(config, configUrl);
    const assetsPath = assetsBase + "/" + localModule.id;
    localModule.definition.assetsUrl = relativizePaths("url", localModule.definition.assetsUrl || "url:./", assetsPath, "/module.jsonc", configUrl);
    const source = new URL(configUrl);
    const assets = new URL(localModule.definition.assetsUrl, appBaseUrl);
    const assetsDirectory = assets.pathname.endsWith("/") ? assets.pathname : assets.pathname + "/";
    const declaringPath = source.origin === assets.origin && source.pathname.startsWith(assetsDirectory) ?
        "/" + source.pathname.substring(assetsDirectory.length) : "/" + source.pathname.substring(source.pathname.lastIndexOf("/") + 1);
    const references = [];
    for (const [moduleId, reference] of Object.entries(config.modules)) {
        if (moduleId === localModule.id) continue;
        const resolved = validateModuleReference(moduleId, reference, configUrl, localModule.definition.assetsUrl, assetsPath, declaringPath);
        reference.configUrl = resolved.configUrl;
        references.push({ id: moduleId, ...resolved });
    }
    localModule.definition.configUrl = configUrl;
    relativizeModulePaths(config, assetsPath, declaringPath, configUrl);
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
async function discoverModuleConfigs(rootConfig, rootConfigUrl, loadConfig, assetsBase) {
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
    const rootNode = prepareModuleConfig(rootConfig, rootConfigUrl, assetsBase);
    registerNode(rootNode, new Set([rootNode.id]));
    let currentNodes = [rootNode];
    while (currentNodes.length) {
        // collect each unresolved URL once while preserving declaration and discovery order
        const pendingByUrl = new Map();
        for (const node of currentNodes) {
            for (const reference of node.references) {
                registerModuleUrl(reference.id, reference.loadUrl);
                const loadedNode = nodesByUrl.get(reference.loadUrl);
                if (loadedNode) {
                    if (loadedNode.id !== reference.id) {
                        throw new Error(`Module reference '${reference.id}' points to '${reference.loadUrl}', but that configuration defines local module '${loadedNode.id}'.`);
                    }
                    continue;
                }
                const pending = pendingByUrl.get(reference.loadUrl) || { expectedIds: new Set() };
                pending.expectedIds.add(reference.id);
                pendingByUrl.set(reference.loadUrl, pending);
            }
        }
        if (!pendingByUrl.size) break;
        const urls = [...pendingByUrl.keys()];
        const loadedConfigs = await Promise.all(urls.map(url => loadConfig(url)));
        currentNodes = [];
        for (let i = 0; i < urls.length; i++) {
            const url = urls[i];
            const node = prepareModuleConfig(loadedConfigs[i], url, assetsBase);
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

// main functions
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
    const rootModuleConfig = await rootModuleConfigTask;
    const assetsBaseValue = rootModuleConfig.xshell?.assetsBase ?? xshellConfig.xshell.assetsBase;
    const assetsBaseUrl = rootModuleConfig.xshell?.assetsBase === undefined ? xshellConfigUrl : rootModuleUrl;
    const assetsBase = normalizeAssetsBase(assetsBaseValue, assetsBaseUrl);
    xshellConfig.app.basePath = appBasePath;
    xshellConfig.xshell.environment = xshellEnvironment || xshellConfig.xshell.environment;
    xshellConfig.xshell.configUrl = xshellConfigUrl || xshellConfig.xshell.configUrl;
    xshellConfig.xshell.temp.url = new URL(xshellTempUrl, document.baseURI).href;
    xshellConfig.xshell.assetsUrl = xshellConfig.xshell.assetsUrl || "url:./";
    xshellConfig.xshell.assetsUrl = relativizePaths("url", xshellConfig.xshell.assetsUrl, assetsBase + "/xshell", "/xshell.jsonc", xshellConfigUrl);
    xshellConfig.xshell.assetsBase = assetsBase;
    relativizeModulePaths(xshellConfig, assetsBase + "/xshell", "/xshell.jsonc", xshellConfigUrl);
    
    // get root module config
    const rootModule = getLocalModule(rootModuleConfig, rootModuleUrl);
    rootModule.definition.params = Object.fromEntries(new URLSearchParams(appParams));
    xshellConfig.app.params = rootModule.definition.params;

    // load canonical modules and determine dependency-first order
    const graph = await discoverModuleConfigs(rootModuleConfig, rootModuleUrl, loadJsonWithComments, assetsBase);
    const configs = graph.configs;
    configs["xshell"] = xshellConfig;

    // merge configs
    const configsToMerge = [configs["xshell"], ...graph.mergeOrder.map(node => node.config)];
    const configMerged = mergeConfigs(configsToMerge);
    configMerged.xshell.assetsBase = assetsBase;
    configMerged.xshell.assetsPath = assetsBase + "/xshell";

    // add generated runtime metadata and the default contract for modules
    for(const moduleId in configMerged.modules)   {
        const module = configMerged.modules[moduleId];
        module.assetsPath = assetsBase + "/" + moduleId;
        if (!module.contract) module.contract = {};
        if (!module.contract.events) module.contract.events = {};
        if (!module.contract.intents) module.contract.intents = {};
        if (!module.contract.actions) module.contract.actions = {};
    }
    
    // log
    console.log("bootstrap: config:", configMerged);

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
    let rules = [];
    rules.push({ 
        src: new URL(config.xshell.assetsPath.substring(1), appBaseUrl).href,
        dst: config.xshell.assetsUrl, 
        version: xshellVersion, 
        name:"xshell", 
        exceptions:[config.xshell.configUrl]});
    for(var moduleId of Object.keys(config.modules)) {
        const module = config.modules[moduleId];
        rules.push({ 
            src: new URL(module.assetsPath.substring(1), appBaseUrl).href,
            dst: module.assetsUrl, 
            version: module.version, 
            name: moduleId, 
            exceptions: [module.configUrl]
        });
    }
    console.log("bootstrap: sw rules: ", rules);

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

async function loadFilesIndexes(config) {
    // load every physical inventory through the Service Worker and expose virtual runtime paths
    console.log("bootstrap: loading file indexes ...");
    const tasks = [];
    const loadFilesIndex = async (id, target) => {
        // load files index
        const moduleFilesUrl = new URL(`${target.assetsPath.substring(1)}/module.files.json`, appBaseUrl).href;
        const response = await fetch(moduleFilesUrl);
        if (!response.ok) {
            throw new Error(`Failed to load file inventory for '${id}' from '${moduleFilesUrl}': ${response.status} ${response.statusText}`);
        }
        const files = await response.json();
        const virtualRoot = target.assetsPath;
        for (const file of files) {
            file.path = relativizePaths("path", file.path, virtualRoot);
        }
        target.files = files;
    };
    for (const moduleId of Object.keys(config.modules)) {
        const module = config.modules[moduleId];
        if (!module.files){
            tasks.push(loadFilesIndex(moduleId, module));
        }
    }
    if (!config.xshell.files) {
        tasks.push(loadFilesIndex("xshell", config.xshell));
    }
    // wait
    await Promise.all(tasks);
    // return
    return config;
}

function fillResolverRules(config) {
    
    // default resolvers for modules
    const resolver = config.xshell.resolver;
    const contractDeclarations = new Map();
    for(const moduleId in config.modules)   {
        const module = config.modules[moduleId];
        const moduleAssetsPath = module.assetsPath;  
        const moduleAssetsPathContracts = moduleAssetsPath + "/contracts";
        // icon resolvers
        resolver.icon = resolver.icon || {};
        resolver.icon[`${moduleId}`] = resolver.icon[`${moduleId}`] || { url: `${moduleAssetsPath}/icons/${moduleId}.svg`, loader: 'icon-svg', cache: true, moduleId: moduleId, modulePath: moduleAssetsPath}
        resolver.icon[`${moduleId}-{name}`] = resolver.icon[`${moduleId}-{name}`] || { url: `${moduleAssetsPath}/icons/${moduleId}-{name}.svg`, loader: 'icon-svg', cache: true, moduleId: moduleId, modulePath: moduleAssetsPath}
        // layout resolvers
        resolver.layout = resolver.layout || {};
        resolver.layout[`${moduleId}-layout-{name}`] = resolver.layout[`${moduleId}-layout-{name}`] || {url: `${moduleAssetsPath}/layouts/${moduleId}-layout-{name}.js`, loader: 'component-js', cache: true, moduleId: moduleId, modulePath: moduleAssetsPath};
        // component resolvers
        resolver.component = resolver.component || {};
        resolver.component[`${moduleId}-{name}`] = resolver.component[`${moduleId}-{name}`] || { url: `${moduleAssetsPath}/components/${moduleId}-{name}.js`, loader: 'component-js', cache: true, moduleId: moduleId, modulePath: moduleAssetsPath};
        // page resolvers
        resolver.page = resolver.page || {};
        resolver.page[`${moduleAssetsPath}/{path}.js`] = resolver.page[`${moduleAssetsPath}/{path}.js`] || { url: `${moduleAssetsPath}/{path}.js`, loader: 'page-js', cache: true, cacheMode: 'path', moduleId: moduleId, modulePath: moduleAssetsPath};
        resolver.page[`${moduleAssetsPath}/{path}.html`] = resolver.page[`${moduleAssetsPath}/{path}.html`] || { url: `${moduleAssetsPath}/{path}.js`, loader: 'page-js', cache: true, cacheMode: 'path', moduleId: moduleId, modulePath: moduleAssetsPath};
        resolver.page[`${moduleAssetsPath}/{path}.md`] = resolver.page[`${moduleAssetsPath}/{path}.md`] || { url: `${moduleAssetsPath}/{path}.md`, loader: 'page-md', cache: true, cacheMode: 'path', moduleId: moduleId, modulePath: moduleAssetsPath};
        // module resolvers
        resolver.module = resolver.module || {};
        resolver.module[`${moduleId}-{name}`] = resolver.module[`${moduleId}-{name}`] || { url: `${moduleAssetsPath}/${moduleId}-{name}.js`, loader: 'module-js', cache: true, moduleId: moduleId, modulePath: moduleAssetsPath};
        resolver.module[`${moduleAssetsPath}/{path}.js`] = resolver.module[`${moduleAssetsPath}/{path}.js`] || { url: `${moduleAssetsPath}/{path}.js`, loader: 'module-js', cache: true, moduleId: moduleId, modulePath: moduleAssetsPath};
        // style resolvers
        resolver.style = resolver.style || {};
        resolver.style[`${moduleAssetsPath}/{path}.css`] = resolver.style[`${moduleAssetsPath}/{path}.css`] || { url: `${moduleAssetsPath}/{path}.css`, loader: 'style-css', cache: true, moduleId: moduleId, modulePath: moduleAssetsPath};
        // string resolvers
        resolver.string = resolver.string || {};
        resolver.string[`${moduleAssetsPath}/{path}`] = resolver.string[`${moduleAssetsPath}/{path}`] || { url: `${moduleAssetsPath}/{path}`, loader: 'string', cache: true, moduleId: moduleId, modulePath: moduleAssetsPath};
        // contract resolvers
        resolver.contract = resolver.contract || {};
        for(const file of module.files) {
            if (file.path.startsWith(moduleAssetsPathContracts + "/") && file.path.endsWith(".json")) {
                const filenameWithoutExtension = file.path.substring(moduleAssetsPathContracts.length + 1).replace(/\.json$/, '');
                const first = contractDeclarations.get(filenameWithoutExtension);
                if (first) {
                    throw new Error(`Duplicate contract '${filenameWithoutExtension}' declared by module '${first.moduleId}' in '${first.path}' and module '${moduleId}' in '${file.path}'.`);
                }
                if (Object.hasOwn(resolver.contract, filenameWithoutExtension)) {
                    throw new Error(`Duplicate contract '${filenameWithoutExtension}' conflicts with an existing resolver entry while processing module '${moduleId}' file '${file.path}'.`);
                }
                contractDeclarations.set(filenameWithoutExtension, { moduleId, path: file.path });
                resolver.contract[`${filenameWithoutExtension}`] = { url: `${moduleAssetsPathContracts}/${filenameWithoutExtension}.json`, loader: 'object-json', cache: true, moduleId: moduleId, modulePath: moduleAssetsPath};
            }
        }
    }
    
    // return    
    return config;
}
async function initializeXShell(config, loadXShellModule = url => import(url)) {
    // import and validate the complete effective configuration before freezing and initialization
    console.log("bootstrap: loading xshell ...");
    const authoredUrl = config.xshell.resolver.module.xshell.url;
    const xshellUrl = new URL(authoredUrl.startsWith("/") ? authoredUrl.substring(1) : authoredUrl, appBaseUrl).href;
    const xshellModule = await loadXShellModule(xshellUrl);
    const xshell = xshellModule.default;
    await xshell.validateConfig(config);

    // initialize XShell with immutable effective configuration
    console.log("bootstrap: initializing xshell ...");
    await xshell.init(deepFreeze(config));
}

// bootstrap
async function bootstrap() {
    // show spinner
    showSpinner();
    // load config
    let config = await loadConfig();
    // installServiceWorker
    if (!await installServiceWorker(config)){
        return;
    }    
    // load files indexes
    config = await loadFilesIndexes(config);
    // fill resolver rules
    config = fillResolverRules(config);
    // import, validate, freeze, and initialize XShell
    await initializeXShell(config);
    // hide spinner
    hideSpinner();
}

// exec bootstrap
bootstrap();


