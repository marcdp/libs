// contract
export const contract = {
    description: "Module menu contributions",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Menus</h2>

        <p>
            A module contributes data to named menu slots. An Area decides whether and where that module participates, then composes the effective
            menus for its own navigation context.
        </p>

        <h3>Static contribution</h3>

        <pre x-pre><code>"menus": {
    "navigation": [
        {
            "label": "Reports",
            "path": "/reports",
            "href": "/pages/report.js"
        }
    ]
}</code></pre>

        <h3>Dynamic named source</h3>

        <pre x-pre><code>"menus": {
    "navigation": "report-pages"
}

areas.registerSource("report-pages", {
    resolve() {
        return menuItems;
    }
});</code></pre>

        <p>
            A named dynamic menu source supplies the complete contribution for a menu name. It is different from an item's
            <code>childrenSource</code>, which supplies only that item's children. The <code>x-demo</code> controller registers
            <code>x-demo-dynamic-navigation-menu-source</code> from its file inventory; filename-derived paths are an x-demo convention, not a
            core XShell rule.
        </p>

        <p>
            A module can participate in zero Areas, one Area, or multiple Areas without creating extra module instances. Module contributions are
            reusable data; Areas own composition and navigation context.
        </p>

        <h3>Current x-demo contribution</h3>

        <x-datafields>
            <x-datafield label="navigation definition"><code>{{ state.navigationDefinition }}</code></x-datafield>
            <x-datafield label="tools definition"><code>{{ state.toolsDefinition }}</code></x-datafield>
            <x-datafield label="current Area"><code>{{ state.areaId }}</code></x-datafield>
            <x-datafield label="effective navigation items"><span>{{ state.itemCount }}</span></x-datafield>
        </x-datafields>

        <ul>
            <li x-for="item in state.items" x-key="id"><strong>{{ item.label }}</strong> — <code>{{ item.href }}</code></li>
        </ul>
    `,

    state: {
        navigationDefinition: "",
        toolsDefinition: "",
        areaId: "",
        itemCount: 0,
        items: []
    },

    controller({ state, config, areas }) {
        return {
            load() {
                // inspect declarative and composed menu data
                const moduleConfig = config.modules["x-demo"] || {};
                const navigation = moduleConfig.menus?.navigation;
                const tools = moduleConfig.menus?.tools;
                const items = areas.getMenu("navigation");
                state.navigationDefinition = typeof navigation === "string" ? navigation : JSON.stringify(navigation || []);
                state.toolsDefinition = JSON.stringify(tools || []);
                state.areaId = areas.getCurrentArea()?.id || "";
                state.itemCount = items.length;
                state.items = items.slice(0, 6).map((item, index) => ({
                    id: String(index),
                    label: item.label,
                    href: item.href
                }));
            }
        };
    }
};
