// contract
export const contract = {
    description: "Provides a container for tree-view item content.",
    events: {},
    properties: {},
    methods: {}
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
