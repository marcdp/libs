// contract
export const contract = {
    description: "Displays a single expandable disclosure section.",
    events: {
        toggle: {
            description: "Raised when the open state changes."
        }
    },
    properties: {
        label: {type: "string", default: "", attribute: true, state: true, description: "Text displayed in the disclosure header."},
        open: {type: "boolean",default: false,attribute: true,state: true,description: "Whether the disclosure content is expanded."},
        disabled: {type: "boolean",default: false,attribute: true,state: true,description: "Whether the disclosure can be toggled by the user."}
    },
    methods: {
        toggle: {description: "Toggles the disclosure state."},
        expand: {description: "Expands the disclosure."},
        collapse: {description: "Collapses the disclosure."}
    },
    slots: {
        "": {description: "Disclosure body content."},
        summary: {description: "Optional custom summary content. Replaces the label when provided."}
    }    
};


// documentation
export const documentation = {
    examples: [
        {   name: "Basic",
            template: `
                <x-details label="Advanced options">
                    <p>Additional content.</p>
                </x-details>
            `
        },
        {   name: "Custom summary",
            template: `
                <x-details>
                    <strong slot="summary">More information</strong>
                    <p>Additional content.</p>
                </x-details>
            `
        }
    ]
};


// implementation
export default {
    style: `
        :host {
            display: block;
        }

        .header {
            display: flex;
            align-items: center;
            gap: .5em;
            min-height: 2.5em;
            padding: 0 .75em;
            padding-left:0;
            cursor: pointer;
            user-select: none;
        }

        :host([disabled]) .header { cursor: default; opacity: .6; }

        .header:hover {text-decoration:underline; cursor:pointer;}
        .header > .label { flex: 1; display:flex;}

        .chevron {flex: none; transition: transform var(--x-transition-duration); }

        .header[open] .chevron {transform: rotate(-90deg);}

        .body {
            display: grid;
            grid-template-rows: 0fr;
            transition: grid-template-rows var(--x-transition-duration) ease;
        }

        .body[open] {
            grid-template-rows: 1fr;
        }

        .body > div {
            overflow: hidden;
        }

        .content {
        }
 
    `,

    template: `
        <div class="header" x-attr:open="state.open" x-on:click="toggle">
            <div class="label">
                <slot name="summary">
                    <span x-text="state.label"></span>
                </slot>
                <x-icon class="chevron" icon="x-arrow-down-fill"></x-icon>
            </div>            
        </div>
        <div class="body" x-attr:open="state.open">
            <div>
                <div class="content">
                    <slot></slot>
                </div>
            </div>
        </div>
    `,

    state: {},

    controller({ state, host }) {
        const setOpen = (value) => {
            if (state.open === value) return;
            state.open = value;
            host.dispatchEvent(new CustomEvent("toggle", {bubbles: true, composed: false, detail: { open: state.open }}));
        };
        return {
            toggle() {
                if (state.disabled) return;
                setOpen(!state.open);
            },

            expand() {
                if (state.disabled) return;
                setOpen(true);
            },

            collapse() {
                if (state.disabled) return;
                setOpen(false);
            }
        };
    }
};