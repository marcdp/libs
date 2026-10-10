// contract
export const contract = {
    description: "Displays a compact labeled item that can optionally be removed.",
    events: {
        remove: {
            description: "Raised when the remove action is invoked."
        }
    },
    properties: {
        label:     {type:"string", default:"", attribute:true, state:true, description:"The chip label."},
        icon:      {type:"string", default:"", attribute:true, state:true, description:"The optional icon displayed before the label."},
        removable: {type:"boolean", default:false, attribute:true, state:true, description:"Indicates whether the chip can be removed."}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {
            display:inline-flex;
        }

        .chip {
            display:inline-flex;
            align-items:center;
            gap:.35em;

            padding:var(--x-chip-padding, .25em .65em);

            background:var(--x-chip-background);
            color:var(--x-chip-color);

            border:var(--x-chip-border);
            border-radius:var(--x-chip-border-radius, 1em);

            font-size:var(--x-chip-font-size, var(--x-font-size-small));
            line-height:1.4;
        }

        .chip x-icon {
            flex:none;
        }

        .label {
            white-space:nowrap;
        }

        .remove {
            display:inline-flex;
            align-items:center;
            justify-content:center;

            cursor:pointer;
            user-select:none;

            border:none;
            background:none;
            color:inherit;

            padding:0;
            margin:0 0 0 .15em;

            font:inherit;
            line-height:1;
        }

        .remove:hover {
            color:var(--x-color-primary);
        }
    `,

    template: `
        <span class="chip">
            <x-icon x-if="state.icon" x-attr:icon="state.icon"></x-icon>

            <span class="label" x-if="state.label" x-text="state.label"></span>

            <button
                class="remove"
                type="button"
                x-if="state.removable"
                x-on:click="remove"
                aria-label="Remove">
                ×
            </button>
        </span>
    `,

    controller({ host }) {
        return {
            remove(args) {
                host.dispatchEvent(new CustomEvent("remove", {
                    bubbles: true,
                    composed: false
                }));

                if (args.event) {
                    args.event.preventDefault();
                    args.event.stopPropagation();
                }
            }
        };
    }
};
