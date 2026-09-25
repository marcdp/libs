// contract
export const contract = {
    description: "Provides a flexible toolbar container.",
    events: {},
    properties: {},
    methods: {},
    slots: {
        "": {
            description: "Default slot for the toolbar content."
        }
    } 
};


// definition
export default {
    style: `
        :host {display:flex; align-items:center; gap:.2em; position:relative; flex-wrap:wrap;}
    `,
    template: `
        <slot></slot>
    `,
    state: {
    }
};

