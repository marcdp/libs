// contract
export const contract = {
    description: "Propertygrid.",
    events: {},
    properties: {
        value: {type: "object", default: {}, state:true, description:"The object containing the properties to be displayed in the property grid."}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:block;}
        table {width:100%;border-collapse:collapse;}
        th:first-child {width:15em;}
        th, td {border:1px solid #ccc;text-align:left; padding:.2em;}
    `,
    template: `
        <table>
            <thead>
                <tr>
                    <th>Property</th>
                    <th>Value</th>
                </tr>
            </thead>
            <tbody>
                <tr x-for="(key, index) in state.schema">
                    <td>{{ key }}</td>
                    <td>
                        <div x-if="state.schema[key].type == 'object'">
                            <x-propertygrid x-prop:value="123"></x-propertygrid>
                        </div>
                        <div x-if="state.schema[key].type != 'object'">
                            <x-datafield x-attr:type="state.schema[key].type" x-model="state.value[key]"></x-datafield>
                        </div>
                    </td>
                </tr>
            </tbody>
        </table>

        <x-json x-prop:value="state.value"></x-json>
    `,
    state: {
        value: {},  
        schema: {}
    },
    controller({ events, state }) {
        return {
            async load() {
                events.on(state, "change:value", "refresh");
            },
            refresh() {
                const schema = {};
                for (const key in state.value) {
                    let type = "text";
                    if (typeof state.value[key] === "number") {
                        type = "number";
                    } else if (typeof state.value[key] === "boolean") {
                        type = "checkbox";
                    } else if (typeof state.value[key] === "object") {
                        type = "object";
                    }
                    schema[key] = { 
                        type: type
                    };
                }
                state.schema = schema;
            }
        }
    }
};

