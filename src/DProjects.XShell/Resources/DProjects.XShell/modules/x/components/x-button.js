
// contract
export const contract = {
    description: "Renders a button or navigation link and can emit a command.",
    events: {
        command: {
            description: "Raised when a configured button command is invoked.",
            detail: {
                command: {type:"string"},
                data: {type:"object"}
            }
        }
    },
    properties: {
        icon:      {type:"string", default:"", attribute:true, state:true, description:""},
        label:     {type:"string", default:"", attribute:true, state:true, description:""},
        message:   {type:"string", default:"", attribute:true, state:true, description:""},
        command:   {type:"string", default:"", attribute:true, state:true, description:""},
        href:      {type:"string", default:"", attribute:true, state:true, description:""},
        autofocus: {type:"boolean", default:false, attribute:true, state:true, description:""}
    },
    methods: {},
    slots: {
        "": {
            description: "Default slot for the button content."
        }
    } 
};


// implementation
export default {
    style: `
        :host {position:relative; display:inline-flex;}
        :host > div {display:flex; flex:1;  }
        :host .button {
            border: var(--x-button-border); 
            padding: var(--x-button-padding); 
            line-height: var(--x-button-line-height);
            border-radius: var(--x-button-border-radius);
            user-select: none; 
            display:inline-flex; 
            position:relative; 
            align-items:baseline; 
            box-sizing: border-box;
            flex:1;
            justify-content: var(--x-button-justify-content);
            text-decoration:none;            
            color:var(--x-color-primary);
        }
        :host .button:hover {border-color:var(--x-color-text); color:var(--x-color-text); background:var(--x-button-background-hover); cursor:pointer; }
        :host .button:active {background:var(--x-button-background-active);}
        :host .button > x-icon.icon {transform:translateY(-.1em); apadding-left:.15em; }
        :host .button > x-icon.icon[icon] + div {padding-left:.15em;}
        :host .button > x-icon.down {transform:translateY(-.1em); padding-left:.15em; margin-right:-.25em; }
        :host .button[expanded] > x-icon.down {transform:translateY(.1em) rotate(-180deg) ; }
        :host .button > div {display:flex; flex-direction:column; }
        :host .button > div span.label {white-space:nowrap; font-weight: 700;}
        :host .button > div span.message {font-size:var(--x-font-size-small);}
        :host .button[has-more] { border-radius:var(--x-button-border-radius) 0 0 var(--x-button-border-radius); border-right:none;}
        :host .button[expanded] {background:var(--x-button-background-hover)!important;}
        
        :host([disabled])  {pointer-events:none;}
        :host([disabled]) .button {none; border-color:var(--x-color-xx-gray); color:var(--x-color-xx-gray);}

        :host div.more {width:1.75em; border:var(--x-button-border); border-radius:0 var(--x-button-border-radius) var(--x-button-border-radius) 0; display:flex; align-items:center; color:var(--x-color-primary); justify-content:center;}
        :host div.more x-icon {margin-right:.1em; }
        :host div.more:hover {border-color:var(--x-color-text); color:var(--x-color-text); background:var(--x-button-background-hover); cursor:pointer; }
        :host div.more:active {background: var(--x-button-background-active);}
        :host div.more[expanded] {background:var(--x-button-background-hover)!important;}
        :host div.more[expanded] x-icon {transform:  rotate(-180deg) ; }

        :host(.submit) .button {background:var(--x-color-secondary); border-color:var(--x-color-secondary); color:var(--x-color-text); }
        :host(.submit) .button x-icon {}
        :host(.submit) .button:hover {background:var(--x-color-secondary-dark); border-color:var(--x-color-secondary-dark);}
        :host(.submit) .button:active {background:var(--x-color-secondary-x-dark); border-color:var(--x-color-secondary-x-dark);}
        :host(.submit[disabled]) .button {background:var(--x-color-xxxxx-gray); border-color:var(--x-color-xxxxx-gray); color:var(--x-color-x-gray);}
        :host(.submit) div.more {background:var(--x-color-secondary); border-color:var(--x-color-secondary); color:var(--x-color-text); margin-left:.1em;}
        :host(.submit) div.more:hover {background:var(--x-color-secondary-dark); border-color:var(--x-color-secondary-dark);}
        :host(.submit) div.more:active {background:var(--x-color-secondary-x-dark);  border-color:var(--x-color-secondary-x-dark);}
        :host(.submit) div.more[expanded] {background:var(--x-color-secondary-dark)!important; border-color:var(--x-color-secondary-dark)!important;}

        :host(.cancel) .button {background:none; border-color:transparent; color:var(--x-color-primary); }
        :host(.cancel) .button:hover {color:var(--x-color-text); background:var(--x-button-background-hover); cursor:pointer; }
        :host(.cancel) .button:active {background:var(--x-button-background-active);}
        :host(.cancel[disabled]) .button {color:var(--x-color-xxx-gray)}

        :host(.anchor) {display:inline;}
        :host(.anchor) > div {display:inline;}
        :host(.anchor) .button {padding:unset; border:none; color:var(--x-color-text);}
        :host(.anchor) .button > div span.label {font-weight: normal; text-decoration:underline;}
        :host(.anchor) .button:hover {background: none; color:var(--x-color-primary)}
        :host(.anchor) .button:active {background: none; color:var(--x-color-primary-dark)}
        :host(.anchor) div.more {border:none;}
        :host(.anchor.big) .button {padding:.25em .25em;}
        :host(.anchor.big) .button x-icon {font-size:1.1em;}
        :host(.anchor[disabled]) .button {color:var(--x-color-xxx-gray)}
        
        :host(.round) .button {padding-left:0; padding-right:0; width:100%; width:2.5em; justify-content: center;}

        :host(.light) .button {background:var(--x-color-background-gray); border-color:var(--x-color-background-gray); color:var(--x-color-text); }
        :host(.light) .button > div span.label {font-weight:500;}
        :host(.light) .button:hover {background:var(--x-color-background-x-gray); border-color:var(--x-color-background-x-gray); }
        :host(.light) .button:active {background:var(--x-color-background-xx-gray); border-color:var(--x-color-background-xx-gray); }
        
        :host(.plain) .button {border-radius: var(--x-datafield-border-radius);border: var(--x-datafield-border); padding:0 0 0 0; line-height:unset;width:100%; width:2em; color:gray;align-items:center;justify-content: center; }
        :host(.plain) .button > div span.label {font-weight: normal; color:var(--x-color-gray);}
        :host(.plain.selected) .button {background:var(--x-color-background-x-gray);}        
        :host(.plain[disabled]) .button {color:var(--x-color-xxx-gray); border-color:var(--x-color-xxx-gray); pointer-events:none;}
        :host(.plain.more) .button {border-radius:0 var(--x-datafield-border-radius) var(--x-datafield-border-radius) 0;}
        

        ::slotted(x-contextmenu) {
            left:0; 
            top:calc(100% + .1em); 
        }
        
        :host(.icon-big) .button {padding:.25em .5em;}
        :host(.icon-big) .button x-icon {font-size:1.1em;}
    `,
    template: ` 
        <div>
            <a class="button" tabindex="1" x-attr:href="state.realHref" x-attr:expanded="(state.expanded && !state.command ? true : false)" x-on:click="command" x-attr:has-more="(state.childs && state.command ? true : false)" x-on:keydown.enter="command">
                <x-icon class="icon" x-if="state.icon" x-attr:icon="state.icon"></x-icon>
                <div x-if="state.label">
                    <span class="label"   x-if="state.label" x-html="state.label"></span>
                    <span class="message" x-if="state.message" x-text="state.message"></span>
                </div>
                <x-icon class="down" x-if="state.childs && !state.command" icon="x-arrow-down-fill"></x-icon>
            </a>
            <div class="more" tabindex="2" x-if="state.childs && state.command" x-on:click="expand" x-attr:expanded="state.expanded" x-on:keydown.enter="toggle">
                <x-icon class="independent" icon="x-arrow-down-fill"></x-icon>
            </div>
        </div>
        <div x-class:right="state.forceRight" x-if="state.childs" x-show="state.expanded" x-attr:expanded="state.expanded">
            <slot x-on:slotchange="refresh"></slot>
        </div>
    `,
    state: {
        realHref: null,
        breadcrumb: false,
        childs: false,
        expanded: false,
        forceRight: ""
    },
    controller({ state, events, navigation, getPage }) {
        return {
            load(args) {
                // load
                events.on(state, "change:href", "refresh");
                this.onCommand("refresh");
            },

            mount(args) {
                // mount
                if (state.autofocus) {
                    requestAnimationFrame(() => {
                        let focusable = this.shadowRoot.querySelector("a.button");
                        if (focusable) {
                            focusable.focus();
                        }
                    });
                }
            },

            command(args) {
                // command
                let handled = false;
                if (state.command) {
                    this.dispatchEvent(new CustomEvent("command", {detail: {command: state.command, data: this.dataset}, bubbles: true, composed: false}));
                    handled = true;
                } else if (state.childs) {
                    if (state.expanded) {
                        this.onCommand("collapse");
                    } else {
                        this.onCommand("expand");
                    }
                    handled = true;
                }
                //cancel propagation
                if (handled) {
                    let event = args.event;
                    if (event) {
                        event.preventDefault();
                        event.stopPropagation();
                    }
                }
            },

            expand(args) {
                // expand
                state.expanded = true;
                // bind event
                if (!this.onDocumentClick) {
                    this.onDocumentClick = function() { this.onCommand("collapse"); }.bind(this);
                }
                document.addEventListener("click", this.onDocumentClick, true);
            },

            collapse(args) {
                // collapse
                state.expanded = false;
                // unbind event
                document.removeEventListener("click", this.onDocumentClick, true);
                delete this.onDocumentClick;
            },

            toggle(args) {
                //toggle
                if (state.expanded) {
                    this.onCommand("collapse");
                } else {
                    this.onCommand("expand");
                }
            },

            refresh(args) {
                // refresh
                state.childs = (this.firstElementChild != null);
                // childsClass
                if (state.childs) {
                    let rect = this.getBoundingClientRect();
                    if (window.innerWidth - rect.right < 100) {
                        state.forceRight = true;
                    }
                }
                // href
                if (state.href) {
                    state.realHref = navigation.getHref(state.href, getPage(), { breadcrumb: state.breadcrumb });
                } else {
                    state.realHref = null;
                }
            }
        }
    }
};

