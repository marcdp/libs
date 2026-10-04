// contract
export const contract = {
    description: "Shows a confirmation dialog with various button options.",
    events: {},
    properties: {
        message: {type:"string", default:"", state:true, description:"The message to display in the confirmation dialog.", context:true},
        variant: {type:"string", default:"yesno", state:true, enum:["yesno","yesnocancel","okcancel","ok"], description:"The type of confirmation dialog to display.", context:true},
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
            <x-button slot="footer" x-if="state.variant == 'okcancel' || state.variant == 'ok'" command="ok" label="OK" class="submit"></x-button>
            <x-button slot="footer" x-if="state.variant == 'yesnocancel' || state.variant == 'okcancel'" command="cancel" label="Cancel"></x-button>
        </x-form>
    `,    
    state: {
        message: "",
        variant: "yesno",
    },
    controller({ page }) {
        return {
            yes() {
                //yes
                page.close("yes");
            },

            no() {
                //no
                page.close("no");
            },

            cancel() {
                //cancel
                page.close("cancel");
            },

            ok() {
                //ok
                page.close("ok");
            }
        };
    }
}
