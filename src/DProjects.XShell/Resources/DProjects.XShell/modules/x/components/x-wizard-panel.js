import XElement from "x-element";

// contract
export const contract = {
    description: "Provides a panel container for a wizard.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default XElement.define("x-wizard-panel", {
    style: `
        :host {display:block;}
    `,
    template: `
        <slot></slot>
    `
});

