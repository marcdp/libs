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
        :host {display:block;
            border: var(--x-datafield-border); 
            background:var(--x-datafield-background);
            border-radius: var(--x-datafield-border-radius);
            overflow: hidden;
        }
        table {width:100%; border-collapse: collapse; border-radius: var(--x-datafield-border-radius);}
        th:first-child, td:first-child {width:10em; }
        th, td {text-align:left; padding:.15em; vertical-align:top; background:var(--x-datafield-background); padding-left:.5em;}
        td.object, td.array {padding:0}
        td.object x-propertygrid, td.array x-propertygrid {margin:-1px;}
        
        tr:nth-child(odd) th, tr:nth-child(odd) td {background:var(--x-color-background-alt);}
        
        .date {color:var(--x-propertygrid-date-color);}
        .number {color:var(--x-propertygrid-number-color);}
        .text {color:var(--x-propertygrid-text-color);}

        :host(.no-border) {
            border: none;
        }

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
                    <td x-if="state.value[key] == null">
                        null
                    </td>
                    <td x-elseif="state.schema[key].type == 'object'" class="object">
                        <x-propertygrid headers="true" x-prop:value="state.value[key]" class="no-border"></x-propertygrid>
                    </td>
                    <td x-elseif="state.schema[key].type == 'array'" class="array">
                        <div x-for="(item, index) in state.value[key]">
                            <x-propertygrid headers="true" x-prop:value="item" class="no-border"></x-propertygrid>
                        </div>
                    </td>
                    <td x-elseif="state.schema[key].type == 'date'" class="date">
                        {{ state.value[key] }}                        
                    </td>
                    <td x-elseif="state.schema[key].type == 'boolean'" class="boolean">
                        <input type="checkbox" x-attr:checked="state.value[key]" disabled>
                    </td>
                    <td x-else x-attr:class="state.schema[key].type">
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
                        type = "boolean";
                    } else if (state.value[key] instanceof Date) {
                        type = "date";
                    } else if (Array.isArray(state.value[key])) {
                        type = "array";
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

