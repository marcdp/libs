import XElement from "x-element";

// class
export default XElement.define("x-treeview-head", {
    style: `
        :host {
            display:flex; 
        }
        ::slotted(x-treeview-column[slot="column"]) {width:10em;}
        ::slotted(x-treeview-column:nth-child(1)) {width:var(--x-treeview-column-width-1); }
        ::slotted(x-treeview-column:nth-child(2)) {width:var(--x-treeview-column-width-2); }
        ::slotted(x-treeview-column:nth-child(3)) {width:var(--x-treeview-column-width-3); }
        ::slotted(x-treeview-column:nth-child(4)) {width:var(--x-treeview-column-width-4); }
        ::slotted(x-treeview-column:nth-child(5)) {width:var(--x-treeview-column-width-5); }
        ::slotted(x-treeview-column:nth-child(6)) {width:var(--x-treeview-column-width-6); }
        ::slotted(x-treeview-column:nth-child(7)) {width:var(--x-treeview-column-width-7); }
        ::slotted(x-treeview-column:nth-child(8)) {width:var(--x-treeview-column-width-8); }
        ::slotted(x-treeview-column:nth-child(9)) {width:var(--x-treeview-column-width-9); }
    `,
    template: `<slot></slot>`,    
});

