// contract
export const contract = {
    description: "Controller service injection",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Services</h2>

        <p>
            A definition-based Page controller requests its execution context by destructuring names. Page-specific values such as
            <code>state</code>, <code>page</code>, and <code>query</code> are combined with services registered by the current XShell runtime.
            This is named runtime access, not a general-purpose arbitrary dependency-injection container.
        </p>

        <pre x-pre><code>controller({ state, page, navigation, dialog, bus, loader }) {
    // names request current Page context or registered XShell services
}</code></pre>

        <h3>Current injected values</h3>

        <x-datafields>
            <x-datafield label="Navigation mode"><code>{{ state.navigationMode }}</code></x-datafield>
            <x-datafield label="Current Page src"><code>{{ state.pageSrc }}</code></x-datafield>
            <x-datafield label="Live module count"><span>{{ state.moduleCount }}</span></x-datafield>
            <x-datafield label="Current Area"><code>{{ state.areaId }}</code></x-datafield>
            <x-datafield label="component:x-button loader"><code>{{ state.componentLoader }}</code></x-datafield>
        </x-datafields>

        <h3>Services resolved for this Page</h3>

        <ul>
            <li x-for="service in state.services" x-key="name"><code>{{ service.name }}</code> — {{ service.kind }}</li>
        </ul>

        <h3>Services versus declarative dependencies</h3>

        <pre x-pre><code>controller({ navigation, bus, loader }) {
    // runtime/service access
}

export default {
    dependencies: {
        icon: "icon:x-file"
    }
    // declarative resource loading
}</code></pre>

        <p>
            <code>dependencies</code> is resolved by the Loader before the Page class is created. The injected <code>loader</code> service is for
            resources selected dynamically at runtime. They are different mechanisms.
        </p>
    `,

    state: {
        navigationMode: "",
        pageSrc: "",
        moduleCount: 0,
        areaId: "",
        componentLoader: "",
        services: []
    },

    controller({ state, page, navigation, dialog, bus, loader, resolver, areas, modules }) {
        return {
            load() {
                // read only values through public services
                state.navigationMode = navigation.mode;
                state.pageSrc = page.src;
                state.moduleCount = modules.getModules().length;
                state.areaId = areas.getCurrentArea()?.id || "";
                state.componentLoader = resolver.resolve("component:x-button")?.definition?.loader || "(not resolved)";
                state.services = [
                    { name: "navigation", kind: navigation.constructor.name },
                    { name: "dialog", kind: dialog.constructor.name },
                    { name: "bus", kind: bus.constructor.name },
                    { name: "loader", kind: loader.constructor.name },
                    { name: "resolver", kind: resolver.constructor.name },
                    { name: "areas", kind: areas.constructor.name },
                    { name: "modules", kind: modules.constructor.name }
                ];
            }
        };
    }
};
