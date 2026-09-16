// export page
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
        title: {value:"", context:true},
        message: {value:"", context:true},
        defaultValue: {value:"", context:true},
        inputType: {value:"text", enum:["text","number","password"], context:true},
        placeholder: {value:"", context:true},
        required: {value:false, context:true},
        value: {value:null, context:true}
    },
    script({ state, context }) {
        return {
            onCommand(command, params) {
                if (command == "load") {
                    // load

                } else if (command == "submit") {
                    //submit
                    this.close(state.value);

                } else if (command == "cancel") {
                    //cancel
                    this.close(null);
                }                
            }
        };
    }
}
