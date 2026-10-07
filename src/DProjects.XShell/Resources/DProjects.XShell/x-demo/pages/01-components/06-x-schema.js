// contract
export const contract = {
    description: "XSchema component",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        Lorem ipsum
        <x-form>
            <x-datafields>
                <x-datafield label="This is the form">
                    <x-schema-editor 
                        x-prop:schema="state.schema" 
                        x-prop:value="state.value">
                    </x-schema-editor>
                </x-datafield>
            </x-datafields>
        </x-form>
        <x-json x-prop:value="state.value"></x-json>
    `,  
    state: {
        schema: {
            "$schema": "http://json-schema.org/draft-07/schema#",
            "$id": "https://json.schemastore.org/vsconfig.json",
            "properties": {
                "version": {
                "description": "The version of the component configuration file format.",
                "type": "string",
                "pattern": "^(\\d+\\.)?(\\d+\\.)?(\\d+\\.)?(\\d+)$"
                },
                "components": {
                "type": "array",
                "description": "An array of Visual Studio component names.",
                "items": {
                    "type": "string",
                    "minLength": 1
                }
                },
                "extensions": {
                "type": "array",
                "description": "An array of Visual Studio extensions. These can be URLs to marketplace extensions or paths to private VSIX files.",
                "items": {
                    "type": "string",
                    "minLength": 1
                }
                }
            },
            "required": ["components"],
            "title": "JSON schema for Visual Studio component configuration files",
            "type": "object"
            },
        value: { 
            a: 123, 
            b: "hello world"
        }
    },
    controller({ }) {
        return {
            load() {
               // load
            }
        };
    }
}
