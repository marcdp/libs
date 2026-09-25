// contract
export const contract = {
    description: "Displays wizard panels with previous and next navigation controls.",
    events: {},
    properties: {
        index:  {type:"number", default:0, attribute:true, state:true, description:""},
        panels: {type:"array", default:[], attribute:true, state:true, description:""},
        style:  {type:"string", default:"", attribute:true, state:true, description:""}
    },
    methods: {},
    slots: {
        "":{
            description: "Slot for providing wizard panels."
        },
        "buttons":{
            description: "Slot for providing buttons."
        }
    }
};


// definition
export default {
    style: `
        :host {display:block;}

        :host .body {padding:1em;}
        :host .footer {text-align:right;}
        :host .footer x-button {
            min-width: var(--x-button-width-wide);
        }

        ::slotted(x-wizard-panel) {
            display:none;
        }
        ::slotted(x-button) {
            min-width: var(--x-button-width-wide);
        }

    `,
    state: {
    },
    template: `        
        <x-wizard-header x-attr:index="state.index" x-on:index-set="set">
            <div x-for="panel in state.panels" 
                x-attr:label="panel.label"
                x-attr:message="panel.message"
                x-attr:icon="panel.icon"
            ></div>
        </x-wizard-header>
 
        <div class="body">
            <slot x-on:slotchange="refresh"></slot>
        </div>

        <div class="footer">
            <x-button x-if="state.index > 0" x-on:click="prev" label="Previous"></x-button>
            <x-button x-if="state.index < state.panels.length - 1" x-on:click="next" label="Next"></x-button>
            <slot name="buttons" x-if="state.index == state.panels.length - 1"></slot>
        </div>
    `,
    controller({ state, host }) {
        let styleSheet = new CSSStyleSheet();
        return {
            load(args) {
                //load
                this.refresh();
            },
            mounted(args) {
                //mounted
                host.shadowRoot.adoptedStyleSheets = [...host.shadowRoot.adoptedStyleSheets, styleSheet];
            },

            set(args) {
                //set
                let index = args.event.detail.index;
                state.index = index;
                this.refresh();
            },

            prev(args) {
                //prev
                state.index--;
                this.refresh();
            },

            next(args) {
                //next
                state.index++;
                this.refresh();
            },

            refresh(args) {
                //refresh
                let panels = [];
                host.querySelectorAll(":scope > x-wizard-panel").forEach((panel, index) => {
                    panels.push({
                        label: panel.getAttribute("label"),
                        message: panel.getAttribute("message"),
                        icon: panel.getAttribute("icon") || "",
                        index: index + 1
                    });
                });
                state.panels = panels;
                styleSheet.replaceSync(`::slotted(x-wizard-panel:nth-child(${parseInt(state.index) + 1})) {display:block;}`);
            }
        };
    }
};

