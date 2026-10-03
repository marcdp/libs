import ContractSchema from "../schemas/contract.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/4.1.1/json-schema.js"

// vars 
const validator = new Validator(ContractSchema, "2020-12");

// export
export default async function validateContract(src, definition) {
    // validate
    const definitionObject = Object.fromEntries(Object.entries(definition).map(([key, value]) => [key, typeof value === "function" ? {} : value]));
    const result = validator.validate(definitionObject);
    if (!result.valid) {
        console.error(`Invalid contract: ${src}`, result.errors);
        throw new Error(`Invalid contract: ${src}`, { cause: result.errors });
    }
}