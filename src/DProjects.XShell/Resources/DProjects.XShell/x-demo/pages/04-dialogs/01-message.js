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
            <x-button label="Info" command="messageInfo"></x-button>
            <x-button label="Success" command="messageSuccess"></x-button>
            <x-button label="Warning" command="messageWarning"></x-button>
            <x-button label="Error" command="messageError"></x-button>
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
            <x-button label="Yes / No" command="confirmYesNo"></x-button>
            <x-button label="Yes / No / Cancel" command="confirmYesNoCancel"></x-button>
            <x-button label="OK / Cancel" command="confirmOkCancel"></x-button>
            <x-button label="OK" command="confirmOk"></x-button>
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
            async messageInfo() {
                await showMessage("info", "Information", "This is an informational message.");
            },
            async messageSuccess() {
                await showMessage("success", "Saved", "The operation completed successfully.");
            },
            async messageWarning() {
                await showMessage("warning", "Review needed", "Check the values before continuing.");
            },
            async messageError() {
                await showMessage("error", "Operation failed", "The sample operation could not be completed.");
            },
            async confirmYesNo() {
                await showConfirmation("yesno");
            },
            async confirmYesNoCancel() {
                await showConfirmation("yesnocancel");
            },
            async confirmOkCancel() {
                await showConfirmation("okcancel");
            },
            async confirmOk() {
                await showConfirmation("ok");
            }
        };
    }
};
