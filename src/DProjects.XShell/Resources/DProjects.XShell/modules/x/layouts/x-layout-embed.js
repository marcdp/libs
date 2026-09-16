import XElement from "x-element";

// class
export default XElement.define("x-layout-embed", {
    style: `
        :host {
            display:block;
            padding: var(--x-layout-embed-page-padding-vertical) var(--x-layout-embed-page-padding-horizontal) var(--x-layout-embed-page-padding-vertical) var(--x-layout-embed-page-padding-horizontal);
        }
        ::slotted(p:first-child) {
            margin-top:0;
        }
        ::slotted(p:last-child) {
            margin-bottom:0;
        }        
        x-loading {
            width:var(--x-loading-width);
            left:50%;
            transform:translateX(-50%);
            z-index:10;
        }
    `,
    template: `
        <x-loading x-if="state.status=='loading'"></x-loading>
        <slot></slot>
    `,
    state: {
        status: ""
    }
});
