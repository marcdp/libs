// contract
export const contract = {
    description: "Displays slotted content only on mobile-sized viewports.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default {
    style: `
        @media (min-width: 769px) {
            :host {display:none;}
        }        
    `,
    template: `<slot></slot>`
};

