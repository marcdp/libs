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
        .details > div {
            display:table;
            table-layout:auto;
            width:100%;
            white-space:nowrap;
        }
        .details > div ::slotted(*) {display:table-row;}
        
        .details > div ::slotted(*:not([name]):nth-child(even)) {background: var(--x-color-background-alt)} 
        .details > div .columns {display:table-row; position:sticky; top:0; background: var(--x-color-background-page);}
        .details > div .columns ::slotted(*) {display:table-cell; background: none!important; color:gray; padding-right:.25em;}
        .details > div .columns ::slotted(*:first-child) {padding-left:1.5em; border-box:border;}
        .details > div .columns ::slotted(x-datafield:first-child) {padding-left:0;}

        /* list */
        .list > div .columns {display:none;}
        .list > div {column-width: 15em; column-gap: 1rem;}

        /* tiles */
        .tiles > div .columns {display:none;}
        .tiles > div { display:flex; flex-wrap:wrap; }
        .tiles > div ::slotted(*) { }
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
    controller({ state, events, host }) {
        return {
            async load() {
                //load
                events.on(state, "change:view", "refresh");
            },
            async refresh() {
                const slot = host.shadowRoot.querySelector("slot:not([name])");
                if (!slot) return;

                const view = state.view;
                let lastElement = null;

                let prevCategory = null;
                // let indexes = {};
                // let css = "";
                slot.assignedElements().forEach((item, index) => {
                    let category = item.getAttribute("category");
                    item.category = (prevCategory != category ? category : "");
                    prevCategory = category;
                    // view
                    item.view = view;
                    // last element
                    lastElement = item;
                });

                // autoscroll to last element
                if (lastElement && state.autoScroll && host.checkVisibility()) {
                    setTimeout(() => {
                        lastElement.scrollIntoView({ block: "end", behavior: "smooth" });
                    }, 25);
                }
            }
        };
    }
};

