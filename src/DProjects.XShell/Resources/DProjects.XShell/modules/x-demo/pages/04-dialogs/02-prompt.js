export const contract = {
    description: "Prompt dialogs",
    events: {},
    properties: {},
    methods: {}
};

export default {
    template: `
        <p>
            A prompt collects one value. The current prompt input types are <code>text</code>, <code>number</code>, and <code>password</code>.
            Saving returns the value; Cancel returns <code>null</code>.
        </p>

        <h2>Prompt configuration</h2>

        <x-datafields columns="2">
            <x-datafield
                type="select"
                label="Input type"
                x-prop:domain="state.inputTypes"
                x-model="state.inputType">
            </x-datafield>

            <x-datafield
                type="text"
                label="Default value"
                x-model="state.defaultValue">
            </x-datafield>

            <x-datafield
                type="text"
                label="Placeholder"
                x-model="state.placeholder">
            </x-datafield>

            <x-datafield
                type="checkbox"
                label="Required"
                label-secondary="The prompt must contain a value"
                x-model="state.required">
            </x-datafield>
        </x-datafields>

        <p>
            <x-button label="Open prompt" command="open-prompt" class="submit"></x-button>
        </p>

        <pre x-pre><code>const result = await dialog.prompt({
    title: "Your name",
    message: "Enter a name for the greeting.",
    defaultValue: "Ada",
    inputType: "text",
    placeholder: "Name",
    required: true
});</code></pre>

        <x-divider></x-divider>

        <h2>Last result</h2>

        <p x-text="state.resultText"></p>
    `,
    state: {
        inputTypes: [
            {value: "text", label: "Text"},
            {value: "number", label: "Number"},
            {value: "password", label: "Password"}
        ],
        inputType: "text",
        defaultValue: "Ada",
        placeholder: "Enter a value",
        required: false,
        resultText: "No prompt has been opened yet."
    },
    controller({ state, dialog }) {
        return {
            async "open-prompt"() {
                const result = await dialog.prompt({
                    title: "Prompt example",
                    message: "Enter a value and choose Save, or choose Cancel.",
                    defaultValue: state.defaultValue,
                    inputType: state.inputType,
                    placeholder: state.placeholder,
                    required: state.required
                });
                state.resultText = result === null ? "null — the prompt was cancelled." : `Returned value: ${String(result)}`;
            }
        };
    }
};
