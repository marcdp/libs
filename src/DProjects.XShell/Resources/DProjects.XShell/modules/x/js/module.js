import { Validator } from "json-schema"

export default class {

    // vars
    _config = null;

    // ctor
    constructor({ config }) {
        this._config = config;
    }

    // methods
    async start() {
        const schema = await fetch("/_assets/xshell/schemes/config.scheme.json").then(res => res.json());
        const validator = new Validator(schema, "2020-12");
        const result = validator.validate(this._config);
        if (!result.valid) {
            console.error(result.errors);
        }
    }
    async stop() {
    }
}