// contract
export const contract = {
    description: "Area composition and application context",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Areas</h2>

        <p>
            Modules contribute reusable menus and resources. Areas compose participating modules into a navigation context. Navigation then activates
            Pages within that context. A module can participate in zero Areas, one Area, or several Areas without creating extra module instances.
        </p>

        <h3>Current Area</h3>

        <x-datafields>
            <x-datafield label="Id"><code>{{ state.current.id }}</code></x-datafield>
            <x-datafield label="Label"><span>{{ state.current.label }}</span></x-datafield>
            <x-datafield label="Prefix"><code>{{ state.current.prefix }}</code></x-datafield>
            <x-datafield label="Home"><code>{{ state.current.home }}</code></x-datafield>
            <x-datafield label="Navigation items"><span>{{ state.current.navigationCount }}</span></x-datafield>
        </x-datafields>

        <h3>Configured Areas</h3>

        <p>
            This demo contributes <code>"navigation": "/pages"</code>. Areas derives its Page menu from the module's generated file inventory,
            including nested sections and their numeric ordering prefixes.
        </p>

        <ul>
            <li x-for="area in state.areas" x-key="id">
                <strong>{{ area.label }}</strong> — <code>{{ area.prefix }}</code>, home <code>{{ area.home }}</code>, modules
                <code>{{ area.modules }}</code>
            </li>
        </ul>

        <h3>Area prefix versus resource ownership</h3>

        <p>
            <code>/customers/reports</code> is a public Area/navigation context. <code>/_assets/reports/pages/report.js</code> identifies the module
            resource that owns a Page. Areas prefix navigation values during composition; they do not change which module owns the resource.
        </p>

        <pre x-pre><code>modules → reusable menu/resource contributions
areas   → participating modules + composed menus + prefix
navigation → active Page within the selected Area</code></pre>

        <p>
            This read-only view uses the public Areas API. It does not mutate Area configuration or reproduce the navigation stack examples.
        </p>
    `,

    state: {
        current: {
            id: "",
            label: "",
            prefix: "",
            home: "",
            navigationCount: 0
        },
        areas: []
    },

    controller({ state, areas }) {
        return {
            load() {
                // inspect composed Area data through public methods
                const current = areas.getCurrentArea();
                const currentNavigation = areas.getMenu("navigation");
                state.current = {
                    id: current?.id || "",
                    label: current?.label || "",
                    prefix: current?.prefix || "/",
                    home: current?.home || "(none)",
                    navigationCount: currentNavigation.length
                };
                state.areas = areas.getAreas().map(area => ({
                    id: area.id,
                    label: area.label,
                    prefix: area.prefix || "/",
                    home: area.home || "(none)",
                    modules: area.modules.join(", ") || "(none)"
                }));
            }
        };
    }
};
