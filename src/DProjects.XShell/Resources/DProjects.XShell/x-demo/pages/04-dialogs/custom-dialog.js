export const contract = {
    description: "Custom dialog Page",
    events: {},
    properties: {
        message: {type:"string", default:"", state:true, description:"The context message.", context:true},
        value: {type:"string", default:"", state:true, description:"The initial value.", context:true},
    },
    methods: {}
};

export default {
    template: `
        <p>
            This is an ordinary Page. It received the following values through <code>context</code>:
        </p>

        <x-notice type="info" label="Context" x-attr:message="state.message"></x-notice>

        <x-form>
            <x-datafield
                type="text"
                label="Value"
                x-model="state.value">
            </x-datafield>

            <pre x-pre><code>controller({ context, host }) {
    save() {
        host.close({ accepted: true, value: ... });
    }

    cancel() {
        host.close(null);
    }
}</code></pre>

            <x-button slot="footer" label="Save" command="save" class="submit"></x-button>
            <x-button slot="footer" label="Cancel" command="cancel" class="cancel"></x-button>
        </x-form>

        <x-button command="showToast" label="Show Toast"></x-button>
    `,
    state: {
        message: "",
        value: ""
    },
    controller({ state, context, page, toast }) {
        return {
            save() {
                // save
                page.close({accepted: true, value: state.value});
            },
            cancel() {
                // cancel
                page.close(null);
            },
            showToast() {
                toast.show("Hello world", {
                    type: "success",
                    page,
                    duration: 4000,
                });
            }
        };
    }
};
