//import xshell from "xshell";
//await xshell.loader.load(["component:x-treeview-body", "component:x-treeview-item"]);

// contract
export const contract = {
    description: "Provides keyboard navigation and selection for a tree of tree-view items.",
    events: {},
    properties: {
        multiple: {type:"boolean", default:false, attribute:true, state:true, description:""}
    },
    methods: {},
    slots: {
        "": {
            description: "Default slot for tree-view items."
        }
    }
};


// definition
export default {
    style: `
        :host {
            display:block; 
            --x-treeview-indent: 0;
        }        
        div:focus {outline:none;}
    `,
    template: `
        <div ref="div" tabindex="0">
            <slot x-on:slotchange="refresh" x-on:click="click"></slot>
        </div>
    `,
    state: {
    },
    controller({ state, events, host }) {
        let styleSheet = new CSSStyleSheet();
        return {
            async load(args) {
                //load
                host.addEventListener("keydown", (event) => {this.keydown({event});});
                host.addEventListener("toggle", (event) => {this.refresh({event});});
                events.on(state, "change:items", "build-items");
                //detect changes in light dom
                this.mutationObserver = new MutationObserver(()=>{
                    this.refresh();
                });
                this.mutationObserver.observe(host, {
                    childList: true, // Observe additions/removals of child nodes
                    attributes: true, // Observe attribute changes
                    subtree: true // Observe changes in child nodes' children
                });
            },

            mount() {
                host.shadowRoot.adoptedStyleSheets = [...host.shadowRoot.adoptedStyleSheets, styleSheet];
            },

            async unload(args) {
                //unload
                this.mutationObserver?.disconnect();
                delete this.mutationObserver;
            },

            async keydown(args) {
                let event = args.event;
                //get visible items
                let items = [];
                let getRecursive = (element) => {
                    for (let child of element.children) {
                        if (child.localName == "x-treeview-item") {
                            items.push(child);
                            if (!child.expanded) continue;
                        }
                        getRecursive(child);
                    }
                };
                getRecursive(host);
                //get selected items
                let selected = null;
                let selecteds = [];
                for(let target of items) {
                    if (target.selected) {
                        selecteds.push(target);
                        selected = target;
                    }
                }
                //process key
                let newSelecteds = null;
                if (event.key == "ArrowUp") {
                    let index = items.indexOf(selected);
                    if (index > 0) newSelecteds = [items[index-1]];
                    event.preventDefault();
                } else if (event.key == "ArrowDown") {
                    let index = items.indexOf(selected);
                    if (index < items.length - 1) newSelecteds = [items[index+1]];
                    event.preventDefault();
                } else if (event.key == "ArrowLeft") {
                    if (selected) {
                        if (selected.expanded) {
                            selected.expanded = false;
                        } else if (selected.parentElement && selected.parentElement.localName == "x-treeview-item") {
                            newSelecteds = [selected.parentElement];
                            selected.expanded = false;
                        }
                    }
                    event.preventDefault();
                } else if (event.key == "ArrowRight") {
                    if (selected) {
                        if (!selected.expanded) {
                            selected.expanded = true;
                            selected.dispatchEvent(new CustomEvent("toggle", {bubbles: true}));
                        } else {
                            let index = items.indexOf(selected);
                            if (index < items.length - 1) newSelecteds = [items[index+1]];
                        }
                    }
                    event.preventDefault();
                } else if (event.key == " ") {
                    if (selected) {
                        selected.expanded = !selected.expanded;
                        selected.dispatchEvent(new CustomEvent("toggle", {bubbles: true}));
                    }
                    event.preventDefault();
                } else if (event.key == "Enter") {
                    if (selected) {
                        selected.expanded = !selected.expanded;
                        selected.dispatchEvent(new CustomEvent("toggle", {bubbles: true}));
                    }
                    event.preventDefault();
                }
                //reselect
                if (newSelecteds) {
                    selecteds.forEach((item) => { item.selected = false; });
                    newSelecteds.forEach((item) => { item.selected = true; });
                }
            },

            async refresh(args) {
                //refresh
                let index = 0;
                //set index of items
                let setIndexRecursive = (element) => {
                    for (let child of element.children) {
                        if (child.localName == "x-treeview-item") {
                            child.index = index++;
                            if (!child.expanded) continue;
                        }
                        setIndexRecursive(child);
                    }
                };
                setIndexRecursive(host);
                //column widths
                let widths = [];
                host.querySelectorAll(":scope > x-treeview-head > x-treeview-column").forEach((column) => {
                    widths.push(column.getAttribute("width"));
                });
                let columnStyles = ":host {\n";
                for(let i = 0; i < widths.length; i++)   {
                    columnStyles += `    --x-treeview-column-width-${i+1}: ${widths[i]};\n`;
                }
                columnStyles += "}\n";
                styleSheet?.replaceSync(columnStyles);
            },

            async click(args) {
                //click
                let event = args.event;
                let item = event.target.closest("x-treeview-item");
                if (item) {
                    if (state.multiple) {
                        item.selected = !item.selected;
                    } else {
                        let items = host.querySelectorAll("x-treeview-item");
                        items.forEach((item) => {
                            item.selected = false;
                        });
                        item.selected = true;
                    }
                }
                //focus if required
                if (!document.activeElement || document.activeElement.localName == "body") {
                    host.shadowRoot.querySelector('[ref="div"]').focus();
                }
            }
        };
    }
};

