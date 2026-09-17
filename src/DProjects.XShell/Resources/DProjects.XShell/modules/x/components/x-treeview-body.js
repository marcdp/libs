import XElement from "x-element";

// declaration
export const declaration = {
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

