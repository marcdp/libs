// contract
export const contract = {
    description: "Modules page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Modules"
    },
    template: `
        <x-listview view="details">
            <x-datafield type="search" x-model="state.id" placeholder="Module" slot="column" style="width:12em"></x-datafield>
            <div slot="column">
                <x-datafield slot="column" type="search" x-model="state.label" placeholder="Label"></x-datafield>
            </div>
            <div slot="column" style="width:5em">
                Version
            </div>
            <div slot="column" style="width:8em">
                Assets path
            </div>
            <div slot="column" style="width:6em; text-align:right">
                Size
            </div>
            <div slot="column" style="width:5em; text-align:right">
                Files
            </div>
            <div slot="column" style="width:5em; text-align:right">
                Pages
            </div>
            <div slot="column" style="width:5em; text-align:right">
                Components
            </div>
            <div slot="column" style="width:5em; text-align:right">
                Services
            </div>
            <div slot="column" style="width:5em; text-align:right">
                Contracts
            </div>
            <div slot="column" style="width:5em; text-align:right">
                Others
            </div>
            <div slot="column" style="width:5em; text-align:right">
                Status
            </div>
            <x-listview-item 
                x-for="module in state.modules"
                x-attr:href="module.configUrl"
                x-attr:label="module.id"
                x-attr:description="module.description"
                x-attr:icon="module.icon"
                target="_blank"
            >
                <div>{{ module.label }}</div>
                <div>{{ module.version }}</div>
                <div>{{ module.assetsPath }}</div>
                <x-file-size x-prop:value="module.size" style="text-align:right"></x-file-size>
                <div style="text-align:right">{{ module.files.all }}</div>
                <div style="text-align:right">{{ module.files.pages }}</div>
                <div style="text-align:right">{{ module.files.components }}</div>
                <div style="text-align:right">{{ module.files.services }}</div>
                <div style="text-align:right">{{ module.files.contracts }}</div>
                <div style="text-align:right">{{ module.files.others }}</div>
                <div style="text-align:right">{{ module.status }}</div>
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        version: "",
        label: "",
        assetsPath: "",
        configUrl: "",
        configUrlName: "",
        size: "",
        files: 0,
        modules: null
    },
    controller({ state, events, modules }) {
        return {
            load() {
               // load
               this.refresh();
               events.on(state, "change:id", "refresh");
               events.on(state, "change:version", "refresh");
               events.on(state, "change:label", "refresh");
               events.on(state, "change:assetsPath", "refresh");
               events.on(state, "change:configUrl", "refresh");
            },
            refresh() {
                // refresh
                let list = [];
                for(let target of modules.getModules()) {
                    let valid = true;
                    if (state.id && target.id.indexOf(state.id) == -1 ) valid = false;
                    if (state.version && (target.config.version || "").indexOf(state.version) == -1 ) valid = false;
                    if (state.label && (target.label || "").indexOf(state.label) == -1 ) valid = false;
                    if (state.assetsPath && (target.assetsPath || "").indexOf(state.assetsPath) == -1 ) valid = false;
                    if (state.configUrl && (target.config.configUrl || "").indexOf(state.configUrl) == -1 ) valid = false;
                    let size = 0;
                    let files = {
                        all: 0,
                        pages: 0,
                        components: 0,
                        services: 0,
                        contracts: 0,
                        others: 0
                    }
                    for(const file of target.config.files) {
                        size += file.size || 0;
                        files.all++;
                        if (file.path.startsWith(target.config.assetsPath + "/pages")) {
                            files.pages++;
                        } else if (file.path.startsWith(target.config.assetsPath + "/components")) {
                            files.components++;
                        } else if (file.path.startsWith(target.config.assetsPath + "/services")) {
                            files.services++;
                        } else if (file.path.startsWith(target.config.assetsPath + "/contracts")) {
                            files.contracts++;
                        } else {
                            files.others++;
                        }
                    }
                    if (valid) {
                        list.push({
                            id: target.id,
                            version: target.config.version || "",
                            label: target.label || "",
                            icon: target.config.icon || "x-module",
                            assetsPath: target.config.assetsPath,
                            configUrl: target.config.configUrl,
                            configUrlName: target.config.configUrl.split("/").pop() || "",
                            files: files,
                            size: size,
                            status: "loaded"
                        });
                    }
                };
                state.modules = list;
            }
        };
    }
}
