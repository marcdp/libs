
// class
export default {
    style: `
        :host {display:block;}
    `,
    template: `
        <slot></slot>
    `,
    state: {
        label: {value:"", attr:true},
        hash:  {value:"", attr:true}
    }
};

