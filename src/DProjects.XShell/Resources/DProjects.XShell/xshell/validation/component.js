import ComponentSchema from "../schemas/component.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/4.1.1/json-schema.js"

// vars 
const validator = new Validator(ComponentSchema, "2020-12");

// export
export default async function validateComponent(src, definition) {
    // validate
    const definitionObject = Object.fromEntries(Object.entries(definition).map(([key, value]) => [key, typeof value === "function" ? {} : value]));
    const result = validator.validate(definitionObject);
    if (!result.valid) {
        console.error(`Invalid component: ${src}`, result.errors);
        throw new Error(`Invalid component: ${src}`, { cause: result.errors });
    }
}