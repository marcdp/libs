
// contract
export const contract = {
    description: "Defines one tab panel for an x-tabs component.",
    events: {},
    properties: {
        label: {type:"string", default:"", attribute:true, state:true, description:""},
        hash:  {type:"string", default:"", attribute:true, state:true, description:""}
    },
    methods: {},
    slots: {
        "":{
            description: "Content for the tab panel"
        }
    }
};


// definition
export default {
    style: `
        :host {display:block;}
    `,
    template: `
        <slot></slot>
    `,
    state: {
    }
};
