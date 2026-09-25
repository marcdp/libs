// contract
export const contract = {
    description: "Arranges list-view items and optionally scrolls to the latest item.",
    events: {},
    properties: {
        view:       {type:"string", default:"list", attribute:true, state:true, description:""},
        autoScroll: {type:"boolean", default:false, attribute:true, state:true, description:""}
    },
    methods: {},
    slots: {
        "": {
            description: "The default slot for data fields."
        },
        "column": {
            description: "The slot for column headers."
        }
    }
};


// implementation
export default {
    style: `
        :host {display:block;}

        /* list */
        .list > div {display:block;}
        .list > div .columns {display:none;}

        /* icons */
        .icons > div {display:flex;gap:.4em;flex-wrap:wrap;}
        .icons > div .columns {display:none;}

        /* details */
        .details {}
        .details > div {display:table; width:100%; white-space: nowrap; }
        .details > div ::slotted(*) {display:table-row;}
        
        .details > div ::slotted(*:not([name]):nth-child(even)) {background: var(--x-color-background-alt)} 
        .details > div .columns {display:table-row; position:sticky; top:0; background: var(--x-color-background-page);}
        .details > div .columns ::slotted(*) {display:table-cell; background: none!important; color:gray; padding-right:.25em;}
        .details > div .columns ::slotted(*:first-child) {padding-left:1.4em; }

    `,
    template: `
        <div x-attr:class="state.view">
            <div>
                <div class="columns">
                    <slot name="column"></slot>
                </div>
                <slot x-on:slotchange="refresh"></slot>
            </div>
        </div>        
    `,
    state: {
    },
    controller({ state, events }) {
        return {
            async load() {
                //load
                events.on(state, "change:view", "refresh");
            },
            async refresh() {
                const slot = this.shadowRoot.querySelector("slot:not([name])");
                if (!slot) return;

                const view = state.view;
                let lastElement = null;

                slot.assignedElements().forEach((item) => {
                    item.view = view;
                    lastElement = item;
                });

                if (lastElement && state.autoScroll && this.checkVisibility()) {
                    lastElement.scrollIntoView({ block: "end", behavior: "smooth" });
                }
            }
        };
    }
};

