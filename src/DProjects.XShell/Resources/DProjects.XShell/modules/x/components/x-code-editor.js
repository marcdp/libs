// contract
export const contract = {
    description: "Provides an Ace code editor with configurable presentation and editing options.",
    events: {
        change: {
            description: "Raised when the editor value changes.",
            detail: {
                oldValue: {type:"string"},
                newValue: {type:"string"}
            }
        }
    },
    properties: {
        value:    {type:"string", default:"", attr:true, state:true, description:""},
        mode:     {type:"string", default:"", attr:true, state:true, description:""},
        theme:    {type:"string", default:"chrome", attr:true, state:true, description:""},
        wrap:     {type:"boolean", default:false, attr:true, state:true, description:""},
        readonly: {type:"boolean", default:false, attr:true, state:true, description:""},
        ready:    {type:"boolean", default:false, attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:flex; height:10em; flex-direction:column; align-items:center; justify-content:center;}
        :host x-lazy {width:100%; height:100%;}
        :host ace-editor {border-radius:var(--x-datafield-border-radius); flex:1; height:100%;}
        :host x-spinner + x-lazy {visibility:hidden; height:0}
    `,
    state: {
    },
    template: `
        <x-spinner x-if="!state.ready"></x-spinner>
        <x-lazy class="no-spinner">
            <ace-editor 
                class="editor"
                value-update-mode="start"

                x-on:blur="change"
                x-on:input="input"
                x-on:ready="ready"
                x-prop:value="state.value" 
                
                x-prop:wrap="state.wrap" 
                x-prop:readonly="state.readonly" 
                x-attr:mode="'ace/mode/' + state.mode"
                x-attr:theme="'ace/theme/' + state.theme"
            ></ace-editor>
        </x-lazy>
    `,
    controller({ state }) {
        return {
            load(params) {
                //load
            },

            ready(params) {
                //ready
                state.ready = true;
            },

            input(params) {
                //input
                console.log("input");
                clearTimeout(this._inputTimeoutId);
                this._inputTimeoutId = setTimeout(()=>{
                    this.onCommand("change");
                }, 500);
            },

            change(params) {
                //change
                console.log("change");
                clearTimeout(this._inputTimeoutId);
                let target = this.shadowRoot.querySelector(".editor");
                let oldValue = state.value;
                let newValue = target.value ?? "";
                state.value = newValue;
                this.dispatchEvent(new CustomEvent("change", {detail: {oldValue, newValue}, bubbles: true, composed: false}));
            },
            preRender() {
                debugger;
                if (this._renderCount > 0) {
                    let spinner = this.shadowRoot.querySelector("x-spinner");
                    let editor = this.shadowRoot.querySelector("ace-editor");
                    if (state.ready && spinner) this.shadowRoot.removeChild(spinner);
                    if (state.wrap) editor.wrap = true;
                    if (state.mode) editor.mode = "ace/mode/" + state.mode;
                    if (state.theme) editor.theme = "ace/theme/" + state.theme;
                    editor.value = this.value;
                    return true;
                }
            }
        }
    }    
};
