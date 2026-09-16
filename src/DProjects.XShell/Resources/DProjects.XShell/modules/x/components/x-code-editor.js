import XElement from "x-element";

// class
export default {
    style: `
        :host {display:flex; height:10em; flex-direction:column; align-items:center; justify-content:center;}
        :host x-lazy {width:100%; height:100%;}
        :host ace-editor {border-radius:var(--x-datafield-border-radius); flex:1; height:100%;}
        :host x-spinner + x-lazy {visibility:hidden; height:0}
    `,
    state: {
        value: {value:"", attr:true},
        mode:  {value:"", attr:true},
        theme: {value:"chrome", attr:true},
        wrap:  {value:false, attr:true},
        readonly: {value:false, attr:true},
        ready: {value:false, attr:true},
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
    script({ state }) {
        return {
            onCommand(command, params){
                if (command == "load") {
                //load

                } else if(command == "ready") {
                    //ready
                    state.ready = true;

                } else if(command == "input") {
                    //input
                    console.log("input");
                    clearTimeout(this._inputTimeoutId);
                    this._inputTimeoutId = setTimeout(()=>{
                        this.onCommand("change");
                    }, 500);
                    
                } else if(command == "change") {
                    //change
                    console.log("change");
                    clearTimeout(this._inputTimeoutId);
                    let target = this.shadowRoot.querySelector(".editor");
                    let oldValue = this.state.value;
                    let newValue = target.value ?? "";
                    state.value = newValue;
                    this.dispatchEvent(new CustomEvent("change", {detail: {oldValue, newValue}, bubbles: true, composed: false}));
                }
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
