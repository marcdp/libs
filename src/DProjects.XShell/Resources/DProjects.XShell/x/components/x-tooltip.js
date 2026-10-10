// contract
export const contract = {
    description: "Displays contextual information when the user hovers or focuses its content.",
    events: {},
    properties: {
        text:     {type:"string", default:"",    attribute:true, state:true, description:"The tooltip text."},
        position: {type:"string", default:"top", attribute:true, state:true, description:"The tooltip position: top, bottom, left, or right."}
    },
    methods: {},
    slots: {
        "": {
            description: "The element or content associated with the tooltip."
        }
    }
};


// implementation
export default {
    style: `
        :host {
            display:inline-block;
            position:relative;
        }

        .tooltip {
            position:absolute;
            z-index:20;
            pointer-events:none;

            padding:.35em .65em;

            background:var(--x-tooltip-background);
            color:var(--x-tooltip-color);

            border-radius:var(--x-tooltip-border-radius, .35em);
            box-shadow:var(--x-tooltip-shadow, none);

            font-size:var(--x-font-size-small);
            line-height:1.4;
            white-space:nowrap;

            opacity:0;
            visibility:hidden;

            transition:
                opacity var(--x-transition-duration),
                visibility var(--x-transition-duration);

            transition-delay:var(--x-tooltip-delay, .4s);
        }

        .tooltip.visible {
            opacity:1;
            visibility:visible;
        }

        /* top */
        .tooltip.top {
            left:50%;
            bottom:calc(100% + var(--x-tooltip-offset, .5em));
            transform:translateX(-50%);
        }

        /* bottom */
        .tooltip.bottom {
            left:50%;
            top:calc(100% + var(--x-tooltip-offset, .5em));
            transform:translateX(-50%);
        }

        /* left */
        .tooltip.left {
            right:calc(100% + var(--x-tooltip-offset, .5em));
            top:50%;
            transform:translateY(-50%);
        }

        /* right */
        .tooltip.right {
            left:calc(100% + var(--x-tooltip-offset, .5em));
            top:50%;
            transform:translateY(-50%);
        }
    `,

    template: `
        <slot></slot>

        <div    
            x-if="state.text"
            class="tooltip"
            role="tooltip"
            x-class:visible="state.visible"
            x-class:top="state.position == 'top'"
            x-class:bottom="state.position == 'bottom'"
            x-class:left="state.position == 'left'"
            x-class:right="state.position == 'right'"
            x-text="state.text"></div>
    `,

    state: {
        visible: false
    },

    controller({ state, host }) {
        return {
            load() {
                // load
                host.addEventListener("mouseenter", () => {
                    this.show();
                });
                host.addEventListener("mouseleave", () => {
                    this.hide();
                });
                host.addEventListener("focusin", () => {
                    this.show();
                });
                host.addEventListener("focusout", () => {
                    this.hide();
                });
                host.addEventListener("keydown", (event) => {
                    if (event.key == "Escape") {
                        this.hide();
                    }
                });
            },
            show() {
                if (state.text) {
                    state.visible = true;
                }
            },
            hide() {
                state.visible = false;
            }
        };
    }
};
