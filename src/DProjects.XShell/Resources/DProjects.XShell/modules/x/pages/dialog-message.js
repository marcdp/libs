// contract
export const contract = {
    description: "Shows a message dialog with various types and an OK button.",
    events: {},
    properties: {
        title: {type:"string", default:"", state:true, description:"The title of the message dialog.", context:true},
        message: {type:"string", default:"", state:true, description:"The message to display in the message dialog.", context:true},
        type: {type:"string", default:"info", state:true, enum:["info","success","warning","error"], description:"The type of message dialog to display.", context:true},
    },
    methods: {}
};

// implementation
export default {
    template: `
        <x-form command="submit">            
            <x-notice x-attr:type="state.type" x-attr:label="state.title" x-attr:message="state.message">
            </x-notice>
            <x-button slot="footer" label="OK" command="submit" class="submit"></x-button>                        
        </x-form>
    `,    
    state: {
        title: "",
        message: "",
        type: "info",
    },
    controller({ page }) {
        return {
            submit() {
                // submit
                page.close("ok");
            }
        };
    }
}
