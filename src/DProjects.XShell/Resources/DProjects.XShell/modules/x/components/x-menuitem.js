// contract
export const contract = {
    description: "Displays a menu item with optional nested menu content.",
    events: {},
    properties: {
        icon:        {type:"string", default:"", attribute:true, state:true, description:""},
        label:       {type:"string", default:"", attribute:true, state:true, description:""},
        href:        {type:"string", default:"", attribute:true, state:true, description:""},
        suffix:      {type:"string", default:"", attribute:true, state:true, description:""},
        command:     {type:"string", default:"", attribute:true, state:true, description:""},
        selected:    {type:"boolean", default:false, attribute:true, state:true, description:""},
        checked:     {type:"boolean", default:false, attribute:true, state:true, description:""},
        disabled:    {type:"boolean", default:false, attribute:true, state:true, description:""},
        embeded:     {type:"boolean", default:false, attribute:true, state:true, description:""},
        toooltip:    {type:"string", default:"", attribute:true, state:true, description:""},
    },
    methods: {},
    slots: {
        "": {
            description: "Default slot for the menu item content."
        }
    }
};


// implementation
export default {
    style: `
        :host {display:flex; position:relative; box-sizing:border-box; flex-direction:column}
        
        /* default */
        :host x-anchor {display:flex; flex:1; flex-direction:row; align-items:center; cursor:pointer; border-radius:var(--x-menuitem-border-radius); user-select: none; }
        :host x-anchor:hover {background:var(--x-color-background-gray); outline:var(--x-menuitem-border); }
        :host x-anchor x-icon {color:var(--x-color-text)!important; text-align:center;}
        :host x-anchor span.label {color:var(--x-color-text)!important; flex:1;  display:block; padding-top:.4em; padding-bottom:.4em; white-space:nowrap; }
        :host x-anchor span.suffix {color:var(--x-color-text-gray)!important; padding-left:1em;}
        :host x-anchor x-icon.has-childs {width:unset; margin-left:.25em;}
        :host x-anchor x-icon:first-child {}
        :host x-anchor x-icon:first-child:last-child {align-self:flex-end;}
        :host x-anchor x-icon + .label {padding-left:.35em;}
        :host x-anchor.selected {font-weight: 600;}
        :host x-anchor[expanded] {outline:var(--x-menuitem-border)}
        :host x-anchor[disabled] {pointer-events:none; cursor:default;}
        :host x-anchor[disabled] x-icon {color:var(--x-color-text-disabled)!important;}
        :host x-anchor[disabled] span.label {color:var(--x-color-text-disabled)!important; padding-left:0!important;}
        :host(.selected) x-anchor {background:var(--x-color-background-x-gray);}
        :host HR {border-top:var(--x-layout-main-border);; margin-top:.5em; margin-bottom:.5em; display:block; box-sizing: border-box; width:100%;}
        
        /* dropdown */
        x-contextmenu {
            width: var(--x-contextmenu-width);
            position:absolute; 
            left:100%;
            box-sizing:border-box;
            
            margin-left:0em;
            margin-top:-.2em;
        }
        x-contextmenu.down {top:calc(100% + .2em); left:-.1em;}
        x-contextmenu.left {left:100%!important; margin-left:.1em; transform:translateX(-100%);}

        /* inline */
        :host(.inline) x-anchor .has-childs {transform:rotate(90deg); transition: transform var(--x-transition-duration);}
        :host(.inline) x-anchor[expanded] .has-childs {transform:rotate(-90deg)}
        :host(.inline) x-contextmenu {
            position:unset;
            border:none;
            background:none;
            padding:0;
            width:unset;
            box-shadow:none;
            padding-left:2.5em;
        }
    `,
    state: {
        embeded: false,
        expanded: false,
        childsDown: false,
        childsLeft: false,
    },
    template: `
        <hr x-if="state.label=='-'" />
        <div x-elseif="state.embeded">
            <x-page x-attr:src="state.href" loading="lazy"></x-page>
        </div>
        <x-anchor x-else class="menuitem anchor" x-class:down="state.childsDown" x-attr:href="state.href" x-attr:command="state.command" x-attr:disabled="state.disabled" x-attr:expanded="state.expanded" x-class:selected="state.selected" x-attr:title="state.tooltip">
            <x-icon x-if="state.checked" icon="x-check"></x-icon>
            <x-icon x-if="!state.checked && state.icon" class="icon"x-attr:icon="state.icon"></x-icon>
            <span   x-if="state.label" class="label">{{ state.label }}</span>
            <span   x-if="state.suffix" class="suffix">{{ state.suffix }}</span>
            <x-icon x-if="state.hasChilds" class="has-childs" x-attr:icon="(state.childsDown ? 'x-keyboard-arrow-down' : 'x-keyboard-arrow-right')"></x-icon>
        </x-anchor>
        <x-contextmenu x-class:down="state.childsDown" x-class:left="state.childsLeft" x-if="state.hasChilds" x-show="state.expanded">
            <slot x-on:slotchange="refresh"></slot>
        </x-contextmenu>
    `,
    controller({ state, host }) {
        return {
            mount() {
                //load
                host.addEventListener("mouseenter", ()=>{
                    if (host.classList.contains("inline")) {
                    } else {
                        const horizontal = host.parentNode?.parentNode?.host?.classList.contains("horizontal");
                        if (horizontal) {
                        } else {
                            state.expanded = true;
                            this.refresh();
                        }
                    }
                });
                host.addEventListener("mouseleave", ()=>{
                    if (host.classList.contains("inline")) {
                    } else {
                        const horizontal = host.parentNode?.parentNode?.host?.classList.contains("horizontal");
                        if (horizontal) {
                        } else {
                            state.expanded = false;
                            this.refresh();
                        }
                    }
                });
                host.addEventListener("click", (event) => {
                    host.shadowRoot.querySelector("x-anchor")?.focus();
                    const horizontal = host.parentNode?.parentNode?.host?.classList.contains("horizontal");
                    if (host.classList.contains("inline") || horizontal) {
                        state.expanded = !state.expanded;
                        this.refresh();
                        event.preventDefault();
                        event.stopPropagation();
                    }
                });
                this.refresh();
            },

            refresh(args) {
                //refresh
                state.hasChilds = (host.firstElementChild != null);
                if (state.hasChilds) {
                    let rect = host.getBoundingClientRect();
                    // childsDown
                    const horizontal = host.parentNode?.parentNode?.host?.classList.contains("horizontal");
                    if (horizontal) {
                        state.childsDown = horizontal;
                    }
                    // childsLeft
                    let right = rect.left + rect.width * 2.5;
                    state.childsLeft = right > window.innerWidth;
                }
            }
        };
    }
};
