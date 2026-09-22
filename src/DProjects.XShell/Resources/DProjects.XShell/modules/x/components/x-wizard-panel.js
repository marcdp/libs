// contract
export const contract = {
    description: "Provides a panel container for a wizard.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:block;}
    `,
    template: `
        <slot></slot>
    `
};
