// export page
export default {
    template: `
        <x-form>
            <p>
                {{state.message}}
            </p>
            <x-button slot="footer" x-if="state.variant === 'yesno' || state.variant === 'yesnocancel'" command="yes" label="Yes" class="submit" autofocus></x-button>    
            <x-button slot="footer" x-if="state.variant === 'yesno' || state.variant === 'yesnocancel'" command="no" label="No" ></x-button>    
            <x-button slot="footer" x-if="state.variant === 'yesnocancel'" command="cancel" label="Cancel" ></x-button>    
            <x-button slot="footer" x-if="state.variant === 'okcancel' || state.variant === 'ok'" command="ok" label="OK" class="submit" ></x-button>    
        </x-form>
    `,    
    state: {
        message: {value:"", context:true},
        variant: {value:"yesno", enum:["yesno","yesnocancel","okcancel","ok"], context:true},
    },
    script({ state, context }) {
        return {
            onCommand(command, params) {
                if (command == "load") {
                    // load

                } else if (command == "yes") {
                    //yes
                    this.close("yes");

                } else if (command == "no") {
                    //no
                    this.close("no");

                } else if (command == "cancel") {
                    //cancel
                    this.close("cancel");

                } else if (command == "ok") {
                    //ok
                    this.close("ok");
                }                
            }
        };
    }
}
