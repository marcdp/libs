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
        }

        .node {
            display:flex;
            align-items:flex-start;
            min-width:0;
        }

        .node.expandable {
            cursor:pointer;
            user-select:none;
        }
        .node.expandable:hover {color:var(--x-color-primary);}

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
        }

        .children x-object:nth-child(odd) {
            background:var(--x-color-background-alt);
        }

        .children x-object:nth-child(even) {
            background:var(--x-color-background-page);
        }

        .string {
            color:var(--x-object-string, green);
        }

        .number {
            color:var(--x-object-number, darkorange);
        }

        .boolean {
            color:var(--x-object-boolean, blue);
        }

        .null {
            color:var(--x-object-null, magenta);
        }

        .undefined {
            color:var(--x-object-undefined, gray);
        }

        .date {
            color:var(--x-object-date, teal);
        }

        .summary {
            color:var(--x-color-text-gray);
            white-space:nowrap;
            overflow:hidden;
            text-overflow:ellipsis;
        }
    `,

    template: `
        <div class="row">

            <div
                x-if="state.expandable"
                class="node expandable"
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
            </div>

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
                x-prop:value="entry.value">
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

    controller({state, events}) {
        const SUMMARY_MAX_LENGTH = 100;

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
                    return value.toISOString();

                case "function":
                    return value.name
                        ? `ƒ ${value.name}()`
                        : "ƒ ()";

                case "symbol":
                    return String(value);

                default:
                    return String(value);
            }
        }

        function formatSummaryValue(value) {
            const type = getType(value);

            if (type === "array") {
                return `Array(${value.length})`;
            }

            if (type === "object") {
                const name = value?.constructor?.name;

                if (name && name !== "Object") {
                    return `${name} {…}`;
                }

                return "{…}";
            }

            return formatPrimitive(value, type);
        }

        function truncate(text, maxLength) {
            if (text.length <= maxLength) {
                return text;
            }

            return text.substring(0, maxLength - 1) + "…";
        }

        function createEntries(value) {
            if (Array.isArray(value)) {
                return value.map((item, index) => ({
                    name: String(index),
                    value: item
                }));
            }

            if (value && typeof value === "object") {
                return Object.keys(value).map(name => ({
                    name,
                    value: value[name]
                }));
            }

            return [];
        }

        function getSummary(value, type) {
            if (type === "array") {
                const parts = value.map(item => formatSummaryValue(item));
                const text = `[${parts.join(", ")}]`;

                return truncate(text, SUMMARY_MAX_LENGTH);
            }

            if (type === "object") {
                const parts = Object.keys(value).map(name => {
                    return `${name}: ${formatSummaryValue(value[name])}`;
                });

                const name = value?.constructor?.name;
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
                const name = value?.constructor?.name;

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

        return {
            load() {
                events.on(state, "change:value", "refresh");
                this.refresh();
            },

            refresh() {
                const value = state.value;
                const type = getType(value);

                state.type = type;
                state.entries = [];
                state.expandable = false;
                state.summary = "";
                state.openText = "";
                state.closeText = "";
                state.text = "";

                if (type === "array" || type === "object") {
                    const entries = createEntries(value);

                    state.entries = entries;
                    state.expandable = true;
                    state.summary = getSummary(value, type);
                    state.openText = getOpenText(value, type);
                    state.closeText = getCloseText(type);
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
    }
};