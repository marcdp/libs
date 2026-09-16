import XElement from "x-element";

// class
export default XElement.define("x-treeview", {
    style: `
        :host {
            display:block; 
            --x-treeview-indent: 0;
        }        
        div:focus {outline:none;}
    `,
    template: `
        <style x-html="state.columnStyles"></style>
        <div ref="div" tabindex="0">
            <slot x-on:slotchange="refresh" x-on:click="click"></slot>
        </div>
    `,
    state: {
        multiple: false
    },
    settings:{
        preload: ["component:x-treeview-body", "component:x-treeview-item"]
    },
    methods: {
        async onCommand(command, args) {
            if (command == "load") {
                //load               
                this.addEventListener("keydown", (event) => {this.onCommand("keydown", {event});});
                this.addEventListener("toggle", (event) => {this.onCommand("refresh", {event});});
                this.bindEvent(this.state, "change:items", "build-items");
                //detect changes in light dom
                this.mutationObserver = new MutationObserver(()=>{
                    this.onCommand("refresh");
                });
                this.mutationObserver.observe(this, {
                    childList: true, // Observe additions/removals of child nodes
                    attributes: true, // Observe attribute changes
                    subtree: true // Observe changes in child nodes' children
                });

            } else if (command == "unload") {
                //unload                
                delete this.mutationObserver;

            } else if (command == "keydown") {
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
                getRecursive(this);
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
                            selected.onCommand("expand");
                        } else {
                            let index = items.indexOf(selected);
                            if (index < items.length - 1) newSelecteds = [items[index+1]];
                        }
                    }
                    event.preventDefault();
                } else if (event.key == " ") {
                    if (selected) {
                        selected.onCommand("toggle");
                    }
                    event.preventDefault();
                } else if (event.key == "Enter") {
                    if (selected) {
                        selected.onCommand("toggle");
                    }
                    event.preventDefault();
                }
                //reselect
                if (newSelecteds) {
                    selecteds.forEach((item) => { item.selected = false; });
                    newSelecteds.forEach((item) => { item.selected = true; });
                }

            } else if (command == "refresh") {
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
                setIndexRecursive(this);
                //column widths
                let widths = [];
                this.querySelectorAll(":scope > x-treeview-head > x-treeview-column").forEach((column) => {
                    widths.push(column.getAttribute("width"));
                });
                let columnStyles = ":host {\n";
                for(let i = 0; i < widths.length; i++)   {
                    columnStyles += `    --x-treeview-column-width-${i+1}: ${widths[i]};\n`;
                }
                columnStyles += "}\n";
                this.state.columnStyles = columnStyles;

            } else if (command == "click") {
                //click
                let event = args.event;
                let item = event.target.closest("x-treeview-item");
                if (item) {
                    if (this.state.multiple) {
                        item.state.selected = !item.state.selected;
                    } else {
                        let items = this.querySelectorAll("x-treeview-item");
                        items.forEach((item) => {
                            item.state.selected = false;
                        });
                        item.state.selected = true;
                    }
                }
                //focus if required
                if (!document.activeElement || document.activeElement.localName == "body") {
                    this.refs.div.focus();
                }
            }
        }
    }
});

