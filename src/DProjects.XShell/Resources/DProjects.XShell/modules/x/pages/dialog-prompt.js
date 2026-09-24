// contract
export const contract = {
    description: "Shows a prompt dialog.",
    events: {},
    properties: {
        title: {type:"string", default:"", state:true, description:"The title of the prompt dialog."},
        message: {type:"string", default:"", state:true, description:"The message to display in the prompt dialog."},
        defaultValue: {type:"any", default:null, state:true, description:"The default value of the prompt."},
        inputType: {type:"string", default:"text", state:true, enum:["text","number","password"], description:"The type of input for the prompt."},
        placeholder: {type:"string", default:"", state:true, description:"The placeholder text for the prompt."},
        required: {type:"boolean", default:false, state:true, description:"Whether the prompt is required."},
        value: {type:"any", default:null, state:true, description:"The current value of the prompt."}
    },
    methods: {}
};

// implementation
export default {
    template: `
        <x-form command="submit">            
            <x-datafields>
                <x-datafield 
                    label-mode="hidden"
                    x-attr:label="state.title"     
                    x-attr:description="state.message"     
                    x-model="state.value" 
                    x-attr:placeholder="state.placeholder"
                    x-attr:required="state.required"
                    x-attr:type="state.inputType"
                ></x-datafield>
            </x-datafields>
            <x-button slot="cancel" label="Cancel" command="cancel" class="cancel"></x-button>
            <x-button slot="footer" label="Save" command="submit" class="submit"></x-button>            
        </x-form>
    `,    
    state: {
        title: "",
        message: "",
        defaultValue: "",
        inputType: "text",
        placeholder: "",
        required: false,
        value: null
    },
    controller({ state, context }) {
        return {
            load(params) {
                // load
            },

            submit(params) {
                //submit
                this.close(state.value);
            },

            cancel(params) {
                //cancel
                this.close(null);
            }
        };
    }
}
