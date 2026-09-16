import XElement from "x-element";

// class
export default XElement.define("x-wizard-panel", {
    style: `
        :host {display:block;}
    `,
    template: `
        <slot></slot>
    `
});

