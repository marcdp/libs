// methods
export function getLocalModule(config, configUrl) {
    // identify the single definition owned by this configuration document
    const entries = Object.entries(config.modules || {}).filter(([, module]) => module && typeof module === "object" && !Object.hasOwn(module, "configUrl"));
    if (entries.length !== 1) {
        const ids = entries.map(([id]) => `'${id}'`);
        const suffix = ids.length ? `: ${ids.join(", ")}.` : ".";
        throw new Error(`Module configuration '${configUrl}' must contain exactly one local module definition, but found ${entries.length}${suffix}`);
    }
    return { id: entries[0][0], definition: entries[0][1] };
}
export function resolveModuleConfigUrl(configUrl, ownerConfigUrl) {
    // resolve url-prefixed and ordinary reference URLs against their owner document
    if (typeof configUrl !== "string" || !configUrl.trim()) {
        throw new Error(`Module reference in '${ownerConfigUrl}' must declare a non-empty configUrl.`);
    }
    const value = configUrl.startsWith("url:") ? configUrl.substring(4).trim() : configUrl;
    return new URL(value, ownerConfigUrl).href;
}
export async function discoverModuleConfigs(rootConfig, rootConfigUrl, loadConfig) {
    // discover and validate the complete canonical dependency graph
    const configsByUrl = new Map();
    const moduleUrls = new Map();
    let pendingByUrl = new Map();
    const registerModuleUrl = (moduleId, configUrl) => {
        const existingUrl = moduleUrls.get(moduleId);
        if (existingUrl && existingUrl !== configUrl) {
            throw new Error(`Module '${moduleId}' is referenced with conflicting configUrl values: '${existingUrl}' and '${configUrl}'.`);
        }
        moduleUrls.set(moduleId, configUrl);
    };
    const registerConfig = (config, configUrl, isRoot) => {
        const localModule = getLocalModule(config, configUrl);
        const expectedIds = pendingByUrl.get(configUrl)?.expectedIds || new Set([localModule.id]);
        for (const expectedId of expectedIds) {
            if (expectedId !== localModule.id) {
                throw new Error(`Module reference '${expectedId}' points to '${configUrl}', but that configuration defines local module '${localModule.id}'.`);
            }
        }
        registerModuleUrl(localModule.id, configUrl);
        const references = [];
        for (const [moduleId, reference] of Object.entries(config.modules || {})) {
            if (moduleId === localModule.id) continue;
            if (!reference || typeof reference !== "object") {
                throw new Error(`Module reference '${moduleId}' in '${configUrl}' must be an object that declares configUrl.`);
            }
            const referenceUrl = resolveModuleConfigUrl(reference.configUrl, configUrl);
            reference.configUrl = referenceUrl;
            if (!isRoot && Object.hasOwn(reference, "params")) {
                throw new Error(`Module '${localModule.id}' cannot configure params for dependency '${moduleId}'; dependency params may only be supplied by the root application.`);
            }
            if (Object.hasOwn(reference, "assetsUrl")) {
                throw new Error(`Module reference '${moduleId}' in '${configUrl}' cannot override assetsUrl; physical assets are owned by the referenced module definition.`);
            }
            registerModuleUrl(moduleId, referenceUrl);
            const referencedNode = configsByUrl.get(referenceUrl);
            if (referencedNode && referencedNode.id !== moduleId) {
                throw new Error(`Module reference '${moduleId}' points to '${referenceUrl}', but that configuration defines local module '${referencedNode.id}'.`);
            }
            references.push({ id: moduleId, definition: reference, configUrl: referenceUrl });
        }
        const node = { id: localModule.id, definition: localModule.definition, config, configUrl, references, isRoot };
        configsByUrl.set(configUrl, node);
        return node;
    };

    // register the root before loading its dependencies
    const rootNode = registerConfig(rootConfig, rootConfigUrl, true);
    while (true) {
        // deduplicate unresolved URLs within this discovery pass
        const nextPendingByUrl = new Map();
        for (const node of configsByUrl.values()) {
            for (const reference of node.references) {
                if (configsByUrl.has(reference.configUrl)) continue;
                const pending = nextPendingByUrl.get(reference.configUrl) || { expectedIds: new Set() };
                pending.expectedIds.add(reference.id);
                nextPendingByUrl.set(reference.configUrl, pending);
            }
        }
        if (!nextPendingByUrl.size) break;
        pendingByUrl = nextPendingByUrl;
        const urls = [...pendingByUrl.keys()];
        const loadedConfigs = await Promise.all(urls.map(url => loadConfig(url)));
        for (let i = 0; i < urls.length; i++) registerConfig(loadedConfigs[i], urls[i], false);
    }

    // derive a deterministic dependency-first order and reject cycles
    const mergeOrder = [];
    const visiting = new Set();
    const visited = new Set();
    const path = [];
    const visit = node => {
        if (visiting.has(node.id)) {
            const cycleStart = path.indexOf(node.id);
            const cycle = [...path.slice(cycleStart), node.id].map(id => `'${id}'`).join(" -> ");
            throw new Error(`Module dependency cycle detected: ${cycle}.`);
        }
        if (visited.has(node.id)) return;
        visiting.add(node.id);
        path.push(node.id);
        for (const reference of [...node.references].sort((left, right) => left.id < right.id ? -1 : left.id > right.id ? 1 : 0)) {
            visit(configsByUrl.get(reference.configUrl));
        }
        path.pop();
        visiting.delete(node.id);
        visited.add(node.id);
        mergeOrder.push(node);
    };
    visit(rootNode);
    return { rootNode, configsByUrl, mergeOrder };
}
export function mergeConfigs(configs) {
    // merge dependency-first fragments so dependents and the root have final precedence
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
