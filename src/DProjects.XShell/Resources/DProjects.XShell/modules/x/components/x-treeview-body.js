import XElement from "x-element";

// class
export default XElement.define("x-treeview-body", {
    style: `
        :host {
            display:block;
        }        
    `,
    template: `<slot></slot>`,    
});

