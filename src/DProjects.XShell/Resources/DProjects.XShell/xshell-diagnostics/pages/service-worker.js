// contract
export const contract = {
    description: "Service Worker mappings",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Service Worker"
    },

    template: `
        <p>Shows the Service Worker mappings registered for virtual XShell and module asset paths.</p>
        <x-listview view="details">
            <x-datafield
                slot="column"
                type="search"
                x-model="state.name"
                placeholder="Name"
                style="width:10em"
            ></x-datafield>

            <x-datafield
                slot="column"
                type="search"
                x-model="state.version"
                placeholder="Version"
                style="width:8em"
            ></x-datafield>

            <x-datafield
                slot="column"
                type="search"
                x-model="state.src"
                placeholder="Source"
            ></x-datafield>

            <x-datafield
                slot="column"
                type="search"
                x-model="state.dst"
                placeholder="Destination"
            ></x-datafield>

            <div slot="column" style="width:5em; text-align:right">
                Exceptions
            </div>

            <x-listview-item
                x-for="item in state.items"
                x-attr:label="item.name"
            >
                <div>{{ item.version }}</div>

                <div>
                    <x-anchor
                        class="plain"
                        x-attr:href="item.src"
                        target="_blank"
                    >
                        {{ item.src }}
                    </x-anchor>
                </div>

                <div>
                    <x-anchor
                        class="plain"
                        x-attr:href="item.dst"
                        target="_blank"
                    >
                        {{ item.dst }}
                    </x-anchor>
                </div>
                <div style="text-align:right"  x-attr:title="item.exceptionUrls">
                    {{ item.exceptionsCount }}
                </div>
            </x-listview-item>
        </x-listview>
    `,

    state: {
        name: "",
        version: "",
        src: "",
        dst: "",
        items: []
    },

    controller({ state, events, config }) {
        return {
            load() {
                events.on(state, "change:name", "refresh");
                events.on(state, "change:version", "refresh");
                events.on(state, "change:src", "refresh");
                events.on(state, "change:dst", "refresh");
                this.refresh();
            },

            refresh() {
                const mappings = config.xshell.serviceWorker?.mappings ?? [];
                state.items = mappings
                    .filter(mapping =>
                        (!state.name || (mapping.name || "").includes(state.name)) &&
                        (!state.version || (mapping.version || "").includes(state.version)) &&
                        (!state.src || mapping.src.includes(state.src)) &&
                        (!state.dst || mapping.dst.includes(state.dst))
                    )
                    .map(mapping => ({
                        name: mapping.name || "",
                        version: mapping.version || "",
                        src: mapping.src,
                        dst: mapping.dst,
                        exceptions: mapping.exceptions || [],
                        exceptionUrls: (mapping.exceptions || []).join(", "),
                        exceptionsCount: mapping.exceptions?.length ?? 0
                    }));
            }
        };
    }
};
