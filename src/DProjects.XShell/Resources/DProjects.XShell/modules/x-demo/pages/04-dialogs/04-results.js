export const contract = {
    description: "Dialog results and asynchronous flow",
    events: {},
    properties: {},
    methods: {}
};

export default {
    template: `
        <p>
            Dialog calls are asynchronous. The controller pauses at <code>await dialog...</code>; when the dialog Page closes, the Promise
            resolves and the returned value can be stored in state.
        </p>

        <pre x-pre><code>controller({ dialog, state }) {
    return {
        async ask() {
            const result = await dialog.confirm({ ... });
            state.result = result;
        }
    };
}</code></pre>

        <h2>Run the flow</h2>

        <p>
            <x-button label="Confirmed result" command="confirmed-result"></x-button>
            <x-button label="Cancelable confirmation" command="cancelable-result"></x-button>
            <x-button label="Message result" command="message-result"></x-button>
        </p>

        <p>Each command opens a Page, waits for it to close, and then stores the result below.</p>

        <x-divider></x-divider>

        <h2>Stored result</h2>

        <p><strong x-text="state.lastAction"></strong></p>
        <pre x-pre><code x-text="state.resultText"></code></pre>
    `,
    state: {
        lastAction: "No dialog has been opened yet.",
        resultText: "null"
    },
    controller({ state, dialog }) {
        const storeResult = (action, result) => {
            state.lastAction = action;
            state.resultText = JSON.stringify(result);
        };

        return {
            async "confirmed-result"() {
                const result = await dialog.confirm({
                    title: "Confirm flow",
                    message: "Choose Yes to return a confirmed result.",
                    variant: "yesno"
                });
                storeResult("dialog.confirm() resolved", result);
            },
            async "cancelable-result"() {
                const result = await dialog.confirm({
                    title: "Cancelable flow",
                    message: "Choose Cancel to observe the cancel result.",
                    variant: "yesnocancel"
                });
                storeResult("dialog.confirm() resolved", result);
            },
            async "message-result"() {
                const result = await dialog.message({
                    type: "info",
                    title: "Message flow",
                    message: "Close this message to resolve the Promise."
                });
                storeResult("dialog.message() resolved", result);
            }
        };
    }
};
