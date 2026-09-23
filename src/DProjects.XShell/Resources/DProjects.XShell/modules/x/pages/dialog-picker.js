// contract
export const contract = {
    description: "Shows a picker dialog with various options and an OK button.",
    events: {},
    properties: {
        title: {type:"string", default:"", state:true, description:"The title of the picker dialog."},
        message: {type:"string", default:"", state:true, description:"The message to display in the picker dialog."},
        defaultValue: {type:"any", default:null, state:true, description:"The default value of the picker."},
        domain: {type:"array", default:[], state:true, description:"The list of keypairs (value and label) for the picker."},
        inputType: {type:"string", default:"select", state:true, enum:["select"], description:"The type of input for the picker."},
        placeholder: {type:"string", default:"", state:true, description:"The placeholder text for the picker."},
        multiple: {type:"boolean", default:false, state:true, description:"Whether multiple selection is allowed."},
        required: {type:"boolean", default:false, state:true, description:"Whether the picker is required."},
        value: {type:"any", default:null, description:"The current value of the picker."},       
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
            <x-button slot="cancel" label="Cancel" command="cancel" class="cancel"></x-button>
            <x-button slot="footer" label="Save" command="submit" class="submit"></x-button>            
        </x-form>
    `,    
    state: {
        title: {value:"", context:true},
        message: {value:"", context:true},
        defaultValue: {value:null, context:true},
        domain: {value:[], description:"list of keypairs value and label)", context:true},
        inputType: {value:"select", enum:["select"], context:true},
        placeholder: {value:"", context:true},
        multiple: {value:false, context:true},
        required: {value:false, context:true},
        value: {value:null, context:true}
    },
    script({ state, context }) {
        return {
            load(params) {
                // load
            },

            submit(params) {
                //ok
                this.close(state.value);
            },

            cancel(params) {
                //cancel
                this.close(null);
            }
        };
    }
}
