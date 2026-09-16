// export page
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
            onCommand(command, params) {
                if (command == "load") {
                    // load

                } else if (command == "submit") {
                    //ok
                    this.close(state.value);

                } else if (command == "cancel") {
                    //cancel
                    this.close(null);
                }                
            }
        };
    }
}
