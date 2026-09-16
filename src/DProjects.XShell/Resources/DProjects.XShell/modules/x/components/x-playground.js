import XElement from "x-element";

// class
export default XElement.define("x-playground", {
    style: `
        :host {
            display:flex; 
            margin-top:1em;
            margin-bottom:1em;
            border: var(--x-datafield-border);
            border-radius: var(--x-datafield-border-radius);
        }
        :host > div {
            flex:1;
        }
        :host > div x-code-editor {
            height:100%;
        }
        :host > div.result {
            padding:1em;
        }
        :host(.no-border) {
            border:none;
            border-radius:0;
        }
        :host(.no-margin) {
            margin:0;
        }
    `,
    template: `
        <div class="code">
            <x-code-editor mode="html" x-attr:value="state.html" x-on:change="change"></x-code-editor>
        </div>
        <x-splitter></x-splitter>
        <div class="result" x-html="state.html"></div>
    `,
    state: {
        html:""

    },
    methods:{
        onCommand(command, args) {
            if (command == "load") {
                //debugger;
                this.state.html = this.innerHTML;

            } else if (command == "change") {
                //change
                let event = args.event;
                this.state.html = event.target.value;
            }
        }
    }
});

