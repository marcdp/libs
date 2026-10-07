
// contract
export const contract = {
    description: "Displays a form, based on an schema.",
    events: {},
    properties: {
        src: {type:"string", default:null, attribute:true, state:true, description:"The source URL of the schema."},
        schema: {type:"object", default:null, attribute:false, state:true, description:"The schema of the form."},
        value: {type:"object", default:null, attribute:false, state:true, description:"The value of the form."}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:block;}
    `,
    template: `
        This is the x-schema-editor component
    `,
    state: {
        src: null,
        schema: null,
        value:  null
    },
    controller({ events, state, host }) {
        return {
            load() {
                // load
                events.on(state, "change:value", "refresh");
                events.on(state, "change:schema", "refresh");
                events.on(state, "change:src", async () => {
                    const response = await fetch(state.src);
                    state.schema = await response.json();
                });
            },
            mount() {
                //mount
            },
            refresh() {
                // refresh
                if (state.schema && state.value) {
                    console.log(JSON.stringify(state))
                }
                //debugger;
            }
        }
    }
};

