export const contract = {
    description: "Custom dialog Page",
    events: {},
    properties: {},
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

            <x-button slot="cancel" label="Cancel" command="cancel" class="cancel"></x-button>
            <x-button slot="footer" label="Save" command="save" class="submit"></x-button>
        </x-form>
    `,
    state: {
        message: "",
        value: ""
    },
    controller({ state, context, host }) {
        return {
            load() {
                state.message = context?.message ?? "No context message was supplied.";
                state.value = context?.initialValue ?? "";
            },
            save() {
                host.close({accepted: true, value: state.value});
            },
            cancel() {
                host.close(null);
            }
        };
    }
};
