// contract
export const contract = {
    description: "Arranges list-view items and optionally scrolls to the latest item.",
    events: {},
    properties: {
        view:       {type:"string", default:"list", attribute:true, state:true, description:"Current list view."},
        views:      {type:"string", default:"", attribute:true, state:true, description:"Comma-separated list of available views (e.g., list, details, tiles, icons)."},
        autoScroll: {type:"boolean", default:false, attribute:true, state:true, description:"Whether to automatically scroll to the latest item."}
    },
    methods: {},
    slots: {
        "": {
            description: "The default slot for list items."
        },
        "column": {
            description: "The slot for column headers."
        }
    }
};


// implementation
export default {
    style: `
        :host {display:block; position:relative;}

        /* x-dropdown */
        x-dropdown {position:absolute; right:0; z-index:2;}
        
        /* views */
        .views {display:flex; justify-content:flex-end; gap:.2em; margin-bottom:.5em;}
        .views button {border:var(--x-datafield-border); border-radius:var(--x-datafield-border-radius); background:none; color:var(--x-color-gray); padding:.2em .6em; cursor:pointer; font:inherit;}
        .views button:hover {color:var(--x-color-text); background:var(--x-button-background-hover);}
        .views button.selected {color:var(--x-color-text); background:var(--x-color-background-x-gray);}

        /* list */
        .list > div {display:block; column-width:15em; column-gap:1rem;}
        .list > div .columns {display:none;}

        /* icons */
        .icons > div {display:flex; gap:.4em; flex-wrap:wrap;}
        .icons > div .columns {display:none;}

        /* details */
        .details > div {display:table; table-layout:auto; width:100%; white-space:nowrap;}
        .details > div ::slotted(*) {display:table-row;}
        .details > div ::slotted(*:not([name]):nth-child(even)) {background:var(--x-color-background-alt);}
        .details > div .columns {display:table-row; position:sticky; top:0; background:var(--x-color-background-page); z-index:1;}
        .details > div .columns ::slotted(*) {display:table-cell; background:none!important; color:var(--x-color-gray); padding-right:.25em;}
        .details > div .columns ::slotted(*:first-child) {padding-left:1.5em; border-box:border;}
        .details > div .columns ::slotted(x-datafield:first-child) {padding-left:0;}

        /* tiles */
        .tiles > div .columns {display:none;}
        .tiles > div {display:flex; flex-wrap:wrap; container:items / inline-size;}
        .tiles > div ::slotted(*) {--x-listview-item-tile-width:17em;}
        @container (width < 70em) {
            .tiles > div ::slotted(*) {width:33.33%; --x-listview-item-tile-width:100%;}
        }
        @container (width < 50em) {
            .tiles > div ::slotted(*) {width:50%; --x-listview-item-tile-width:100%;}
        }
        @container (width < 30em) {
            .tiles > div ::slotted(*) {width:100%; --x-listview-item-tile-width:100%;}
        }
    `,
    template: `
        <x-dropdown x-if="state.availableViews.length > 1" class="menu left">
            <x-button tabindex="1" x-attr:icon="'x-listview-' + state.view" class="size-x2 round light" tooltip="Change view"></x-button>
            <div slot="dropdown">
                <x-menuitem x-for="view in state.availableViews" 
                    x-attr:data-view="view"
                    x-class:selected="view == state.view"
                    x-attr:icon="'x-listview-' + view"
                    x-attr:label="view"
                    x-attr:title="view"
                    class="menuitem"
                    x-on:click="changeView"
                    ></x-menuitem>
            </div>
        </x-dropdown>

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
        availableViews: []
    },
    controller({ state, events, host }) {
        const supportedViews = ["list", "details", "tiles", "icons"];

        return {
            async load() {
                // initialize views and react to configuration changes
                events.on(state, "change:view", "refresh");
                events.on(state, "change:views", "refreshViews");
                this.refreshViews();
            },

            changeView(args) {
                // change the current presentation mode
                const view = args.event.currentTarget.dataset.view;
                if (state.availableViews.includes(view)) state.view = view;
            },

            refreshViews() {
                // parse and validate the configured available views
                const configuredViews = String(state.views || "")
                    .split(",")
                    .map(view => view.trim().toLowerCase())
                    .filter(view => supportedViews.includes(view));

                state.availableViews = [...new Set(configuredViews)];

                // keep the current view inside the configured set
                if (state.availableViews.length && !state.availableViews.includes(state.view)) state.view = state.availableViews[0];

                this.refresh();
            },

            async refresh() {
                // propagate the selected view to every list item
                const slot = host.shadowRoot.querySelector("slot:not([name])");
                if (!slot) return;

                const view = state.view;
                let lastElement = null;
                let prevCategory = null;

                slot.assignedElements().forEach(item => {
                    const category = item.getAttribute("category");
                    item.category = prevCategory != category ? category : "";
                    prevCategory = category;
                    item.view = view;
                    lastElement = item;
                });

                // scroll to the latest item when requested
                if (lastElement && state.autoScroll && host.checkVisibility()) {
                    setTimeout(() => {
                        lastElement.scrollIntoView({block:"end", behavior:"smooth"});
                    }, 25);
                }
            }
        };
    }
};
