// contract
export const contract = {
    description: "Shows a picker dialog with various options and an OK button.",
    events: {},
    properties: {
        title: {type:"string", default:"", state:true, description:"The title of the picker dialog.", context:true},
        message: {type:"string", default:"", state:true, description:"The message to display in the picker dialog.", context:true},
        domain: {type:"array", default:[], state:true, description:"The list of keypairs (value and label) for the picker.", context:true},
        inputType: {type:"string", default:"select", state:true, enum:["select"], description:"The type of input for the picker.", context:true}, 
        placeholder: {type:"string", default:"", state:true, description:"The placeholder text for the picker.", context:true},
        multiple: {type:"boolean", default:false, state:true, description:"Whether multiple selection is allowed.", context:true},
        required: {type:"boolean", default:false, state:true, description:"Whether the picker is required.", context:true},
        value: {type:"any", default:null, state:true, description:"The current value of the picker.", context:true},       
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
                    x-attr:placeholder="state.placeholder"
                    x-attr:required="state.required"
                    x-attr:multiple="state.multiple"
                    x-attr:type="state.inputType"
                    x-prop:domain="state.domain"
                    x-model="state.value" 
                ></x-datafield>
            </x-datafields>
            <x-button slot="footer" label="Save" command="submit" class="submit"></x-button>            
            <x-button slot="footer" label="Cancel" command="cancel" class="cancel"></x-button>
        </x-form>
    `,    
    state: {
        title: "",
        message: "",
        domain: [],
        inputType: "select",
        placeholder: "",
        multiple: false,
        required: false,
        value: null
    },
    controller({ state, page }) {
        return {
            submit() {
                //ok
                page.close(state.value);
            },
            cancel() {
                //cancel
                page.close(null);
            }
        };
    }
}
