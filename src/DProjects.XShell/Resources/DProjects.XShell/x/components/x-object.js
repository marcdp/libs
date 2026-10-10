// contract
export const contract = {
    description: "Displays a JavaScript value as an expandable object tree.",
    events: {},
    properties: {
        value: {
            type: "any",
            default: null,
            attribute: false,
            state: true,
            description: "Value to display."
        },
        name: {
            type: "string",
            default: "",
            attribute: true,
            state: true,
            description: "Optional property name displayed before the value."
        },
        expanded: {
            type: "boolean",
            default: false,
            attribute: true,
            state: true,
            description: "Whether this object node is expanded."
        }
    },
    methods: {
        expand: {
            description: "Expands the current node."
        },
        collapse: {
            description: "Collapses the current node."
        },
        toggle: {
            description: "Toggles the current node."
        }
    },
    slots: {}
};


// implementation
export default {
    style: `
        :host {
            display:block;
            min-width:0;
        }

        .row {
            display:flex;
            align-items:flex-start;
            min-width:0;
            line-height:1.5em;
            padding:.05em .25em;
            border-radius:.25em;
        }

        .row:hover {background:var(--x-object-row-background-hover);}

        .node {
            display:flex;
            align-items:flex-start;
            min-width:0;
        }

        .node.expandable {
            cursor:pointer;
            user-select:none;
            appearance:none;
            border:0;
            background:none;
            padding:0;
            font:inherit;
            color:inherit;
            text-align:left;
        }
        .node.expandable:hover .toggle,
        .node.expandable:hover .name {color:var(--x-color-primary);}
        .node.expandable:focus-visible {outline:2px solid var(--x-color-primary);outline-offset:2px;}

        .toggle {
            flex:none;
            width:1.1em;
            user-select:none;
            margin-right:.25em;
        }

        .toggle.empty {
            visibility:hidden;
        }

        .toggle:not(.expanded) {
            transform:rotate(-90deg);
        }

        .name {
            flex:none;
            margin-right:.35em;
            color:var(--x-object-key-color);
        }
            

        .name:not(:empty)::after {
            content:":";
        }

        .value {
            min-width:0;
            overflow-wrap:anywhere;
        }

        .children {
            margin-left:1.1em;
            padding-left:.35em;
            border-left:1px solid var(--x-object-guide-color);
        }

        .string {
            color:var(--x-object-string);
        }

        .number {
            color:var(--x-object-number);
        }

        .boolean {
            color:var(--x-object-boolean);
        }

        .null {
            color:var(--x-object-null);
        }

        .undefined {
            color:var(--x-object-undefined);
        }

        .date {
            color:var(--x-object-date);
        }

        .summary {
            color:var(--x-object-summary-color);
            white-space:nowrap;
            overflow:hidden;
            text-overflow:ellipsis;
        }

        .object, .array, .accessor, .circular {color:var(--x-object-summary-color);}
    `,

    template: `
        <div class="row">

            <button
                x-if="state.expandable"
                type="button"
                class="node expandable"
                x-attr:aria-expanded="state.expanded ? 'true' : 'false'"
                x-on:click="toggle">

                <span class="toggle" x-class:expanded="state.expanded">
                    <x-icon icon="x-arrow-down-fill"></x-icon>
                </span>

                <span
                    x-if="state.name"
                    class="name"
                    x-text="state.name"></span>

                <span
                    x-if="!state.expanded"
                    class="value summary"
                    x-text="state.summary"></span>

                <span
                    x-else
                    class="value summary"
                    x-text="state.openText"></span>
            </button>

            <div
                x-else
                class="node">

                <span class="toggle empty">
                    ▶
                </span>

                <span
                    x-if="state.name"
                    class="name"
                    x-text="state.name"></span>

                <span
                    class="value"
                    x-attr:class="'value ' + state.type"
                    x-text="state.text"></span>
            </div>

        </div>

        <div
            x-if="state.expandable && state.expanded"
            class="children">

            <x-object
                x-for="entry in state.entries"
                x-attr:name="entry.name"
                x-prop:_inspection="entry.inspection"
                x-prop:value="entry.renderValue">
            </x-object>

            <div class="row">
                <span
                    class="summary"
                    x-text="state.closeText"></span>
            </div>
        </div>
    `,

    state: {
        type: "undefined",
        text: "",
        expandable: false,
        summary: "",
        openText: "",
        closeText: "",
        entries: []
    },

    controller({state, events, host}) {
        const SUMMARY_MAX_LENGTH = 100;
        let inspection = null;

        function getType(value) {
            if (value === null) {
                return "null";
            }

            if (value instanceof Date) {
                return "date";
            }

            if (Array.isArray(value)) {
                return "array";
            }

            return typeof value;
        }

        function formatPrimitive(value, type) {
            switch (type) {
                case "string":
                    return JSON.stringify(value);

                case "number":
                case "boolean":
                case "bigint":
                    return String(value);

                case "null":
                    return "null";

                case "undefined":
                    return "undefined";

                case "date":
                    return Number.isNaN(Date.prototype.getTime.call(value)) ? "Invalid Date" : Date.prototype.toISOString.call(value);

                case "function": {
                    const name = Object.getOwnPropertyDescriptor(value, "name")?.value;
                    return typeof name === "string" && name ? `ƒ ${name}()` : "ƒ ()";
                }

                case "symbol":
                    return String(value);

                default:
                    return String(value);
            }
        }

        function getConstructorName(value) {
            const prototype = Object.getPrototypeOf(value);
            const constructor = prototype && Object.getOwnPropertyDescriptor(prototype, "constructor")?.value;
            const name = typeof constructor === "function" && Object.getOwnPropertyDescriptor(constructor, "name")?.value;
            return typeof name === "string" ? name : "";
        }

        function formatSummaryValue(value, ancestors) {
            const type = getType(value);

            if ((type === "array" || type === "object") && ancestors.includes(value)) {
                return "[Circular]";
            }

            if (type === "array") {
                return `Array(${value.length})`;
            }

            if (type === "object") {
                const name = getConstructorName(value);

                if (name && name !== "Object") {
                    return `${name} ${Object.keys(value).length ? "{…}" : "{}"}`;
                }

                return Object.keys(value).length ? "{…}" : "{}";
            }

            return formatPrimitive(value, type);
        }

        function truncate(text, maxLength) {
            if (text.length <= maxLength) {
                return text;
            }

            return text.substring(0, maxLength - 1) + "…";
        }

        function createEntries(value, ancestors) {
            return Object.keys(value).map(name => {
                const descriptor = Object.getOwnPropertyDescriptor(value, name);
                const accessor = descriptor && !Object.hasOwn(descriptor, "value")
                    ? descriptor.get && descriptor.set ? "[Getter/Setter]" : descriptor.get ? "[Getter]" : "[Setter]"
                    : null;
                const entryValue = accessor ? undefined : descriptor?.value;
                // return functions as data because XTemplate evaluates function-valued bindings.
                const renderValue = typeof entryValue === "function" ? () => entryValue : entryValue;
                return { name, value: entryValue, renderValue, inspection: { ancestors, accessor } };
            });
        }

        function getSummary(value, type, entries, ancestors) {
            const parts = entries.map(entry => {
                const text = entry.inspection.accessor || formatSummaryValue(entry.value, ancestors);
                return type === "array" ? text : `${entry.name}: ${text}`;
            });

            if (type === "array") {
                const text = `[${parts.join(", ")}]`;

                return truncate(text, SUMMARY_MAX_LENGTH);
            }

            if (type === "object") {
                const name = getConstructorName(value);
                const prefix =
                    name && name !== "Object"
                        ? `${name} `
                        : "";

                const text = `${prefix}{${parts.join(", ")}}`;

                return truncate(text, SUMMARY_MAX_LENGTH);
            }

            return "";
        }

        function getOpenText(value, type) {
            if (type === "array") {
                return "[";
            }

            if (type === "object") {
                const name = getConstructorName(value);

                if (name && name !== "Object") {
                    return `${name} {`;
                }

                return "{";
            }

            return "";
        }

        function getCloseText(type) {
            return type === "array" ? "]" : "}";
        }

        const controller = {
            load() {
                events.on(state, "change:value", "refresh");
                this.refresh();
            },

            refresh() {
                const value = state.value;
                const type = getType(value);
                const ancestors = inspection?.ancestors ?? [];

                state.type = type;
                state.entries = [];
                state.expandable = false;
                state.summary = "";
                state.openText = "";
                state.closeText = "";
                state.text = "";

                if (inspection?.accessor) {
                    state.type = "accessor";
                    state.text = inspection.accessor;
                    return;
                }

                if (type === "array" || type === "object") {
                    if (ancestors.includes(value)) {
                        state.type = "circular";
                        state.text = "[Circular]";
                        return;
                    }

                    const branch = [...ancestors, value];
                    const entries = createEntries(value, branch);

                    state.entries = entries;
                    state.expandable = entries.length > 0;
                    state.summary = getSummary(value, type, entries, branch);
                    state.openText = getOpenText(value, type);
                    state.closeText = getCloseText(type);
                    if (!state.expandable) {
                        state.text = state.summary;
                    }
                    return;
                }

                state.text = formatPrimitive(value, type);
            },

            toggle() {
                if (!state.expandable) {
                    return;
                }

                state.expanded = !state.expanded;
            },

            expand() {
                if (state.expandable) {
                    state.expanded = true;
                }
            },

            collapse() {
                state.expanded = false;
            }
        };

        Object.defineProperty(host, "_inspection", {
            get() { return inspection; },
            set(value) {
                inspection = value;
                controller.refresh();
            }
        });

        return controller;
    }
};
