// contract
export const contract = {
    description: "Displays read-only preformatted code with optional copy functionality.",
    events: {
        copy: {
            description: "Raised after the code has been copied to the clipboard.",
            detail: {
                value: {type:"string"}
            }
        }
    },
    properties: {
        value:    {type:"string", default:"", attribute:true, state:true, description:"The code to display."},
        language: {type:"string", default:"", attribute:true, state:true, description:"Optional language identifier associated with the code."},
        wrap:     {type:"boolean", default:false, attribute:true, state:true, description:"Indicates whether long lines should wrap."},
        copyable: {type:"boolean", default:true, attribute:true, state:true, description:"Indicates whether the copy action is displayed."}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {
            display:block;
            position:relative;
            min-width:0;
        }

        .container {
            position:relative;
        }

        pre {
            margin:0;
            padding:var(--x-code-padding, 1em);

            overflow:auto;

            background:var(--x-code-background);
            color:var(--x-code-color);

            border:var(--x-code-border);
            border-radius:var(--x-code-border-radius, var(--x-datafield-border-radius));

            font-family:var(--x-code-font-family, monospace);
            font-size:var(--x-code-font-size, .9em);
            line-height:var(--x-code-line-height, 1.45);
        }

        code {
            font-family:inherit;
            font-size:inherit;
        }

        pre.wrap {
            white-space:pre-wrap;
            overflow-wrap:anywhere;
        }

        pre:not(.wrap) {
            white-space:pre;
        }

        .copy {
            position:absolute;
            top:.5em;
            right:.5em;

            border:0;
            border-radius:.35em;

            padding:.3em .55em;

            background:var(--x-code-copy-background);
            color:var(--x-code-copy-color);

            font:inherit;
            font-size:var(--x-font-size-small);

            cursor:pointer;
            opacity:.8;
        }

        .copy:hover {
            opacity:1;
            color:var(--x-color-primary);
        }

        .copy + pre {
            padding-top:2.75em;
        }
    `,

    template: `
        <div class="container">

            <button
                x-if="state.copyable"
                class="copy"
                type="button"
                x-on:click="copy">
                Copy
            </button>

            <pre
                x-class:wrap="state.wrap"
                x-attr:data-language="state.language"><code x-text="state.value"></code></pre>

        </div>
    `,

    controller({ state, host }) {
        return {
            async copy() {
                await navigator.clipboard.writeText(state.value);

                host.dispatchEvent(new CustomEvent("copy", {
                    detail: {
                        value: state.value
                    },
                    bubbles: true,
                    composed: false
                }));
            }
        };
    }
};
