
// declaration
export const declaration = {
    description: "Displays slotted content only on desktop-sized viewports.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default {
    style: `
        @media (max-width: 768px) {
            :host {display:none;}
        }        
    `,
    template: `<slot></slot>`
};

