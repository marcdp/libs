import XElement from "x-element";

// class
export default XElement.define("x-pager", {
    style: `
        :host {
            display:flex;
            align-items: center;
            gap:.25em;
        }
        span.flex {flex:1;}
        span.total {font-weight:600;}
        
    `,
    template: `
        <span class="total" x-text="state.total"></span>
        <span class="label" x-text="state.label"></span>

        <span class="flex"></span>
        
        <span class="from">{{ (state.size * state.index) + 1 }}</span>
        <span>-</span>
        <span class="to">{{ state.size * (state.index + 1) }}</span>

        of

        <span class="total" x-text="state.total"></span>
        
        &nbsp;

        <x-button class="short plain prev" x-on:click="prev" icon="x-keyboard-arrow-left"   x-attr:disabled="state.index == 0"></x-button>
        <x-button class="short plain next" x-on:click="next" icon="x-keyboard-arrow-right"  x-attr:disabled="state.index == Math.floor(state.total/state.size) - 1"></x-button>
    `,
    state: {
        total: 0,
        index: 0,
        size: 20,
        label: "records"
    },
    //settings: {
    //    observedAttributes: ["total", "index", "size", "label"]
    //},
    methods:{
        onCommand(command, args) {
            if (command === "load") {
                //load
                

            } else if (command == "prev") {
                //prev
                this.dispatchEvent(new CustomEvent("change", {detail: {index: this.state.index - 1, size: this.state.size}}));

            } else if (command == "next") {
                //next
                this.dispatchEvent(new CustomEvent("change", {detail: {index: this.state.index + 1, size: this.state.size}}));
            }
        }
    }
});

