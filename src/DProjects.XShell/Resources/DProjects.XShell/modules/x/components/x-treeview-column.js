import XElement from "x-element";


// contract
export const contract = {
    description: "Defines a labeled column in a tree view.",
    events: {},
    properties: {
        label: {type:"string", default:"", attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default XElement.define("x-treeview-column", {
    style: `
        :host {
            display:block; 
            width: var(--x-treeview-column-width);
            box-sizing:border-box;
        }
    `,
    template: `
        {{state.label}}
    `,
    state: {
        label: ""        
    }
});

