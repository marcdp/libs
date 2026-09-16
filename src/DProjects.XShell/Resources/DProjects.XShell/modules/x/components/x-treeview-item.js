import XElement from "x-element";

// class
export default XElement.define("x-treeview-item", {
    style: `
        :host {
            display:block;
        }

        :host(.error) .row {color:var(--x-color-error);}
        
        .row {display:flex;}
        .row .cell {
            white-space: nowrap;
            box-sizing:border-box;
            padding-left: calc(.5em + var(--x-treeview-indent) * 1.25em);
            width:var(--x-treeview-column-width-1); 
            min-width:var(--x-treeview-column-width-1); 
            display:flex;
            user-select: none;
        }
        .row .cell > x-icon {cursor:pointer; display:inline-block; transform:translateY(-.1em); }
        .row .cell x-anchor {flex:1; }
        .row .cell x-anchor x-icon {cursor:pointer; display:inline-block; transform:translateY(-.1em);}

        .row.selected {outline: .1em solid var(--x-color-primary); border-radius:.25em;}
        .row.no-childs .cell > x-icon {visibility:hidden;}
        .row.expanded .cell > x-icon {transform:rotate(90deg);}
        .row.even {background-color:var(--x-color-background-alt);}

        ::slotted(div[slot="column"]) {width:10em; white-space:nowrap; text-overflow:ellipsis; overflow:hidden; }
        ::slotted(div[slot="column"]:nth-child(1)) {width:var(--x-treeview-column-width-2); min-width:var(--x-treeview-column-width-2); }
        ::slotted(div[slot="column"]:nth-child(2)) {width:var(--x-treeview-column-width-3); min-width:var(--x-treeview-column-width-3); }
        ::slotted(div[slot="column"]:nth-child(3)) {width:var(--x-treeview-column-width-4); min-width:var(--x-treeview-column-width-4); }
        ::slotted(div[slot="column"]:nth-child(4)) {width:var(--x-treeview-column-width-5); min-width:var(--x-treeview-column-width-5); }
        ::slotted(div[slot="column"]:nth-child(5)) {width:var(--x-treeview-column-width-6); min-width:var(--x-treeview-column-width-6); }
        ::slotted(div[slot="column"]:nth-child(6)) {width:var(--x-treeview-column-width-7); min-width:var(--x-treeview-column-width-7); }
        ::slotted(div[slot="column"]:nth-child(7)) {width:var(--x-treeview-column-width-8); min-width:var(--x-treeview-column-width-8); }
        ::slotted(div[slot="column"]:nth-child(8)) {width:var(--x-treeview-column-width-9); min-width:var(--x-treeview-column-width-9); }
    `,
    template: `
        <div class="row" x-class:selected="state.selected" x-class:expanded="state.expanded" x-class:childs="state.hasChilds" x-class:no-childs="!state.hasChilds" x-class:even="(state.index % 2 == 0)">
            <div class="cell">
                <x-icon icon="x-keyboard-arrow-right" x-on:click="toggle"></x-icon>
                <x-anchor x-attr:href="state.href ? state.href : false" x-attr:target="state.target" x-attr:title="state.description" x-on:dblclick="toggle">
                    <x-icon x-attr:icon="state.icon"></x-icon>
                    <span class="label" x-text="state.label"></span>
                    <span class="description" x-text="state.description"></span>
                </x-anchor>
            </div>
            <slot name="column"></slot>            
        </div>
        <div class="children" x-if="state.expanded">
            <style x-html="':host .children {--x-treeview-indent:' + (state.indent + 1) + '}'"></style>
            <slot></slot>
        </div>
    `,
    state: {
        indent: 0,
        icon: "x-file",
        label: "",
        description: "",
        href: "",
        target: "",
        hasChilds: false,
        expanded: false,
        selected: false,
        index: 0
    },
    methods: {
        async onCommand(command) {
            if (command == "load") {
                //load                
                this.onCommand("refresh");
    
            } else if (command == "toggle") {
                //toggle
                if (this.state.expanded) {
                    this.onCommand("collapse");
                } else {
                    this.onCommand("expand");
                }

            } else if (command == "expand") {
                //expand
                if (this.state.hasChilds) {
                    this.state.expanded = true;
                    this.dispatchEvent(new CustomEvent("toggle", {bubbles: true}));    
                }

            } else if (command == "collapse") {
                //collapse
                if (this.state.expanded) {
                    this.state.expanded = false;
                    this.dispatchEvent(new CustomEvent("toggle", {bubbles: true}));    
                }

            } else if (command == "refresh") {
                //refresh
                this.state.hasChilds = (this.querySelectorAll(':scope > :not([slot])').length > 0);
                //indent
                let element = this;
                let indent = 0;
                while (element && element.localName !== "x-treeview") {
                    element = element.parentElement; // Move directly to the parent
                    if (element.localName == "x-treeview-item") indent++;
                }
                this.state.indent = indent;
            }
        }
    }
});

