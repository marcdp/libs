
// cache
const svgCache = new Map(); // key: url, value: Promise<SVGElement>

// contract
export const contract = {
    description: "Loads and displays an SVG icon.",
    events: {},
    properties: {
        icon: {type:"string", default:"", attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:inline; }
        :host svg {height:1.25em;width:1.25em; fill:currentcolor; vertical-align:middle; display:inline-block;}
        :host(.size-x2) svg {height:2em;width:2em; fill:currentcolor}
    `,
    template: `
        <span x-if="state.svg" x-children="state.svg"></span>
        <svg x-else></svg>
    `,
    state: {
        svg: null
    },
    script({ state, events, loader }) {
        return {
            load(params) {
                //load
                events.on(state, "change:icon", async (event) => {
                    if (state.icon) {
                        state.svg = await loader.load("icon:" + state.icon);
                    } else {
                        state.svg = null;
                    }
                });
            }
        }
    }
}
