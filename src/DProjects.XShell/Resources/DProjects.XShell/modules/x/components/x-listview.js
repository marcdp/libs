import XElement from "x-element";

// class
export default XElement.define("x-listview", {
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
        view: "list",
        autoScroll: false
    },
    methods: {
        async onCommand(command) {
            if (command == "load") {
                //load               
                this.bindEvent(this.state, "change:view", "refresh");

            } else if (command == "refresh") {
                //slotchange
                let view = this.state.view;
                let lastElement = null;
                this.shadowRoot.querySelector("slot:not([name])").assignedElements().forEach((item) => {
                    item.view = view;
                    lastElement = item;
                });
                if (lastElement && this.state.autoScroll && this.checkVisibility()) {
                    lastElement.scrollIntoView({ block: "end", behavior: "smooth" });
                }
            }
        }
    }
});

