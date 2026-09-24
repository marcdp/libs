// contract
export const contract = {
    description: "Shows editable component markup alongside its rendered result.",
    events: {},
    properties: {
        html: {type:"string", default:"", attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
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
    },
    controller({ state }) {
        return {
            load(args) {
                //debugger;
                state.html = this.innerHTML;
            },

            change(args) {
                //change
                let event = args.event;
                state.html = event.target.value;
            }
        };
    }
};

