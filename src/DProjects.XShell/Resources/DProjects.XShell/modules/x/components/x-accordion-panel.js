
// class
export default {
    style: `
        :host {display:block;}
        .header {display:flex; height:2.5em; align-items:center; padding:0 .5em 0 1em; cursor:pointer; user-select: none;}
        .header > x-icon {margin-right:.5em; transition: transform var(--x-transition-duration); }
        .header > span {flex:1; font-weight:600;}
        .header > .toolbar {margin-right:1em; }
        .header:hover > span {color:var(--x-color-primary)!important; }
        .header:hover > x-icon {color:var(--x-color-primary)!important;}
        .header[expanded] span {color:var(--x-color-primary);}
        .header[expanded] x-icon:last-child {transform:rotate(-180deg); }
        
        .body {
            display: grid; 
            grid-template-rows: 0fr;
            transition: var(--x-transition-duration) grid-template-rows ease;
            border-bottom:var(--x-accordion-border); 
            border-bottom-width:1px;
        }
        .body > div {overflow: hidden;}
        .body > div > div {padding:1em;}
        .body[expanded] {grid-template-rows: 1fr;}

        :host(:last-child) .body {border-bottom:none;}
    `,
    template: `
        <div class="header" x-on:click="toggle" x-attr:expanded="state.expanded">
            <x-icon x-if="state.icon" x-attr:icon="state.icon"></x-icon>
            <span x-text="state.label"></span>
            <div class="toolbar">
                <slot name="toolbar"></slot>
            </div>
            <x-icon icon="x-keyboard-arrow-down"></x-icon>
        </div>
        <div class="body" x-attr:expanded="state.expanded">
            <div>
                <div>
                    <slot></slot>
                </div>
            </div>
        </div>
    `,
    state: {
        label: {value:"", attr:true},
        icon:  {value:"", attr:true},
        expanded: {value:false, attr:true},
    },
    script({ state, navigation, getPage }) {
        return {
            onCommand(command, params){
                if (command == "load") {
                    //load
                    this.onCommand("refresh");

                } else if (command == "collapse") {
                    //collapse
                    if (state.expanded) {
                        this.onCommand("toggle");
                    }

                } else if (command == "toggle") {
                    //toggle
                    state.expanded = !state.expanded;
                    this.dispatchEvent(new CustomEvent("toggle", {bubbles: true, composed: false}));
                }
            }
        }
    }
};

