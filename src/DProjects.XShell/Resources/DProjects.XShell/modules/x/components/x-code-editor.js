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
        value:    {type:"string", default:"", attribute:true, state:true, description:""},
        mode:     {type:"string", default:"", attribute:true, state:true, description:""},
        readonly: {type:"boolean", default:false, attribute:true, state:true, description:""},
        //theme:    {type:"string", default:"chrome", attribute:true, state:true, description:""},
        //wrap:     {type:"boolean", default:false, attribute:true, state:true, description:""},
        //ready:    {type:"boolean", default:false, attribute:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:flex; height:10em; flex-direction:column; align-items:center; justify-content:center;}
        :host x-lazy {width:100%; height:100%;}
        :host codemirror-editor {border-radius:var(--x-datafield-border-radius); flex:1; height:100%; overflow:hidden;;}
        :host x-spinner + x-lazy {visibility:hidden; height:0}
    `,
    state: {
        value: "",
        mode: "",
        readonly: false
    },
    template: `
        <!--<x-spinner x-if="!state.ready"></x-spinner>-->
        <codemirror-editor 
            class="editor"
            x-prop:value="state.value"             
            x-prop:readonly="state.readonly" 
            x-attr:mode="state.mode"
            x-on:change="change"
        ></codemirror-editor>
    `,
    controller({ state, host }) {
        return {
            load(params) {
                //load
            },
            change(params) {
                //change
                let oldValue = state.value;
                let newValue = params.event.detail.value ?? "";
                state.value = newValue;
                host.dispatchEvent(new CustomEvent("change", {detail: {oldValue, newValue}, bubbles: true, composed: false}));
            }
        }
    }    
};
