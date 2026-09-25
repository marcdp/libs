
// contract
export const contract = {
    description: "Provides a container for data table content.",
    events: {},
    properties: {},
    methods: {},
    slots: {
        "": {
            description: "Default slot for data table content."
        }
    }
};


// implementation
export default {
    style: `
        :host { display:block; }
    `,
    template: `
        <slot></slot>
    `,
    state: {
    }
};

