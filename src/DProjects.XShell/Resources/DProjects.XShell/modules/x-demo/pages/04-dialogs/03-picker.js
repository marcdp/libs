export const contract = {
    description: "Picker dialogs",
    events: {},
    properties: {},
    methods: {}
};

export default {
    template: `
        <p>
            The current picker Page uses an HTML select field. Its domain is an array of <code>{ value, label }</code> items, and the result is
            the selected value or <code>null</code> when the dialog is cancelled.
        </p>

        <h2>Picker configuration</h2>

        <x-datafields columns="2">
            <x-datafield
                type="select"
                label="Default value"
                x-prop:domain="state.domain"
                x-model="state.defaultValue">
            </x-datafield>

            <x-datafield
                type="checkbox"
                label="Required"
                label-secondary="A selection is required"
                x-model="state.required">
            </x-datafield>
        </x-datafields>

        <p>
            <x-button label="Open picker" command="open-picker" class="submit"></x-button>
        </p>

        <pre x-pre><code>const result = await dialog.picker({
    title: "Choose a color",
    message: "Select the color used for the example.",
    domain: [
        {value: "red", label: "Red"},
        {value: "green", label: "Green"},
        {value: "blue", label: "Blue"}
    ],
    inputType: "select",
    defaultValue: "green",
    required: true
});</code></pre>

        <p>
            The service also exposes <code>multiple</code>, but the current x-datafield template/model path does not provide a reliable
            multiple-select result, so this executable example keeps <code>multiple</code> false.
        </p>

        <x-divider></x-divider>

        <h2>Last result</h2>

        <p x-text="state.resultText"></p>
    `,
    state: {
        domain: [
            {value: "red", label: "Red"},
            {value: "green", label: "Green"},
            {value: "blue", label: "Blue"}
        ],
        defaultValue: "green",
        required: false,
        resultText: "No picker has been opened yet."
    },
    controller({ state, dialog }) {
        return {
            async "open-picker"() {
                const result = await dialog.picker({
                    title: "Choose a color",
                    message: "Select the color used for the example.",
                    domain: state.domain,
                    inputType: "select",
                    defaultValue: state.defaultValue,
                    placeholder: "Choose a color",
                    multiple: false,
                    required: state.required
                });
                state.resultText = result === null ? "null — the picker was cancelled." : `Returned value: ${String(result)}`;
            }
        };
    }
};
