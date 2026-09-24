import ConfigSchema from "../schemas/config.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/json-schema.js"

// vars
const validator = new Validator(ConfigSchema, "2020-12");

// export
export default async function validateConfig(src, contract) {
    // validate
    const result = validator.validate(contract);
    if (!result.valid) {
        console.error(`Invalid config: ${src}`, result.errors);
        throw new Error(`Invalid config: ${src}`, { cause: result.errors });
    }
}