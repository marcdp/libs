// contract
export const contract = {
    description: "Query parameters test page",
    events: {},
    properties: {
        name: { type: "string", default:"marcus", query: true, state: true },
        count: { type: "number", default:123, query: true, state: true, reflect: true }
    },
    methods: {}
};

// implementation
export default {
    template: `
        <h2>Query parameters</h2>

        <p>
            Navigate to this page with query parameters, for example:
        </p>

        <pre>?name=Marc&count=10&enabled=true</pre>

        <x-datafields>
            <x-datafield label="name">
                <span>{{ state.name }}</span>
            </x-datafield>

            <x-datafield label="count">
                <span>{{ state.count }}</span>
            </x-datafield>

        </x-datafields>

        <x-button label="Increment count" command="incCount"></x-button>


    `,

    state: {
        name: "marcus",
        count: 123,
        enabled: "",
    },

    controller({ state, query }) {
        return {
            load(params) {
            },  
            incCount() {
                state.count++;
            }
        };
    }
};