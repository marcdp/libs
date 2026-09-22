import { Validator } from "json-schema"

export default class {

    // vars
    _config = null;
    _loader = null;

    // ctor
    constructor({ config, loader }) {
        this._config = config;
        this._loader = loader;
    }

    // methods
    async start() {
        const schema = await this._loader.load("schema:config.schema.json");
        const validator = new Validator(schema, "2020-12");
        const result = validator.validate(this._config);
        if (!result.valid) {
            throw new Error("Invalid XShell configuration", {
                cause: result.errors
            });
        }
    }
    async stop() {
    }
}