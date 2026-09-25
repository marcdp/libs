
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


// definition
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

