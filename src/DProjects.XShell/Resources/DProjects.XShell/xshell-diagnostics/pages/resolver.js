// contract
export const contract = {
    description: "Resolver page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Resolver"
    },
    style: `
        .test {margin-bottom:1.5em;}
        .test x-toolbar {margin-bottom:.5em;}
        .test x-datafield {width:40em; max-width:70vw;}
    `,
    template: `
        <div class="test">
            <x-toolbar>
                <x-datafield type="text" x-model="state.test_resource" placeholder="Resource, e.g. component:x-button"></x-datafield>
                <x-button label="Resolve" command="resolve"></x-button>
            </x-toolbar>

            <x-propertygrid x-if="state.test_result" x-prop:value="state.test_result"></x-propertygrid>
            <div x-if="state.test_message">{{ state.test_message }}</div>
        </div>

        <x-listview view="details">
            <x-datafield slot="column" type="search" x-model="state.query_resource" placeholder="Resource"></x-datafield>
            <div slot="column">
                <x-datafield type="search" x-model="state.query_src" placeholder="Source"></x-datafield>
            </div>
            <div slot="column" style="width:10em">
                <x-datafield type="search" x-model="state.query_loader" placeholder="Loader"></x-datafield>
            </div>
            <div slot="column" style="width:5em">
                Cache
            </div>
            <div slot="column" style="width:7em">
                Cache mode
            </div>

            <x-listview-item x-for="item in state.items" x-attr:label="item.resource" icon="x-settings">
                <div>{{ item.src }}</div>
                <div>{{ item.loader }}</div>
                <div>{{ item.cache }}</div>
                <div>{{ item.cacheMode }}</div>
            </x-listview-item>
        </x-listview>
    `,
    state: {
        test_resource: "",
        test_result: null,
        test_message: "",
        query_resource: "",
        query_src: "",
        query_loader: "",
        items: []
    },
    controller({ state, events, resolver }) {
        return {
            load() {
                // load
                this.refresh();
                events.on(state, "change:query_resource", "refresh");
                events.on(state, "change:query_src", "refresh");
                events.on(state, "change:query_loader", "refresh");
            },
            resolve() {
                // resolve
                const resource = state.test_resource.trim();
                state.test_result = null;
                state.test_message = "";

                if (!resource) {
                    state.test_message = "Enter a resource to resolve.";
                    return;
                }

                // Resolver.resolve() ignores query and fragment when matching.
                const normalizedResource = resource.split("#")[0].split("?")[0];
                if (!resolver.has(normalizedResource)) {
                    state.test_message = "No resolver rule matched the resource.";
                    return;
                }

                const result = resolver.resolve(resource);
                if (!result) {
                    state.test_message = "Unable to resolve the resource.";
                    return;
                }

                state.test_result = {
                    resource: resource,
                    rule: result.definition.resource,
                    src: result.definition.src,
                    path: result.path ?? "",
                    url: result.url,
                    loader: result.definition.loader ?? "",
                    cache: result.definition.cache ?? false,
                    cacheMode: result.definition.cacheMode ?? "full"
                };
            },
            refresh() {
                // refresh
                state.items = resolver.registry
                    .filter(item =>
                        (!state.query_resource || item.resource.includes(state.query_resource)) &&
                        (!state.query_src || item.src.includes(state.query_src)) &&
                        (!state.query_loader || item.loader?.includes(state.query_loader))
                    )
                    .map(item => ({
                        resource: item.resource,
                        src: item.src,
                        loader: item.loader ?? "",
                        cache: item.cache ? "yes" : "no",
                        cacheMode: item.cacheMode ?? "full"
                    }));
            }
        };
    }
};