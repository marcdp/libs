// contract
export const contract = {
    description: "Displays an empty-state message when no content is available.",
    events: {},
    properties: {
        icon:    {type:"string", default:"", attribute:true, state:true, description:"The icon displayed for the empty state."},
        label:   {type:"string", default:"", attribute:true, state:true, description:"The primary empty-state label."},
        message: {type:"string", default:"", attribute:true, state:true, description:"Additional information about the empty state."}
    },
    methods: {},
    slots: {
        "": {
            description: "Optional additional empty-state content."
        },
        "actions": {
            description: "Optional actions displayed below the empty-state message."
        }
    }
};


// implementation
export default {
    style: `
        :host {
            display:block;
            text-align:center;
            padding:var(--x-empty-padding, 2em);
            color:var(--x-empty-color, var(--x-color-text-gray));
        }

        .container {
            display:flex;
            flex-direction:column;
            align-items:center;
            justify-content:center;
        }

        x-icon {
            font-size:var(--x-empty-icon-size, 3em);
            color:var(--x-empty-icon-color, var(--x-color-x-gray));
            margin-bottom:.5em;
        }

        .label {
            font-size:var(--x-empty-label-size, var(--x-font-size-subtitle));
            font-weight:600;
            color:var(--x-empty-label-color, var(--x-color-text));
        }

        .message {
            margin-top:.35em;
            max-width:32em;
        }

        .content {
            margin-top:.75em;
        }

        .actions {
            margin-top:1em;
        }

        .content:empty,
        .actions:empty {
            display:none;
        }
    `,

    template: `
        <div class="container">
            <x-icon x-if="state.icon" x-attr:icon="state.icon"></x-icon>
            <div class="label" x-if="state.label" x-text="state.label"></div>
            <div class="message" x-if="state.message" x-text="state.message"></div>
            
            <div class="content">
                <slot></slot>
            </div>

            <div class="actions">
                <slot name="actions"></slot>
            </div>
        </div>
    `
};
