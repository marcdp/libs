import XElement from "x-element";

// class
export default XElement.define("x-wizard-header", {
    style: `
        :host {display:block;}
        
        :host ul {display:flex; margin:0; justify-content:center; padding:1em;}
        :host ul li {list-style:none; display:block; align-items:center; width:7em; text-align:center; padding:.5em; position:relative; }
        
        :host ul li div.icon-container {height:1em;}

        :host ul li div.icon {width:.6em; height:.6em; text-align:center; border:.15em var(--x-color-xx-gray) solid; margin:0 auto; border-radius:50%; }
        :host ul li div.icon .number {font-size:var(--x-font-size-small);}

        :host ul li div.labels {display:block; position:relative}
        :host ul li div.labels span.label {margin-top:0.75em; display:block;}
        :host ul li div.labels span.message {font-size:var(--x-font-size-small); display:block;}
        :host ul li:before {
            content:"";
            border:.075em var(--x-color-xx-gray) solid; 
            top:0.85em;
            width:calc(50% - 1.25em);
            left:0;
            position:absolute;
        }
        :host ul li:after {
            content:"";
            border:.075em var(--x-color-xx-gray) solid; 
            top:0.85em;
            width:calc(50% - 1.25em);
            right:0;
            position:absolute;
        }
        :host ul li:first-child:before {display:none;}
        :host ul li:last-child:after {display:none;}
        
        :host ul li[visited] { cursor: pointer; }
        :host ul li[visited] div.icon {background:var(--x-color-text); border-color:var(--x-color-text)}
        :host ul li[visited]:hover div.icon {background:var(--x-color-primary); border-color:var(--x-color-primary)}
        :host ul li[visited]:hover span.label {color:var(--x-color-primary);}

        :host ul li[selected] div.icon {width:.4em; height:.4em; outline:.175em var(--x-color-primary) solid; outline-offset:.2em; color:var(--x-color-primary); background:var(--x-color-primary); border-color:var(--x-color-primary);}
        :host ul li[selected] span.label {font-weight:600; color:var(--x-color-primary);}



        :host(.vertical) {}
        :host(.vertical) ul {flex-direction:column; padding:0; padding-top:1em; padding-bottom:1em; padding-right:2em}
        :host(.vertical) ul li {display:flex; align-items:center; width:unset; min-height:4em; padding:0; }
        :host(.vertical) ul li div.icon-container { width:3em; }
        :host(.vertical) ul li div.icon {}
        :host(.vertical) ul li div.labels {text-align:left; align-self:start; margin-top:1.25em;}
        :host(.vertical) ul li div.labels span.label {white-space:nowrap; margin-top:0;}
        :host(.vertical) ul li:before {
            content:"";
            border:.075em var(--x-color-xx-gray) solid; 
            top:0;
            left:1.45em;
            height:calc(50% - 1.25em);
            width:0em;
            position:absolute;
        }
        :host(.vertical) ul li:after {
            content:"";
            border:.075em var(--x-color-xx-gray) solid; 
            left:1.45em;
            height:calc(50% - .85em);
            top:calc(50% + 1em);
            width:0;
            position:absolute;
        }

    `,
    state: {
        index: 0,
        panels: []
    },
    template: `        
        <ul>
            <li x-for="panel in state.panels" x-attr:selected="(state.index == panel.index - 1 ? true : false)" x-attr:visited="(panel.index - 1 <= state.index ? true : false)" x-on:click="click" x-attr:data-index="panel.index - 1">
                <div class="icon-container">
                    <div class="icon">
                        
                    </div>
                </div>
                <div class="labels">                
                    <span x-if="panel.label" class="label" x-text="panel.label"></span>
                    <span x-if="panel.message" class="message" x-text="panel.message"></span>
                </div>
            </li>
        </ul>
    `,
    //settings: {
    //    observedAttributes: ["index"],
    //},
    methods:{
        onCommand(command, args) {
            if (command == "load") {
                //load
                this.onCommand("refresh");

            } else if (command == "click") {
                //click
                let index = parseInt(args.event.currentTarget.dataset.index);
                if (index < this.state.index) {
                    this.dispatchEvent(new CustomEvent("index-set", {detail: {index: index}, bubbles: false, composed: false}));
                }

            } else if (command == "refresh") {
                //refresh
                let panels = [];
                this.querySelectorAll(":scope > *").forEach((panel, index) => {
                    panels.push({
                        label: panel.getAttribute("label"),
                        message: panel.getAttribute("message"),
                        icon: panel.getAttribute("icon") || "",
                        index: index + 1
                    });
                });
                this.state.panels = panels;
            }
        }
    }
});

