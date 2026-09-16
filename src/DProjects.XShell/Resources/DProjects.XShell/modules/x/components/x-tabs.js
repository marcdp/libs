
// class
export default {
    style: `
        :host {display:block; margin-bottom:1.5em;}
        
        nav {
            margin-bottom:1.5em; 
            border-bottom:var(--x-accordion-border); 
            display:flex;
            overflow-x:auto;
        }
        
        nav a {padding-left:1em; padding-right:1em; cursor:pointer; position:relative; line-height:2.5em; display:block; flex:unset; font-size:var(--x-font-size-tab); font-weight:600; white-space:nowrap; }
        
        nav a:after {content:""; border-radius:.1em; position:absolute; width:calc(100% - .25em); bottom:0; left:0; }

        nav a:hover {color:var(--x-color-primary);}
        nav a.selected {font-weight:600; color:var(--x-color-primary);}
        nav a.selected:after {font-weight:600; border:.125em var(--x-color-primary) solid;}

        ::slotted(x-tab) {display:none;}

        @media only screen and (max-width: 768px) {
            nav a {
                font-size:var(--x-font-size-text);
                padding-left:.75em; 
                padding-right:.75em;
            }
        }

    `,
    state: {
        selectedIndex: {value:0, attr:true},
        selectedHash:  {value:"", attr:true},
        tabs: {value:[]}
    },
    template: `
        <style x-html="state.style"></style>
        <nav>
            <a x-for="(tab,index) in state.tabs" 
                x-attr:tabindex="index+1"
                x-text="tab.label" 
                x-attr:icon="tab.icon" 
                x-class:plain="true"
                x-class:selected="index == state.selectedIndex"
                x-on:click="click"
                x-on:keydown.enter="click"
                ></a>
        </nav>        
        <slot x-on:slotchange="refresh"></slot>
    `, 
    script({ state, events }) {
        return {
            onCommand(command, params) {
                if (command == "load") {
                    //load
                    events.on(state, "change:selectedHash", (event) => {
                        let hash = event.newValue;
                        let tabs = this.querySelectorAll(":scope > x-tab");
                        let tab = Array.from(tabs).find(tab => tab.getAttribute("hash") == hash);
                        let tabIndex = Array.from(tabs).indexOf(tab);
                        if (tabIndex != -1) state.selectedIndex = tabIndex;
                    });
                    events.on(state, "change:selectedIndex", (event) => {
                        let selectedIndex = event.newValue;
                        let tabs = this.querySelectorAll(":scope > x-tab");
                        let tab = tabs[selectedIndex];
                        if (tab) state.selectedHash = tab.hash;
                    });
                    //hash
                    var hash = (this.src + "#").split("#")[1];
                    if (!hash) hash = state.selectedHash;
                    if (hash) {
                        state.selectedHash = "";
                        state.selectedHash = hash;
                    }

                } else if (command == "click") {
                    //click
                    let event = params.event;
                    let anchors = Array.from(this.shadowRoot.querySelectorAll("nav a"));
                    state.selectedIndex = anchors.indexOf(event.target);
                    this.onCommand("refresh");
                    event.preventDefault();
                    
                } else if (command == "refresh") {
                    //refresh
                    let tabs = [];
                    this.querySelectorAll(":scope > x-tab").forEach(tab => {
                        tabs.push({
                            label: tab.getAttribute("label"),
                            icon: tab.getAttribute("icon") || "",
                            hash: tab.getAttribute("hash") || ""
                        });
                    });
                    state.tabs = tabs;
                    state.style = `::slotted(x-tab:nth-child(${parseInt(state.selectedIndex) + 1})) {display:block;}`;
                    //hash
                    let tab = tabs[state.selectedIndex];
                    if (tab && tab.hash) {
                        this.page.replace("#" + tab.hash);
                    }
                    
                }
            }
        }
    }
};

