import XElement from "x-element";

// class
export default XElement.define("x-layout-default", {
    style:`
        :host {
            display:block;
            padding: var(--x-layout-default-page-padding-vertical) var(--x-layout-default-page-padding-horizontal) var(--x-layout-default-page-padding-vertical) var(--x-layout-default-page-padding-horizontal);
        }
    `,
    template: `
        <x-loading x-if="state.status=='loading'"></x-loading>
        <slot></slot>
    `,
    state: {
        status: ""
    },    
});
