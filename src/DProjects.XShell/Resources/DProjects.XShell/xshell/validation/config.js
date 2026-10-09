import ConfigSchema from "../schemas/config.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/4.1.1/json-schema.js"
import { compileRoute } from "../utils/route.js";

// vars
const validator = new Validator(ConfigSchema, "2020-12");

// export
export default async function validateConfig(src, config) {
    // validate
    const result = validator.validate(config);
    if (!result.valid) {
        console.error(`Invalid config: ${src}`, result.errors);
        throw new Error(`Invalid config: ${src}`, { cause: result.errors });
    }
    // validate cross-field Area module references
    for (const [areaId, area] of Object.entries(config.xshell.areas.definitions)) {
        for (const moduleId of area.modules || []) {
            if (!Object.hasOwn(config.modules, moduleId)) throw new Error(`Area '${areaId}' references unknown module '${moduleId}'.`);
        }
    }
    // validate routes with the same compiler used by Navigation
    for (const [moduleId, module] of Object.entries(config.modules)) {
        for (const path of Object.keys(module.routes || {})) {
            try {
                compileRoute(path);
            } catch (cause) {
                throw new Error(`Module '${moduleId}' declares invalid route '${path}'.`, { cause });
            }
        }
    }
    // global menu composition accepts only static arrays
    for (const menuName of config.xshell.areas.global) {
        for (const [moduleId, module] of Object.entries(config.modules)) {
            const menu = module.menus?.[menuName];
            if (menu !== undefined && !Array.isArray(menu)) throw new Error(`Global menu '${menuName}' from module '${moduleId}' must be a static array.`);
        }
    }
}
