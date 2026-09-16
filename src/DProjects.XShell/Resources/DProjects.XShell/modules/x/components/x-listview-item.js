import XElement from "x-element";

// class
export default XElement.define("x-listview-item", {
    style: `
        :host {}

        /* list */
        x-anchor.list {display:flex;} 
        x-anchor.list x-icon {margin-right:.25em;transform:translateY(-.1em);}
        x-anchor.list::part(a) { display:flex; align-items:center; }
        x-anchor.list .description {color:var(--x-color-text-gray);}
        x-anchor.list .description:empty {display:none;}
        x-anchor.list .description::before {content:"("; padding-left:.25em;}
        x-anchor.list .description::after {content:")";}
        :host(.selected) x-anchor.list {font-weight:600;}
        :host(.selected) x-anchor.list x-anchor::part(a) {color:var(--x-color-primary);}
        .category {font-weight:600; padding-left: 1.5em; padding-top:.25em; width:100%;}
        

        /* icons */
        x-anchor.icons {display:flex; width:6em; height:6em; border-radius:.5em; }
        x-anchor.icons x-icon {font-size:2em; amargin-bottom:.15em;}
        x-anchor.icons::part(a) { display:flex; flex-direction:column; align-items:center; padding-left:.25em; padding-right:.25em; box-sizing:border-box; justify-content: center; padding-bottom:.75em;}
        x-anchor.icons :focus { outline:.15em solid var(--x-color-text)!important;}
        x-anchor.icons::part(a):focus {outline:anone; }
        x-anchor.icons .label { text-align:center; display:block; width:100%; text-overflow:ellipsis; white-space:nowrap; overflow:hidden;}
        x-anchor.icons .description {display:none}

        :host(.selected) x-anchor.icons {font-weight:600; outline:.15em solid var(--x-color-primary);}
        :host(.selected) x-anchor.icons::part(a) {color:var(--x-color-primary);}

        /* details */
        x-anchor.details {display:table-cell; padding-right:.5em;} 
        x-anchor.details x-icon {vertical-align:bottom; }
        ::slotted(*) {display:table-cell; padding:.1em; padding-right:.5em;}
        
    `,
    template: `
        <div x-if="state.category" class="category">
            {{state.category}}
        </div>
        <x-anchor x-attr:href="state.href" x-attr:target="state.target" x-attr:class="'plain ' + state.view" x-attr:title="state.description" x-prop:breadcrumb="state.breadcrumb" x-attr:open="state.open">
            <x-icon x-else x-attr:icon="state.icon || 'x-file'"></x-icon>
            <span class="label" x-text="state.label"></span>
            <span class="description" x-text="state.description"></span>
        </x-anchor>
        <slot></slot>
    `,
    state: {
        icon: "",
        label: "",
        breadcrumb: false,
        description: "",
        category: "",
        href: "",
        target: "",
        open: "",
        view: "list",
    },
    methods: {
        async onCommand(command) {
            if (command == "load") {
                //load                
            }
        }
    }
});

