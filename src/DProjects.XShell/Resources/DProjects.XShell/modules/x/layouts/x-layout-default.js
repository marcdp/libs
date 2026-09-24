// contract
export const contract = {
    description: "Layout for a default page.",
    events: {},
    properties: {},
    methods: {},
    slots: {
        "": {
            description: "Default slot for the layout content."
        }
    }
};


// implementation
export default {
    meta: {
        renderEngine: "x",
        stateEngine: "proxy"
    },
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
};
