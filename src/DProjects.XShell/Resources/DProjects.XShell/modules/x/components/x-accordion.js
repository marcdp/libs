

// contract
export const contract = {
    description: "Groups accordion panels and keeps only the expanded panel active.",
    events: {},
    properties: {
        selectedIndex: {type:"number", default:0, attribute:true, state:true, description:""}
    },
    methods: {},
    slots: {
        "": {
            description: "The default slot for accordion panels."
        }
    },
    examples: [
        {
            name: "Basic",
            template: `<x-accordion></x-accordion>`
        },
    ]
};


// definition
export default {
    style: `
        :host {
            display:block; 
            border:var(--x-accordion-border); 
            border-radius:var(--x-accordion-border-radius); 
            box-shadow:var(--x-accordion-shadow); 
        }
    `,
    state: {
        tabs: []
    },
    template: `
        <slot></slot>
    `,
    controller({ host }) {
        return {
            load(params) {
                //load
                host.addEventListener("toggle", (event) => {
                    let target = event.target;
                    if (target.expanded) {
                        host.querySelectorAll(":scope > x-accordion-panel").forEach((panel) => {
                            if (panel != target) {
                                panel.expanded = false;
                                panel.dispatchEvent(new CustomEvent("toggle", {bubbles: true, composed: false}));
                            }
                        });
                    }
                    event.stopPropagation();
                    event.preventDefault();
                    return false;
                });
            }
        }
    }
}

