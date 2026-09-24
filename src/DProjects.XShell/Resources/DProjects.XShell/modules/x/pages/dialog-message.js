// contract
export const contract = {
    description: "Shows a message dialog with various types and an OK button.",
    events: {},
    properties: {
        title: {type:"string", default:"", state:true, description:"The title of the message dialog."},
        message: {type:"string", default:"", state:true, description:"The message to display in the message dialog."},
        type: {type:"string", default:"info", state:true, enum:["info","success","warning","error"], description:"The type of message dialog to display."},
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
        title: {value:"", context:true},
        message: {value:"", context:true},
        type: {value:"info", enum:["info","success","warning","error"], context:true},
    },
    controller({ state, context }) {
        return {
            load(params) {
                // load
            },

            submit(params) {
                // submit
                this.close("ok");
            }
        };
    }
}
