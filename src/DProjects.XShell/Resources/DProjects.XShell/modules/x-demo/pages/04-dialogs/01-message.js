export const contract = {
    description: "Message and confirmation dialogs",
    events: {},
    properties: {},
    methods: {}
};

export default {
    template: `
        <p>
            Message dialogs display a notice and return <code>"ok"</code> when the user closes them. Confirmation dialogs return the value of
            the button that was pressed.
        </p>

        <h2>Message</h2>

        <p>Each button uses the same API with a different supported message type.</p>

        <p>
            <x-button label="Info" command="message-info"></x-button>
            <x-button label="Success" command="message-success"></x-button>
            <x-button label="Warning" command="message-warning"></x-button>
            <x-button label="Error" command="message-error"></x-button>
        </p>

        <pre x-pre><code>const result = await dialog.message({
    type: "success",
    title: "Saved",
    message: "The changes were saved."
});</code></pre>

        <h2>Confirmation</h2>

        <p>
            The current variants are <code>yesno</code>, <code>yesnocancel</code>, <code>okcancel</code>, and <code>ok</code>.
        </p>

        <p>
            <x-button label="Yes / No" command="confirm-yesno"></x-button>
            <x-button label="Yes / No / Cancel" command="confirm-yesnocancel"></x-button>
            <x-button label="OK / Cancel" command="confirm-okcancel"></x-button>
            <x-button label="OK" command="confirm-ok"></x-button>
        </p>

        <pre x-pre><code>const result = await dialog.confirm({
    title: "Delete item",
    message: "Are you sure?",
    variant: "yesno"
});</code></pre>

        <x-divider></x-divider>

        <p x-if="state.lastResult != null">
            Last result from <strong x-text="state.lastAction"></strong>:
            <code x-text="state.lastResult"></code>
        </p>
    `,
    state: {
        lastAction: "",
        lastResult: null
    },
    controller({ state, dialog }) {
        const showMessage = async (type, title, message) => {
            state.lastAction = `dialog.message(${type})`;
            state.lastResult = await dialog.message({type, title, message});
        };

        const showConfirmation = async (variant) => {
            state.lastAction = `dialog.confirm(${variant})`;
            state.lastResult = await dialog.confirm({
                title: "Confirmation",
                message: "Choose a button to see the returned value.",
                variant
            });
        };

        return {
            async "message-info"() {
                await showMessage("info", "Information", "This is an informational message.");
            },
            async "message-success"() {
                await showMessage("success", "Saved", "The operation completed successfully.");
            },
            async "message-warning"() {
                await showMessage("warning", "Review needed", "Check the values before continuing.");
            },
            async "message-error"() {
                await showMessage("error", "Operation failed", "The sample operation could not be completed.");
            },
            async "confirm-yesno"() {
                await showConfirmation("yesno");
            },
            async "confirm-yesnocancel"() {
                await showConfirmation("yesnocancel");
            },
            async "confirm-okcancel"() {
                await showConfirmation("okcancel");
            },
            async "confirm-ok"() {
                await showConfirmation("ok");
            }
        };
    }
};
