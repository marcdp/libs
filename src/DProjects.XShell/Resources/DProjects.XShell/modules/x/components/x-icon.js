
// cache
const svgCache = new Map(); // key: url, value: Promise<SVGElement>

// class
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
        icon: {value:"", attr:true},
        svg:  {value:null}
    },
    script({ state, events, loader }) {
        return {
            onCommand(command, params){
                if (command == "load") {
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
}
