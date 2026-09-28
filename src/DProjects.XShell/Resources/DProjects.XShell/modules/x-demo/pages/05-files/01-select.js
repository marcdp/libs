// contract
export const contract = {
    description: "Selecting one browser file",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    style: `
        .file-demo-result {padding:.75em; border:var(--x-datafield-border); border-radius:var(--x-datafield-border-radius);}
        .file-demo-result p {margin:.25em 0;}
    `,
    template: `
        <h2>Select</h2>

        <x-form>
            <x-datafields>
                <x-datafield label="Select number" type="number" x-model="state.a"></x-datafield>
                <x-datafield label="Select a single file" type="file" x-model="state.file"></x-datafield>
                <x-datafield label="Select a multiple files" type="file" x-model="state.files" multiple></x-datafield>
            </x-datafields>

        </x-form>

        <x-json x-prop:value="state | json_stringify"></x-json>

    `,
    state: {
        a:123, 
        file: null,
        files: []
    },
    controller({ state }) {
        return {
            load() {
            }
        };
    }
};
