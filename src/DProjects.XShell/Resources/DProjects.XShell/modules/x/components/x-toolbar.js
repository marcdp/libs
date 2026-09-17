import XElement from "x-element";

// declaration
export const declaration = {
    description: "Provides a flexible toolbar container.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default XElement.define("x-toolbar", {
    style: `
        :host {display:flex; align-items:center; gap:.2em; position:relative; flex-wrap:wrap;}
    `,
    template: `
        <slot></slot>
    `,
    state: {
    }
});

