import ComponentContractSchema from "../schemas/component.contract.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/4.1.1/json-schema.js"

// vars 
const validator = new Validator(ComponentContractSchema, "2020-12");

// export
export default async function validateComponentContract(src, contract) {
    // validate
    const result = validator.validate(contract);
    if (!result.valid) {
        console.error(`Invalid component contract: ${src}`, result.errors);
        throw new Error(`Invalid component contract: ${src}`, { cause: result.errors });
    }
}