
//utils
function syntaxHighlight(json) {
    if (typeof json != 'string') {
        json = JSON.stringify(json, undefined, 2);
    }
    json = json.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    return json.replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g, function (match) {
        var cls = 'number';
        if (/^"/.test(match)) {
            if (/:$/.test(match)) {
                cls = 'key';
            } else {
                cls = 'string';
            }
        } else if (/true|false/.test(match)) {
            cls = 'boolean';
        } else if (/null/.test(match)) {
            cls = 'null';
        }
        return '<span class="' + cls + '">' + match + '</span>';
    });
}

// contract
export const contract = {
    description: "Displays syntax-highlighted JSON source.",
    events: {},
    properties: {
        value: {type:"object", default:null, state:true, description:"Object to be displayed as syntax-highlighted JSON."},
        indent: {type:"number", default:0, state:true, attribute:true, description:"Indentation level for JSON formatting."}
    },
    methods: {}
};


// implementation
export default {
    style:`
        :host {}
        pre {
            margin: 0;
            padding: 0;
            overflow:auto;
        }
        pre.indent {
            white-space: normal;
        }
        pre .string {color: green;}
        pre .number {color: darkorange;}
        pre .boolean {color: blue;}
        pre .null {color: magenta;}
        pre .key {color: red;}
        
        :host(.plain) pre {border: none; padding: 0;}
    `,
    template: `
        <pre x-class:indent="!state.indent"><code x-html="state.jsonColorized"></code></pre>
    `,
    state: {
        value: null,
        indent: 0,
        jsonColorized: null
    },
    controller({ state, events }) {
        return {
            load() {
                //load
                events.on(state, ["change:value", "change:indent"], (event) => {
                    const json = JSON.stringify(state.value, null, state.indent);
                    state.jsonColorized = syntaxHighlight(json);
                });
            }
        }
    }
}
