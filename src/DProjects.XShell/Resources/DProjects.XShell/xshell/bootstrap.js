// utils
function parseJsonc(source){let stripped="",inString=false;for(let i=0;i<source.length;i++){const char=source[i];if(inString){stripped+=char;if(char==="\\")stripped+=source[++i]??"";else if(char==='"')inString=false;}else if(char==='"'){inString=true;stripped+=char;}else if(char==="/"&&source[i+1]==="/"){stripped+="  ";i++;while(i+1<source.length&&source[i+1]!=="\n"&&source[i+1]!=="\r"){stripped+=" ";i++;}}else if(char==="/"&&source[i+1]==="*"){stripped+="  ";i++;while(i+1<source.length&&!(source[i+1]==="*"&&source[i+2]==="/")){stripped+=source[i+1]==="\n"||source[i+1]==="\r"?source[i+1]:" ";i++;}if(i+2>=source.length)throw new SyntaxError("Unterminated JSONC block comment.");stripped+="  ";i+=2;}else stripped+=char;}let normalized="";inString=false;for(let i=0;i<stripped.length;i++){const char=stripped[i];if(inString){normalized+=char;if(char==="\\")normalized+=stripped[++i]??"";else if(char==='"')inString=false;}else if(char==='"'){inString=true;normalized+=char;}else if(char===","){let next=i+1;while(next<stripped.length&&/\s/.test(stripped[next]))next++;normalized+=stripped[next]==="}"||stripped[next]==="]"?" ":char;}else normalized+=char;}return JSON.parse(normalized);}
async function loadJsonWithComments(url){const response=await fetch(url);if(!response.ok)throw new Error(`Failed to load json file: ${url}`);return parseJsonc(await response.text());}
function meta(name){return document.head.querySelector(`meta[name="${name}"]`)?.content;}
function deepFreeze(obj){if(obj===null||typeof obj!=="object")return obj;Object.freeze(obj);for(const value of Object.values(obj))deepFreeze(value);return obj;}
function resolveAppUrl(value){const root="/__xshell_app_root__/";const logicalUrl=new URL(value.substring(4).trim().replace(/^\/+/,""),"https://app.invalid"+root);if(logicalUrl.origin!=="https://app.invalid"||!logicalUrl.pathname.startsWith(root))throw new Error(`Application URL '${value}' escapes the application root.`);return new URL(logicalUrl.pathname.substring(root.length)+logicalUrl.search+logicalUrl.hash,appBaseUrl).href;}
function normalizeAssetsBasePath(value){if(typeof value!=="string"||!value.trim())throw new Error("xshell.assetsBasePath must be a non-empty URL or path.");let resolved;if(value.startsWith("app:"))resolved=resolveAppUrl(value);else if(/^https?:\/\//i.test(value))resolved=value;else throw new Error(`xshell.assetsBasePath must use app: or an absolute HTTP(S) URL: '${value}'.`);const url=new URL(resolved),appPath=new URL(appBaseUrl).pathname.replace(/\/+$/,"");if(url.origin!==document.location.origin||(appPath&&url.pathname!==appPath&&!url.pathname.startsWith(appPath+"/"))||url.search||url.hash)throw new Error(`xshell.assetsBasePath must resolve within the application base URL: '${value}'.`);const assetsBasePath=url.pathname.substring(appPath.length).replace(/\/+$/,"");if(!assetsBasePath)throw new Error("xshell.assetsBasePath must identify a namespace below the application base URL.");return assetsBasePath;}
function getAssetsPath(assetsBasePath, moduleId, version, hash, environment) {
    if (typeof version !== "string" || !version.trim()) throw new Error(`Module '${moduleId}' requires a non-empty version for its asset generation.`);
    let generation;
    if (String(environment).toLowerCase() === "development") {
        generation = `${version}.dev`;
    } else {
        if (typeof hash !== "string" || !hash.trim()) throw new Error(`Published module '${moduleId}' requires a non-empty package hash.`);
        generation = `${version}.${hash}`;
    }
    return `${assetsBasePath}/${moduleId}/${generation}`;
}
function relativizePaths(value,assetsPath,declaringPath="/module.jsonc",physicalUrl=""){if(Array.isArray(value))return value.map(item=>relativizePaths(item,assetsPath,declaringPath,physicalUrl));if(value&&typeof value==="object"){for(const[name,item]of Object.entries(value))value[name]=relativizePaths(item,assetsPath,declaringPath,physicalUrl);return value;}if(typeof value!=="string")return value;const restoreBraces=normalized=>value.includes("{")||value.includes("}")?normalized.replace(/%7B/gi,"{").replace(/%7D/gi,"}"):normalized;if(value.startsWith("app:"))return restoreBraces(resolveAppUrl(value));if(value.startsWith("url:"))return restoreBraces(new URL(value.substring(4).trim(),physicalUrl).href);if(/^[a-z][a-z0-9+.-]*:/i.test(value))return value;if(!value.startsWith("/")&&!value.startsWith("./")&&!value.startsWith("../"))return value;const root="/__xshell_module_root__",logicalUrl=value.startsWith("/")?new URL(root+value,"https://module.invalid"):new URL(value,"https://module.invalid"+root+declaringPath);if(!logicalUrl.pathname.startsWith(root+"/"))throw new Error(`Module URL '${value}' in '${physicalUrl||declaringPath}' escapes the module root.`);return restoreBraces(assetsPath+logicalUrl.pathname.substring(root.length)+logicalUrl.search+logicalUrl.hash);}


// consts
const appConfigPath = meta("xshell:app.configPath");
const appParams = meta("xshell:app.params");
const appBasePath = meta("xshell:app.basePath");
const appBaseUrl = document.location.origin + appBasePath.replace(/\/+$/, "") + "/";
const xshellEnvironment = meta("xshell:xshell.environment");
const xshellTempUrl = meta("xshell:xshell.temp.url");
const bootstrapUrl = new URL(document.currentScript.src);
const bootstrapUrlDir = bootstrapUrl.href.substring(0, bootstrapUrl.href.lastIndexOf("/"));
const moduleIdPattern = /^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$/;
const reservedModuleId = "xshell";


// spinner functions
function showSpinner() {
    document.addEventListener("DOMContentLoaded", () => {
        const stylesheet = new CSSStyleSheet();
        stylesheet.replaceSync(`
            body {padding:0; margin:0;}
            .spinner {
                display:block;width:100%;
                background:#cccccc;height:.4em;border-radius:.25em;position:absolute;top:50%;left:50%;
                transform:translate(-50%, -50%);width:12em;visibility:hidden;
                animation:spinnerShowDiv 0s forwards;animation-delay:200ms;
            }
            .spinner div {
                display:block;animation:spinnerProgressBar 2s ease-in-out;animation-delay:200ms;
                animation-fill-mode:both;animation-iteration-count:infinite;
                background:#006CE0;height:.4em;border-radius:.25em;position:absolute;
            }
            @keyframes spinnerProgressBar { 0% {left:0;width:0;} 50% {left:0;width:100%;} 100% {left:100%;width:0;} }
            @keyframes spinnerShowDiv { to {visibility:visible;} }
        `);

        document.adoptedStyleSheets = [...document.adoptedStyleSheets, stylesheet];

        const div = document.createElement("DIV");
        div.innerHTML = "<div></div>";
        div.className = "spinner";
        document.body.appendChild(div);
    });
}
function hideSpinner() {
    document.querySelector(".spinner")?.remove();
}
function showBootstrapError(error) {
    const render = () => {
        hideSpinner();
        console.error("XShell bootstrap failed:", error);
        const container = document.createElement("main");
        const title = document.createElement("h1");
        const message = document.createElement("pre");
        title.textContent = "Application failed to start";
        message.textContent = error?.message || String(error);
        container.append(title, message);
        document.body.replaceChildren(container);
    };
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", render, { once: true });
    } else {
        render();
    }
}

// module functions
function validateModuleId(moduleId, configUrl) {
    if (!moduleIdPattern.test(moduleId)) {
        throw new Error(`Invalid module id '${moduleId}' in module configuration '${configUrl}'.`);
    }

    if (moduleId === reservedModuleId) {
        throw new Error(`Module id '${moduleId}' is reserved in module configuration '${configUrl}'.`);
    }
}
function getLocalModule(config, configUrl) {
    const modules = config?.modules;

    if (!modules || typeof modules !== "object" || Array.isArray(modules)) {
        throw new Error(`Module configuration '${configUrl}' must contain a modules object with exactly one local module definition.`);
    }

    for (const moduleId of Object.keys(modules)) {
        validateModuleId(moduleId, configUrl);
    }

    const entries = Object.entries(modules).filter(([, module]) =>
        module &&
        typeof module === "object" &&
        !Array.isArray(module) &&
        !Object.hasOwn(module, "configUrl")
    );

    if (entries.length !== 1) {
        const ids = entries.map(([id]) => `'${id}'`);
        throw new Error(
            `Module configuration '${configUrl}' must contain exactly one local module definition, ` +
            `but found ${entries.length}${ids.length ? `: ${ids.join(", ")}.` : "."}`
        );
    }

    return { id: entries[0][0], definition: entries[0][1] };
}

function prepareModuleConfig(config, configUrl, assetsBasePath, environment) {
    const localModule = getLocalModule(config, configUrl);
    const assetsPath = getAssetsPath(assetsBasePath, localModule.id, localModule.definition.version, localModule.definition.hash, environment);

    let assetsUrl = relativizePaths(localModule.definition.assetsUrl || "url:./", assetsPath, "/module.jsonc", configUrl);

    if (!/^[a-z][a-z0-9+.-]*:/i.test(assetsUrl)) {
        throw new Error(`assetsUrl must resolve to an absolute HTTP(S) URL: '${assetsUrl}'.`);
    }

    const assetsUrlObject = new URL(assetsUrl);

    if (assetsUrlObject.protocol !== "http:" && assetsUrlObject.protocol !== "https:") {
        throw new Error(`assetsUrl must resolve to an absolute HTTP(S) URL: '${assetsUrl}'.`);
    }

    localModule.definition.assetsUrl = assetsUrlObject.href;

    const source = new URL(configUrl);
    const assetsDirectory = assetsUrlObject.pathname.endsWith("/") ? assetsUrlObject.pathname : assetsUrlObject.pathname + "/";

    const declaringPath = source.origin === assetsUrlObject.origin && source.pathname.startsWith(assetsDirectory)
        ? "/" + source.pathname.substring(assetsDirectory.length)
        : "/" + source.pathname.substring(source.pathname.lastIndexOf("/") + 1);

    const references = [];

    for (const [moduleId, reference] of Object.entries(config.modules)) {
        if (moduleId === localModule.id) continue;

        if (!reference || typeof reference !== "object" || Array.isArray(reference)) {
            throw new Error(`Module reference '${moduleId}' in '${configUrl}' must be an object that declares configUrl.`);
        }

        if (Object.hasOwn(reference, "assetsUrl")) {
            throw new Error(`Module reference '${moduleId}' in '${configUrl}' cannot override assetsUrl; it is owned by the local definition.`);
        }

        const authoredConfigUrl = reference.configUrl;

        if (typeof authoredConfigUrl !== "string" || !authoredConfigUrl.trim()) {
            throw new Error(`Module reference '${moduleId}' in '${configUrl}' must declare a non-empty configUrl.`);
        }

        const effectiveUrl = relativizePaths(authoredConfigUrl, assetsPath, declaringPath, configUrl);

        if (effectiveUrl === authoredConfigUrl && !/^[a-z][a-z0-9+.-]*:/i.test(authoredConfigUrl)) {
            throw new Error(
                `Module reference '${moduleId}' in '${configUrl}' has an unsupported configUrl '${authoredConfigUrl}'. ` +
                "Use /, ./, ../, app:, url:, or an absolute URL."
            );
        }

        const loadUrl = effectiveUrl.startsWith(assetsPath + "/")
            ? new URL(effectiveUrl.substring(assetsPath.length + 1), localModule.definition.assetsUrl).href
            : effectiveUrl;

        delete reference.configUrl;
        references.push({ id: moduleId, configUrl: effectiveUrl, loadUrl });
    }

    localModule.definition.configUrl = configUrl;

    relativizePaths(config, assetsPath, declaringPath, configUrl);

    return { id: localModule.id, config, configUrl, assetsPath, references };
}

async function discoverModuleConfigs(rootConfig, rootConfigUrl, loadConfig, assetsBasePath, environment) {
    const nodesById = new Map();
    const nodesByUrl = new Map();

    const registerNode = (node, expectedIds) => {
        for (const expectedId of expectedIds) {
            if (expectedId !== node.id) {
                throw new Error(`Module reference '${expectedId}' points to '${node.configUrl}', but that configuration defines local module '${node.id}'.`);
            }
        }

        const existing = nodesById.get(node.id);

        if (existing && existing.configUrl !== node.configUrl) {
            throw new Error(`Module '${node.id}' is referenced with conflicting configUrl values: '${existing.configUrl}' and '${node.configUrl}'.`);
        }

        nodesById.set(node.id, node);
        nodesByUrl.set(node.configUrl, node);
    };

    const rootNode = prepareModuleConfig(rootConfig, rootConfigUrl, assetsBasePath, environment);

    registerNode(rootNode, new Set([rootNode.id]));

    let currentNodes = [rootNode];

    while (currentNodes.length) {
        const pendingByUrl = new Map();

        for (const node of currentNodes) {
            for (const reference of node.references) {
                const existing = nodesById.get(reference.id);

                if (existing && existing.configUrl !== reference.loadUrl) {
                    throw new Error(`Module '${reference.id}' is referenced with conflicting configUrl values: '${existing.configUrl}' and '${reference.loadUrl}'.`);
                }

                const loadedNode = nodesByUrl.get(reference.loadUrl);

                if (loadedNode) {
                    if (loadedNode.id !== reference.id) {
                        throw new Error(
                            `Module reference '${reference.id}' points to '${reference.loadUrl}', ` +
                            `but that configuration defines local module '${loadedNode.id}'.`
                        );
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
            const node = prepareModuleConfig(loadedConfigs[i], url, assetsBasePath, environment);

            registerNode(node, pendingByUrl.get(url).expectedIds);
            currentNodes.push(node);
        }
    }

    const mergeOrder = [];
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
        mergeOrder.push(node);
    };

    visit(rootNode);

    return { rootNode, nodesById, mergeOrder };
}

function mergeConfigs(configs) {
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
    console.log("bootstrap: loading config ...");

    const xshellConfigUrl = bootstrapUrlDir + "/xshell.json";
    const rootModuleUrl = new URL(appConfigPath, document.baseURI).href;

    const [xshellConfig, rootModuleConfig] = await Promise.all([
        loadJsonWithComments(xshellConfigUrl),
        loadJsonWithComments(rootModuleUrl)
    ]);

    const assetsBasePath = normalizeAssetsBasePath(
        rootModuleConfig.xshell?.assetsBasePath ?? xshellConfig.xshell.assetsBasePath
    );

    const environment = xshellEnvironment || rootModuleConfig.xshell?.environment || xshellConfig.xshell.environment;
    const xshellAssetsPath = getAssetsPath(assetsBasePath, "xshell", xshellConfig.xshell.version, xshellConfig.xshell.hash, environment);

    let xshellAssetsUrl = relativizePaths(
        xshellConfig.xshell.assetsUrl || "url:./",
        xshellAssetsPath,
        "/xshell.json",
        xshellConfigUrl
    );

    if (!/^[a-z][a-z0-9+.-]*:/i.test(xshellAssetsUrl)) {
        throw new Error(`xshell.assetsUrl must resolve to an absolute HTTP(S) URL: '${xshellAssetsUrl}'.`);
    }

    const xshellAssetsUrlObject = new URL(xshellAssetsUrl);

    if (xshellAssetsUrlObject.protocol !== "http:" && xshellAssetsUrlObject.protocol !== "https:") {
        throw new Error(`xshell.assetsUrl must resolve to an absolute HTTP(S) URL: '${xshellAssetsUrl}'.`);
    }

    xshellConfig.xshell.assetsUrl = xshellAssetsUrlObject.href;

    relativizePaths(xshellConfig, xshellAssetsPath, "/xshell.json", xshellConfigUrl);

    // Host/runtime values are added after normalization so they are not reinterpreted as authored configuration.
    xshellConfig.app.basePath = appBasePath;
    xshellConfig.app.baseUrl = appBaseUrl;
    xshellConfig.xshell.environment = environment;
    xshellConfig.xshell.configUrl = xshellConfigUrl;
    xshellConfig.xshell.temp.url = new URL(xshellTempUrl, document.baseURI).href;
    xshellConfig.xshell.assetsBasePath = assetsBasePath;

    const rootModule = getLocalModule(rootModuleConfig, rootModuleUrl);
    rootModule.definition.params = Object.fromEntries(new URLSearchParams(appParams));
    xshellConfig.app.params = rootModule.definition.params;

    const graph = await discoverModuleConfigs(rootModuleConfig, rootModuleUrl, loadJsonWithComments, assetsBasePath, environment);
    const config = mergeConfigs([xshellConfig, ...graph.mergeOrder.map(node => node.config)]);

    if (!/^[a-z][a-z0-9+.-]*:/i.test(config.xshell.assetsUrl)) {
        throw new Error(`xshell.assetsUrl must resolve to an absolute HTTP(S) URL: '${config.xshell.assetsUrl}'.`);
    }

    const finalAssetsUrl = new URL(config.xshell.assetsUrl);

    if (finalAssetsUrl.protocol !== "http:" && finalAssetsUrl.protocol !== "https:") {
        throw new Error(`xshell.assetsUrl must resolve to an absolute HTTP(S) URL: '${config.xshell.assetsUrl}'.`);
    }

    config.xshell.assetsUrl = finalAssetsUrl.href;
    config.xshell.environment = environment;
    config.xshell.assetsBasePath = assetsBasePath;
    config.xshell.assetsPath = xshellAssetsPath;

    for (const [moduleId, module] of Object.entries(config.modules)) {
        module.assetsPath = graph.nodesById.get(moduleId).assetsPath;

        module.contract ??= {};
        module.contract.events ??= {};
    }

    console.log("bootstrap: config:", config);

    return config;
}

// service worker functions
async function installServiceWorker(config) {
    console.log("bootstrap: installing service worker ...");

    const reg = await navigator.serviceWorker.register(appBasePath + "/sw.js", {
        scope: appBasePath + "/"
    });

    const rules = [{
        src: new URL(config.xshell.assetsPath.substring(1), appBaseUrl).href,
        dst: config.xshell.assetsUrl,
        version: config.xshell.version,
        name: "xshell",
        exceptions: [config.xshell.configUrl]
    }];

    for (const [moduleId, module] of Object.entries(config.modules)) {
        rules.push({
            src: new URL(module.assetsPath.substring(1), appBaseUrl).href,
            dst: module.assetsUrl,
            version: module.version,
            name: moduleId,
            exceptions: [module.configUrl]
        });
    }

    console.log("bootstrap: sw rules: ", rules);
    console.log("bootstrap: waiting for ready ...");

    await navigator.serviceWorker.ready;

    if (!navigator.serviceWorker.controller) {
        console.log("bootstrap: page is not controlled ... forcing reload");
        location.reload();
        return false;
    }

    console.log("bootstrap: send init message to service worker ...");

    await new Promise((resolve, reject) => {
        const channel = new MessageChannel();

        channel.port1.onmessage = event => {
            resolve(event.data);
            channel.port1.close();
        };

        setTimeout(() => {
            reject(new Error("Service Worker did not reply in time"));
            channel.port1.close();
        }, 5000);

        reg.active.postMessage({ type: "init", payload: { rules } }, [channel.port2]);
    });

    console.log("bootstrap: service worker ready to receive requests");

    return true;
}

// file index functions
async function loadFilesIndexes(config) {
    console.log("bootstrap: loading file indexes ...");

    const tasks = [];

    const loadFilesIndex = async (id, target) => {
        const moduleFilesUrl = new URL(`${target.assetsPath.substring(1)}/module.files.json`, appBaseUrl).href;
        const response = await fetch(moduleFilesUrl);

        if (!response.ok) {
            throw new Error(`Failed to load file inventory for '${id}' from '${moduleFilesUrl}': ${response.status} ${response.statusText}`);
        }

        const files = await response.json();

        for (const file of files) {
            file.path = relativizePaths(file.path, target.assetsPath);
        }

        target.files = files;
    };

    for (const [moduleId, module] of Object.entries(config.modules)) {
        if (!module.files) {
            tasks.push(loadFilesIndex(moduleId, module));
        }
    }

    if (!config.xshell.files) {
        tasks.push(loadFilesIndex("xshell", config.xshell));
    }

    await Promise.all(tasks);

    return config;
}

// resolver functions
function fillResolverRules(config) {
    const resolver = config.xshell.resolver;
    const contractDeclarations = new Map();

    for (const [moduleId, module] of Object.entries(config.modules)) {
        const moduleAssetsPath = module.assetsPath;
        const moduleAssetsPathContracts = moduleAssetsPath + "/contracts";

        resolver.icon ??= {};
        resolver.icon[moduleId] ??= {
            src: `${moduleAssetsPath}/icons/${moduleId}.svg`,
            loader: "icon-svg",
            cache: true,
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.icon[`${moduleId}-{name}`] ??= {
            src: `${moduleAssetsPath}/icons/${moduleId}-{name}.svg`,
            loader: "icon-svg",
            cache: true,
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.layout ??= {};
        resolver.layout[`${moduleId}-layout-{name}`] ??= {
            src: `${moduleAssetsPath}/layouts/${moduleId}-layout-{name}.js`,
            loader: "component-js",
            cache: true,
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.component ??= {};
        resolver.component[`${moduleId}-{name}`] ??= {
            src: `${moduleAssetsPath}/components/${moduleId}-{name}.js`,
            loader: "component-js",
            cache: true,
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.page ??= {};
        resolver.page[`${moduleAssetsPath}/{path}.js`] ??= {
            src: `${moduleAssetsPath}/{path}.js`,
            loader: "page-js",
            cache: true,
            cacheMode: "path",
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.page[`${moduleAssetsPath}/{path}.html`] ??= {
            src: `${moduleAssetsPath}/{path}.js`,
            loader: "page-js",
            cache: true,
            cacheMode: "path",
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.page[`${moduleAssetsPath}/{path}.md`] ??= {
            src: `${moduleAssetsPath}/{path}.md`,
            loader: "page-md",
            cache: true,
            cacheMode: "path",
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.module ??= {};
        resolver.module[`${moduleId}-{name}`] ??= {
            src: `${moduleAssetsPath}/${moduleId}-{name}.js`,
            loader: "module-js",
            cache: true,
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.module[`${moduleAssetsPath}/{path}.js`] ??= {
            src: `${moduleAssetsPath}/{path}.js`,
            loader: "module-js",
            cache: true,
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.style ??= {};
        resolver.style[`${moduleAssetsPath}/{path}.css`] ??= {
            src: `${moduleAssetsPath}/{path}.css`,
            loader: "style-css",
            cache: true,
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.string ??= {};
        resolver.string[`${moduleAssetsPath}/{path}`] ??= {
            src: `${moduleAssetsPath}/{path}`,
            loader: "string",
            cache: true,
            moduleId,
            modulePath: moduleAssetsPath
        };

        resolver.contract ??= {};

        for (const file of module.files) {
            if (!file.path.startsWith(moduleAssetsPathContracts + "/") || !file.path.endsWith(".json")) continue;

            const id = file.path.substring(moduleAssetsPathContracts.length + 1).replace(/\.json$/, "");
            const first = contractDeclarations.get(id);

            if (first) {
                throw new Error(
                    `Duplicate contract '${id}' declared by module '${first.moduleId}' in '${first.path}' ` +
                    `and module '${moduleId}' in '${file.path}'.`
                );
            }

            if (Object.hasOwn(resolver.contract, id)) {
                throw new Error(
                    `Duplicate contract '${id}' conflicts with an existing resolver entry while processing ` +
                    `module '${moduleId}' file '${file.path}'.`
                );
            }

            contractDeclarations.set(id, { moduleId, path: file.path });

            resolver.contract[id] = {
                src: `${moduleAssetsPathContracts}/${id}.json`,
                loader: "object-json",
                cache: true,
                moduleId,
                modulePath: moduleAssetsPath
            };
        }
    }

    return config;
}

// xshell initialization functions
async function initializeXShell(config, loadXShellModule = url => import(url)) {
    console.log("bootstrap: loading xshell ...");

    const authoredSrc = config.xshell.resolver.module.xshell.src;
    const xshellUrl = new URL(authoredSrc.startsWith("/") ? authoredSrc.substring(1) : authoredSrc, appBaseUrl).href;
    const xshell = (await loadXShellModule(xshellUrl)).default;

    console.log("bootstrap: validating xshell config ...");
    await xshell.validateConfig(config);

    console.log("bootstrap: initializing xshell ...");

    await xshell.init(deepFreeze(config));
}


// bootstrap
async function bootstrap() {
    showSpinner();

    let config = await loadConfig();

    if (!await installServiceWorker(config)) return;

    config = await loadFilesIndexes(config);
    config = fillResolverRules(config);

    await initializeXShell(config);

    hideSpinner();
}


// exec bootstrap
bootstrap().catch(error => {
    console.error("XShell bootstrap failed:", error);
    hideSpinner();
    showBootstrapError(error);
});
