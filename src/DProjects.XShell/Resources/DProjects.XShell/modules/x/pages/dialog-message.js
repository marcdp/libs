// export page
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
    script({ state, context }) {
        return {
            onCommand(command, params) {
                if (command == "load") {
                    // load
                } else if (command == "submit") {
                    // submit
                    this.close("ok");
                }                
            }
        };
    }
}
