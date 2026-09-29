// contract
export const contract = {
    description: "Propertygrid.",
    events: {},
    properties: {
        headers: {type: "boolean", default: true, attribute:true, state:true, description:"Whether to display the headers in the property grid."},
        value: {type: "object", default: null, state:true, description:"The object containing the properties to be displayed in the property grid."}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:block;}
        table {width:100%; background:#cccccc; border-spacing:1px;}
        td:first-child {width:10em;}
        th, td {text-align:left; padding:.3em; vertical-align:top; background:white}
        td.object {padding:0}
        td.object x-propertygrid {margin:-1px;}
    `,
    template: `
        <table>
            <thead x-if="state.headers">
                <tr>
                    <th>Property</th>
                    <th>Value</th>
                </tr>
            </thead>
            <tbody>
                <tr x-for="(key, index) in state.schema">
                    <td class="key">
                        {{ key }}:
                    </td>
                    <td x-if="state.schema[key].type == 'object'" class="object">
                        <x-propertygrid headers="false" x-prop:value="state.value[key]"></x-propertygrid>
                    </td>
                    <td x-elseif="state.schema[key].type == 'date'">
                        {{ state.value[key] }}                        
                    </td>
                    <td x-else>
                        {{ state.value[key]}}                        
                    </td>
                </tr>
            </tbody>
        </table>

        
    `,
    state: {
        headers: true,
        value: null,  
        schema: {}
    },
    controller({ events, state, host}) {
        return {
            async load() {
                events.on(state, "change:value", "refresh");                
                this.refresh();
            },
            refresh() {
                const schema = {};
                for (const key in state.value) {
                    let type = "text";
                    if (typeof state.value[key] === "number") {
                        type = "number";
                    } else if (typeof state.value[key] === "boolean") {
                        type = "checkbox";
                    } else if (state.value[key] instanceof Date) {
                        type = "date";
                    } else if (typeof state.value[key] === "object") {
                        type = "object";
                    }
                    schema[key] = { type: type };
                }
                state.schema = schema;
            }
        }
    }
};

