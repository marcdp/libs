

// contract
export const contract = {
    description: "Groups accordion panels and keeps only the expanded panel active.",
    events: {},
    properties: {
        selectedIndex: {type:"number", default:0, attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
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
        selectedIndex: {value:0, attr:true},
        tabs: {value:[]}
    },
    template: `
        <slot></slot>
    `,
    script({}) {
        return {
            onCommand(command, params){
                if (command == "load") {
                    //load
                    this.addEventListener("toggle", (event) => {
                        let target = event.target;
                        if (target.expanded) {
                            this.querySelectorAll(":scope > x-accordion-panel").forEach((panel) => {
                                if (panel != target) {
                                    panel.onCommand("collapse");
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
}

