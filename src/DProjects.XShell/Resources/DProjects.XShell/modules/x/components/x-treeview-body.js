import XElement from "x-element";

// contract
export const contract = {
    description: "Provides a container for tree-view item content.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default XElement.define("x-treeview-body", {
    style: `
        :host {
            display:block;
        }        
    `,
    template: `<slot></slot>`,    
});

