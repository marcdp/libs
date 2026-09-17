
// declaration
export const declaration = {
    description: "Defines one tab panel for an x-tabs component.",
    events: {},
    properties: {
        label: {type:"string", default:"", attr:true, state:true, description:""},
        hash:  {type:"string", default:"", attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
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

