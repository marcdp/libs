// contract
export const contract = {
    description: "Displays one item in a list view.",
    events: {},
    properties: {
        icon:        {type:"string", default:"", attribute:true, state:true, description:""},
        label:       {type:"string", default:"", attribute:true, state:true, description:""},
        breadcrumb:  {type:"boolean", default:false, attribute:true, state:true, description:""},
        description: {type:"string", default:"", attribute:true, state:true, description:""},
        category:    {type:"string", default:"", attribute:false, state:true, description:""},
        categoryVisible: {type:"boolean", default:false, attribute:false, state:true, description:""},
        href:        {type:"string", default:"", attribute:true, state:true, description:""},
        target:      {type:"string", default:"", attribute:true, state:true, description:""},
        open:        {type:"string", default:"", attribute:true, state:true, description:""},
        view:        {type:"string", default:"list", attribute:true, state:true, description:""}
    },
    methods: {},
    slots: {
        "": {
            description: "The default slot for list view item content."
        }
    }
};


// implementation
export default {
    style: `
        :host {display:block; position:relative}
        ::slotted(*) {display:var(--x-listview-item-display, table-cell); padding:.1em; padding-right:.5em;}
        ::slotted(.fill) {width:100%; max-width:0; overflow:hidden;}
        

        /* category */
        x-anchor[category]::before {content:attr(category); position:absolute; transform:translateY(-1.75em); font-weight:500; font-style:italic; 
            box-sizing:border;
            awidth:100%; 
            display:block;
            background:var(--x-color-background-page);
            left:0;
            right:0;
            padding-top:.125em;
            padding-bottom:.15em;
            padding-left:1.45em;
        }
        x-anchor[category='']:before {display:none;}
        x-anchor:not([category='']) {padding-top:1.75em;}       

        /* list */
        x-anchor.list {display:flex;} 
        x-anchor.list x-icon {margin-right:.25em;transform:translateY(-.1em);}
        x-anchor.list::part(a) { display:flex; align-items:center; }
        x-anchor.list {max-width:100%; text-overflow:ellipsis; white-space:nowrap; overflow:hidden;}
        x-anchor.list .description {display:none;}
        :host(.selected) x-anchor.list {font-weight:600;}
        :host(.selected) x-anchor.list x-anchor::part(a) {color:var(--x-color-primary);}
        
        /* icons */
        x-anchor.icons {display:flex; width:6em; height:6em; border-radius:.5em; padding-top:0;}
        x-anchor.icons x-icon {font-size:2em; amargin-bottom:.15em;}
        x-anchor.icons::part(a) { display:flex; flex-direction:column; align-items:center; padding-left:.25em; padding-right:.25em; box-sizing:border-box; justify-content: center; padding-bottom:.75em;}
        x-anchor.icons :focus { outline:.15em solid var(--x-color-text)!important;}
        x-anchor.icons::part(a):focus {outline:anone; }
        x-anchor.icons .label { text-align:center; display:block; width:100%; text-overflow:ellipsis; white-space:nowrap; overflow:hidden;}
        x-anchor.icons .description {display:none}
        x-anchor.icons[category]::before {display:none;}
        :host(.selected) x-anchor.icons {font-weight:600; outline:.15em solid var(--x-color-primary);}
        :host(.selected) x-anchor.icons::part(a) {color:var(--x-color-primary);}

        /* details */
        x-anchor.details {display:table-cell; padding-right:.5em;} 
        x-anchor.details x-icon {vertical-align:bottom; }
        x-anchor.details .description {color:var(--x-color-text-gray); max-width: 10em; text-overflow:ellipsis; white-space:nowrap; overflow:hidden;}
        x-anchor.details .description:empty {display:none;}
        x-anchor.details .description::before {content:"("; padding-left:.25em;}
        x-anchor.details .description::after {content:")";}
        
        /* tiles */
        x-anchor.tiles {display:block; width:var(--x-listview-item-tile-width, 17em); align-items:center; padding-top:0;}
        x-anchor.tiles x-icon {position:absolute;font-size:24px; padding:.1em; margin-left:.35em; margin-top:.1em;}   
        x-anchor.tiles span {display:block; margin-left:50px; }
        x-anchor.tiles .label {padding-top:.4em;}
        x-anchor.tiles .description {color:var(--x-color-text-gray); display:block; max-width: 100%; text-overflow:ellipsis; white-space:nowrap; overflow:hidden; padding-bottom:.4em; height:1.4em;}
        x-anchor.tiles:hover {background:var(--x-color-xxxxx-gray); border-radius:var(--x-card-border-radius);}
        x-anchor.tiles[category]::before {display:none;}

    `,
    template: `
        <x-anchor x-attr:href="state.href" x-attr:target="state.target" x-attr:class="'plain block ' + state.view" x-attr:title="state.description" x-prop:breadcrumb="state.breadcrumb" x-attr:open="state.open" x-attr:category="state.category">
            <x-icon x-attr:icon="state.icon || 'x-file'"></x-icon>
            <span class="label" x-text="state.label"></span>
            <span class="description" x-text="state.description"></span>
        </x-anchor>
        <slot x-if="state.view == 'details'"></slot>        
    `,
    state: {
        category: "",
    }
};

