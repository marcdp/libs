// contract
export const contract = {
    description: "Displays slotted content only on mobile-sized viewports.",
    events: {},
    properties: {},
    methods: {},
    slots: {
        "":{
            description: "Slot for providing content that will only be displayed on mobile-sized viewports."
        }
    }
};


// definition
export default {
    style: `
        @media (min-width: 769px) {
            :host {display:none;}
        }        
    `,
    template: `<slot></slot>`
};

