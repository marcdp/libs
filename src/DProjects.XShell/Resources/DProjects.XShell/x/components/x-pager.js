// contract
export const contract = {
    description: "Displays pagination controls and emits requested page changes.",
    events: {
        change: {
            description: "Raised when the previous or next page is requested.",
            detail: {
                index: {type:"number"},
                size: {type:"number"}
            }
        }
    },
    properties: {
        total: {type:"number", default:0, attribute:true, state:true, description:""},
        index: {type:"number", default:0, attribute:true, state:true, description:""},
        size:  {type:"number", default:20, attribute:true, state:true, description:""},
        label: {type:"string", default:"records", attribute:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {
            display:flex;
            align-items: center;
            gap:.25em;
        }
        span.flex {flex:1;}
        span.total {font-weight:600;}
        
    `,
    template: `
        <span class="total" x-text="state.total"></span>
        <span class="label" x-text="state.label"></span>

        <span class="flex"></span>
        
        <span class="from">{{ (state.size * state.index) + 1 }}</span>
        <span>-</span>
        <span class="to">{{ state.size * (state.index + 1) }}</span>

        of

        <span class="total" x-text="state.total"></span>
        
        &nbsp;

        <x-button class="short plain prev" x-on:click="prev" icon="x-keyboard-arrow-left"   x-attr:disabled="state.index == 0"></x-button>
        <x-button class="short plain next" x-on:click="next" icon="x-keyboard-arrow-right"  x-attr:disabled="state.isLastPage"></x-button>
    `,
    state: {
        isLastPage: false
    },
    controller({ state, events, host }) {
        const updateIsLastPage = () => {
            state.isLastPage = state.index == Math.floor(state.total / state.size) - 1;
        };
        return {
            load(args) {
                //load
                events.on(state, ["change:index", "change:size", "change:total"], updateIsLastPage);
                updateIsLastPage();
            },

            prev(args) {
                //prev
                host.dispatchEvent(new CustomEvent("change", {detail: {index: state.index - 1, size: state.size}}));
            },

            next(args) {
                //next
                host.dispatchEvent(new CustomEvent("change", {detail: {index: state.index + 1, size: state.size}}));
            }
        };
    }
};

