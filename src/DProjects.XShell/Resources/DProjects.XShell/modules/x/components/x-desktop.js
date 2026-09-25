
// contract
export const contract = {
    description: "Displays slotted content only on desktop-sized viewports.",
    events: {},
    properties: {},
    methods: {},
    slots: {
        "":{
            description: "Slot for providing content that will only be displayed on desktop-sized viewports."
        }
    }
};


// definition
export default {
    style: `
        @media (max-width: 768px) {
            :host {display:none;}
        }        
    `,
    template: `<slot></slot>`
};

