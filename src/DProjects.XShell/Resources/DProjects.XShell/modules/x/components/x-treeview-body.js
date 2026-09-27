// contract
export const contract = {
    description: "Provides a container for tree-view item content.",
    events: {},
    properties: {},
    methods: {},
    slots: {
        "": {
            description: "Default slot."
        }
    }
};


// implementation
export default {
    style: `
        :host {
            display:block;
        }        
    `,
    template: `<slot></slot>`,    
};
