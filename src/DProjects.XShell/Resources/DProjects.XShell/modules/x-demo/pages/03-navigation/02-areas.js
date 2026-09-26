// contract
export const contract = {
    description: "Area navigation contexts",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Areas</h2>

        <p>
            An Area is a navigation and application-composition context. A Module contributes reusable Pages and menu definitions; an Area selects
            participating modules, composes their menus, and applies a navigation prefix. It does not create a second module instance or duplicate
            the module's resources.
        </p>

        <h3>Current runtime context</h3>

        <x-datafields>
            <x-datafield label="Area id"><span>{{ state.current.id }}</span></x-datafield>
            <x-datafield label="Area label"><span>{{ state.current.label }}</span></x-datafield>
            <x-datafield label="Area prefix"><code>{{ state.current.prefix }}</code></x-datafield>
            <x-datafield label="Current Page src"><code>{{ state.pageSrc }}</code></x-datafield>
        </x-datafields>

        <p>
            The current application configures one useful Area. The list below is read from the public <code>areas</code> service rather than from a
            hard-coded example.
        </p>

        <ul>
            <li x-for="area in state.areas" x-key="id">
                <strong>{{ area.label }}</strong> — prefix <code>{{ area.prefix }}</code>, modules <code>{{ area.modules }}</code>.
                <x-anchor x-attr:href="area.home">Open Area home</x-anchor>
            </li>
        </ul>

        <h3>Prefix versus resource owner</h3>

        <p>
            This Page's canonical destination is
            <code>/demo/_assets/x-demo/pages/03-navigation/02-areas.js</code>:
        </p>

        <ul>
            <li><code>/demo</code> is the Area navigation context.</li>
            <li><code>/_assets/x-demo/...</code> identifies the resource owned by the <code>x-demo</code> Module.</li>
        </ul>

        <p>
            <x-anchor href="/demo/_assets/x-demo/pages/03-navigation/02-areas.js">Open this canonical Area-aware href</x-anchor>
        </p>

        <p>
            The friendly menu path <code>/demo/navigation/areas</code> resolves to the same Page. Navigation performs that path-to-href translation
            before <code>x-page</code> removes the Area prefix for module resource loading.
        </p>

        <p>
            <x-anchor href="/demo/navigation/areas">Open the friendly path</x-anchor>
        </p>
    `,

    state: {
        current: {
            id: "",
            label: "",
            prefix: ""
        },
        areas: [],
        pageSrc: ""
    },

    controller({ state, areas, page }) {
        return {
            load() {
                // expose the configured Area data through simple template values
                const current = areas.getCurrentArea();
                state.current = {
                    id: current?.id ?? "",
                    label: current?.label ?? "",
                    prefix: current?.prefix || "/"
                };
                state.areas = areas.getAreas().map(area => ({
                    id: area.id,
                    label: area.label,
                    prefix: area.prefix || "/",
                    modules: area.modules.join(", "),
                    home: area.home
                }));
                state.pageSrc = page.src;
            }
        };
    }
};
