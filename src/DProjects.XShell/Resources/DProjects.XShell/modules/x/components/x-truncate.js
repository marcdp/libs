// contract
export const contract = {
    description: "Displays text truncated with an ellipsis when it exceeds the available space.",
    events: {},
    properties: {
        text:  {type:"string", default:"", attribute:true, state:true, description:"The text to display."},
        lines: {type:"number", default:1, attribute:true, state:true, description:"The maximum number of visible lines."}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {
            display:block;
            min-width:0;
        }

        .truncate {
            overflow:hidden;
            min-width:0;
        }

        .single {
            white-space:nowrap;
            text-overflow:ellipsis;
        }

        .multi {
            display:-webkit-box;
            -webkit-box-orient:vertical;
            overflow:hidden;
        }
    `,

    template: `
        <div
            class="truncate"
            x-class:single="state.lines <= 1"
            x-class:multi="state.lines > 1"
            x-style:-webkit-line-clamp="state.lines"
            x-attr:title="state.text"
            x-text="state.text"></div>
    `
};
