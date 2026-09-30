// contract
export const contract = {
    description: "Displays interactive content in a floating panel anchored to a trigger.",
    events: {},
    properties: {
        expanded: {type:"boolean", default:false, attribute:true, state:true, description:"Indicates whether the popover is open."},
        position: {type:"string", default:"bottom", attribute:true, state:true, description:"The preferred popover position: top, bottom, left, or right."}
    },
    methods: {},
    slots: {
        "": {
            description: "The trigger content used to open the popover."
        },
        "popover": {
            description: "The content displayed inside the popover."
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

        .trigger {
            display:inline-block;
        }

        .popover {
            display:none;
            position:absolute;
            z-index:20;

            min-width:12em;
            max-width:min(30em, 90vw);

            background:var(--x-popover-background);
            border:var(--x-popover-border);
            border-radius:var(--x-popover-border-radius);
            box-shadow:var(--x-popover-shadow);

            padding:var(--x-popover-padding);

            box-sizing:border-box;
        }

        .popover.expanded {
            display:block;
        }


        /* bottom */
        .popover.bottom {
            top:calc(100% + var(--x-popover-offset, .75em));
            left:50%;
            transform:translateX(-50%);
        }

        /* top */
        .popover.top {
            bottom:calc(100% + var(--x-popover-offset, .75em));
            left:50%;
            transform:translateX(-50%);
        }

        /* left */
        .popover.left {
            right:calc(100% + var(--x-popover-offset, .75em));
            top:50%;
            transform:translateY(-50%);
        }

        /* right */
        .popover.right {
            left:calc(100% + var(--x-popover-offset, .75em));
            top:50%;
            transform:translateY(-50%);
        }


        /* arrow */

        .arrow,
        .arrow-border {
            position:absolute;
            width:0;
            height:0;
            pointer-events:none;
        }

        .popover.bottom .arrow-border {
            top:-.7em;
            left:50%;
            transform:translateX(-50%);
            border-left:.7em solid transparent;
            border-right:.7em solid transparent;
            border-bottom:.7em solid var(--x-popover-border-color, #B4B4BB);
        }

        .popover.bottom .arrow {
            top:-.6em;
            left:50%;
            transform:translateX(-50%);
            border-left:.6em solid transparent;
            border-right:.6em solid transparent;
            border-bottom:.6em solid var(--x-popover-background);
        }

        .popover.top .arrow-border {
            bottom:-.7em;
            left:50%;
            transform:translateX(-50%);
            border-left:.7em solid transparent;
            border-right:.7em solid transparent;
            border-top:.7em solid var(--x-popover-border-color, #B4B4BB);
        }

        .popover.top .arrow {
            bottom:-.6em;
            left:50%;
            transform:translateX(-50%);
            border-left:.6em solid transparent;
            border-right:.6em solid transparent;
            border-top:.6em solid var(--x-popover-background);
        }

        .popover.left .arrow-border {
            right:-.7em;
            top:50%;
            transform:translateY(-50%);
            border-top:.7em solid transparent;
            border-bottom:.7em solid transparent;
            border-left:.7em solid var(--x-popover-border-color, #B4B4BB);
        }

        .popover.left .arrow {
            right:-.6em;
            top:50%;
            transform:translateY(-50%);
            border-top:.6em solid transparent;
            border-bottom:.6em solid transparent;
            border-left:.6em solid var(--x-popover-background);
        }

        .popover.right .arrow-border {
            left:-.7em;
            top:50%;
            transform:translateY(-50%);
            border-top:.7em solid transparent;
            border-bottom:.7em solid transparent;
            border-right:.7em solid var(--x-popover-border-color, #B4B4BB);
        }

        .popover.right .arrow {
            left:-.6em;
            top:50%;
            transform:translateY(-50%);
            border-top:.6em solid transparent;
            border-bottom:.6em solid transparent;
            border-right:.6em solid var(--x-popover-background);
        }
    `,

    template: `
        <div
            class="trigger"
            x-on:click="toggle"
            x-on:keydown.enter="toggle"
            x-on:keydown.space="toggle">
            <slot></slot>
        </div>

        <div
            class="popover"
            x-class:expanded="state.expanded"
            x-class:top="state.position == 'top'"
            x-class:bottom="state.position == 'bottom'"
            x-class:left="state.position == 'left'"
            x-class:right="state.position == 'right'">

            <span class="arrow-border"></span>
            <span class="arrow"></span>

            <slot name="popover"></slot>
        </div>
    `,

    controller({ state, host }) {
        return {
            load() {
                this.onDocumentClick = (event) => {
                    if (!state.expanded) return;
                    if (event.composedPath().includes(host)) return;

                    this.collapse();
                };

                this.onKeydown = (event) => {
                    if (event.key == "Escape") {
                        this.collapse();
                    }
                };

                document.addEventListener("click", this.onDocumentClick, true);
                document.addEventListener("keydown", this.onKeydown);
            },

            unload() {
                document.removeEventListener("click", this.onDocumentClick, true);
                document.removeEventListener("keydown", this.onKeydown);
            },

            toggle(args) {
                if (state.expanded) {
                    this.collapse();
                } else {
                    this.expand();
                }

                if (args?.event) {
                    args.event.preventDefault();
                    args.event.stopPropagation();
                }
            },

            expand() {
                state.expanded = true;
            },

            collapse() {
                state.expanded = false;
            }
        };
    }
};
