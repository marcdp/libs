// contract
export const contract = {
    description: "Provides a draggable visual splitter.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default {
    style:`
        :host {
            display:block;
            width:.2em;
            background: var(--x-splitter-background); 
            cursor:ew-resize;
        }
    `,
    template: ``,
    state: { },
    controller({}) {
        return {
            load() {
                //init
                let mouseMove = () => {
                    console.log("mouse move");
                };
                let mouseUp = () => {
                    console.log("mouse up");
                    document.removeEventListener("mousemove", mouseMove);
                    document.removeEventListener("mouseup", mouseUp);
                };
                this.addEventListener("mousedown", () => {
                    document.addEventListener("mousemove", mouseMove);
                    document.addEventListener("mouseup", mouseUp);
                });
            },

            resize() {
                //resize
            }
        };
    }                
};
