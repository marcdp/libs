import XElement from "x-element";

// class
export default XElement.define("x-wizard", {
    style: `
        :host {display:block;}

        :host .body {padding:1em;}
        :host .footer {text-align:right;}
        :host .footer x-button {
            min-width: var(--x-button-width-wide);
        }

        ::slotted(x-wizard-panel) {
            display:none;
        }
        ::slotted(x-button) {
            min-width: var(--x-button-width-wide);
        }

    `,
    state: {
        index: 0,
        panels: [],
        style: ""
    },
    template: `        

        <style x-html="state.style"></style>

        <x-wizard-header x-attr:index="state.index" x-on:index-set="set">
            <div x-for="panel in state.panels" 
                x-attr:label="panel.label"
                x-attr:message="panel.message"
                x-attr:icon="panel.icon"
            ></div>
        </x-wizard-header>
 
        <div class="body">
            <slot x-on:slotchange="refresh"></slot>
        </div>

        <div class="footer">
            <x-button x-if="state.index > 0" x-on:click="prev" label="Previous"></x-button>
            <x-button x-if="state.index < state.panels.length - 1" x-on:click="next" label="Next"></x-button>
            <slot name="buttons" x-if="state.index == state.panels.length - 1"></slot>
        </div>
    `,
    methods:{
        onCommand(command) {
            if (command == "load") {
                //load
                this.onCommand("refresh");

            } else if (command == "set") {
                //set
                let index = args.event.detail.index;
                this.state.index = index;
                this.onCommand("refresh");

            } else if (command == "prev") {
                //prev
                this.state.index--;
                this.onCommand("refresh");

            } else if (command == "next") {
                //next
                this.state.index++;
                this.onCommand("refresh");

            } else if (command == "refresh") {
                //refresh
                let panels = [];
                this.querySelectorAll(":scope > x-wizard-panel").forEach((panel, index) => {
                    panels.push({
                        label: panel.getAttribute("label"),
                        message: panel.getAttribute("message"),
                        icon: panel.getAttribute("icon") || "",
                        index: index + 1
                    });
                });
                this.state.panels = panels;
                this.state.style = `::slotted(x-wizard-panel:nth-child(${parseInt(this.state.index) + 1})) {display:block;}`;
            }
        }
    }
});

