
// declaration
export const declaration = {
    description: "Provides a container for data table content.",
    events: {},
    properties: {},
    methods: {}
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

