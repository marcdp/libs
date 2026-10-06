// contract
export const contract = {
    description: "Provides a draggable splitter between two adjacent panes.",
    events: {
        resize: {
            description: "Raised after the splitter changes the size of the preceding pane."
        }
    },
    properties: {
        orientation: {
            type: "string",
            default: "vertical",
            attribute: true,
            state: true,
            reflect: true,
            enum: ["vertical", "horizontal"],
            description: "Splitter orientation."
        },
        disabled: {
            type: "boolean",
            default: false,
            attribute: true,
            state: true,
            reflect: true,
            description: "Whether resizing is disabled."
        }
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {
            display:block;
            flex:0 0 .15em;
            background:var(--x-splitter-background);
            cursor:col-resize;
            user-select:none;
            touch-action:none;
        }

        :host([orientation="horizontal"]) {
            cursor:row-resize;
        }

        :host([disabled]) {
            cursor:default;
        }

        .handle {
            width:100%;
            height:100%;
            outline:none;
            touch-action:none;
        }

        .handle:focus-visible {
            background:var(--x-color-primary);
        }
    `,
    template: `
        <div
            class="handle"
            role="separator"
            tabindex="0"
            x-attr:aria-orientation="state.orientation"
            x-on:pointerdown="pointerDown"
            x-on:pointermove="pointerMove"
            x-on:pointerup="pointerUp"
            x-on:pointercancel="pointerUp"
            x-on:keydown="keydown">
        </div>
    `,
    controller({ state, host }) {
        return {
            pointerDown({ event }) {
                if (state.disabled || event.button !== 0) return;

                const pane = host.previousElementSibling;
                const nextPane = host.nextElementSibling;
                if (!pane || !nextPane) return;

                const vertical = state.orientation === "vertical";
                const paneRect = pane.getBoundingClientRect();
                const nextRect = nextPane.getBoundingClientRect();

                this._drag = {
                    pointerId: event.pointerId,
                    pane,
                    vertical,
                    start: vertical ? event.clientX : event.clientY,
                    size: vertical ? paneRect.width : paneRect.height,
                    max: vertical ? paneRect.width + nextRect.width : paneRect.height + nextRect.height
                };

                event.currentTarget.setPointerCapture(event.pointerId);
                event.preventDefault();
            },

            pointerMove({ event }) {
                const drag = this._drag;
                if (!drag || drag.pointerId !== event.pointerId) return;

                const position = drag.vertical ? event.clientX : event.clientY;
                const size = Math.max(0, Math.min(drag.max, drag.size + position - drag.start));

                drag.pane.style.flex = `0 0 ${size}px`;
            },

            pointerUp({ event }) {
                const drag = this._drag;
                if (!drag || drag.pointerId !== event.pointerId) return;

                const rect = drag.pane.getBoundingClientRect();
                const size = drag.vertical ? rect.width : rect.height;

                if (event.currentTarget.hasPointerCapture?.(event.pointerId)) {
                    event.currentTarget.releasePointerCapture(event.pointerId);
                }

                this._drag = null;

                host.dispatchEvent(new CustomEvent("resize", {
                    bubbles: true,
                    composed: false,
                    detail: { size }
                }));
            },

            keydown({ event }) {
                if (state.disabled) return;

                const pane = host.previousElementSibling;
                const nextPane = host.nextElementSibling;
                if (!pane || !nextPane) return;

                const vertical = state.orientation === "vertical";
                let direction = 0;

                if (vertical && event.key === "ArrowLeft") direction = -1;
                if (vertical && event.key === "ArrowRight") direction = 1;
                if (!vertical && event.key === "ArrowUp") direction = -1;
                if (!vertical && event.key === "ArrowDown") direction = 1;

                if (!direction) return;

                const paneRect = pane.getBoundingClientRect();
                const nextRect = nextPane.getBoundingClientRect();
                const current = vertical ? paneRect.width : paneRect.height;
                const max = vertical ? paneRect.width + nextRect.width : paneRect.height + nextRect.height;
                const size = Math.max(0, Math.min(max, current + direction * 10));

                pane.style.flex = `0 0 ${size}px`;

                host.dispatchEvent(new CustomEvent("resize", {
                    bubbles: true,
                    composed: false,
                    detail: { size }
                }));

                event.preventDefault();
            },

            unload() {
                this._drag = null;
            }
        };
    }
};