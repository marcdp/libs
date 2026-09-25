// contract
export const contract = {
    description: "Shows a confirmation dialog with various button options.",
    events: {},
    properties: {
        message: {type:"string", default:"", state:true, description:"The message to display in the confirmation dialog."},
        variant: {type:"string", default:"yesno", state:true, enum:["yesno","yesnocancel","okcancel","ok"], description:"The type of confirmation dialog to display."},
    },
    methods: {}
};


// implementation
export default {
    template: `
        <x-form>
            <p>
                {{state.message}}
            </p>
            <x-button slot="footer" x-if="state.variant == 'yesno' || state.variant == 'yesnocancel'" command="yes" label="Yes" class="submit" autofocus></x-button>
            <x-button slot="footer" x-if="state.variant == 'yesno' || state.variant == 'yesnocancel'" command="no" label="No"></x-button>
            <x-button slot="footer" x-if="state.variant == 'yesnocancel'" command="cancel" label="Cancel"></x-button>
            <x-button slot="footer" x-if="state.variant == 'okcancel' || state.variant == 'ok'" command="ok" label="OK" class="submit"></x-button>
        </x-form>
    `,    
    state: {
        message: "",
        variant: "yesno",
    },
    controller({ state, context }) {
        return {
            load(params) {
                // load
            },

            yes(params) {
                //yes
                this.close("yes");
            },

            no(params) {
                //no
                this.close("no");
            },

            cancel(params) {
                //cancel
                this.close("cancel");
            },

            ok(params) {
                //ok
                this.close("ok");
            }
        };
    }
}
