import {  EditorState,
    EditorView,
    basicSetup,
    javascript,
    json,
    html,
    css,
    xml,
    markdown} from "../vendor/codemirror/6.0.2/codemirror.js";


// contract
export const contract = {
    description: "Displays a code mirror editor.",
    events: {        
        change: {
            detail: { 
                value: {
                    type: "string",
                    description: "The new value of the editor."
                }
            },
            description: "Fired when the content of the editor changes."
        },
    },
    properties: {
        value: {type:"string", default:"", attribute:true, state:true, description:""},
        mode: {type:"string", default:"", attribute:true, state:true, description:"", enum:["javascript", "json", "html", "css"]}
    },
    methods: {}
};


// implementation
export default  {
    style: `
        :host {display:block; width:100%; height:100%; }
        :host > div {width:100%; height:100%;}
    `,
    template: ``,
    state: {
        value: "",
        mode: ""    
    },
    controller({ state, host, events }) {
        let editorState = null;
        let editor = null;
        return {
            load() {
                // load
                events.on(state, "change:value", (event) => {
                    if (!editor) return;
                    const value = event.newValue ?? "";
                    const currentValue = editor.state.doc.toString();
                    if (currentValue == value) return;
                    editor.dispatch({
                        changes: {
                            from: 0,
                            to: editor.state.doc.length,
                            insert: value
                        }
                    });
                });
            },
            mount() {
                // mount
                let mode = html();
                if (state.mode == "html") mode = html();
                if (state.mode == "javascript") mode = javascript();
                if (state.mode == "json") mode = json();
                if (state.mode == "css") mode = css();
                if (state.mode == "xml") mode = xml();
                if (state.mode == "markdown") mode = markdown();
                editorState = EditorState.create({
                    doc: state.value,
                    extensions: [
                        basicSetup,
                        mode
                    ]
                });
                editor = new EditorView({
                    state: editorState,
                    parent: host.shadowRoot
                });
                editor.dom.addEventListener("blur", () => {
                    const value = editor.state.doc.toString();
                    host.dispatchEvent(new CustomEvent("change", {
                        detail: { value },
                        bubbles: true
                    }));
                }, true);
            },
        }
    }
};


