# Modules

The application is the **root module**. Its `module.jsonc` imports dependencies recursively. A module definition can contribute metadata,
configuration, menus, resolvers, pages, styles, components, icons, and an optional module controller. These declarative contributions belong to the
canonical definition and are not duplicated by repeated imports.

## Definition, import, and instance

| Concept | Location | Meaning |
| --- | --- | --- |
| Module definition | `config.modules.<module-id>` | Canonical declarative data keyed by stable module id. |
| Module import | A definition's `imports` array | Dependency URL and optional params for the target module. |
| Live module instance | `xshell.modules` service | One runtime record per canonical module id. |

```jsonc
{
    "modules": {
        "test": {
            "label": "Test module",
            "version": "1.0.0",
            "copyright": "",
            "icon": "",
            "imports": [
                { "configUrl": "url:../x/module.jsonc", "params": { "mode": "normal", "debug": false } },
                { "configUrl": "url:../x/module.jsonc", "params": { "mode": "compact" } }
            ]
        }
    }
}
```

Bootstrap registers the resolved definition URL once and fetches its JSONC once. In this example, the first registered import gives
`config.modules.x.params` the values `{ "mode": "normal", "debug": false }`. The later import does not override or merge them. Runtime
creates one live `x` instance from `config.modules.x`. Registration order depends on bootstrap's discovery order across the import graph.

The resolved `configUrl` on a definition records its `module.jsonc` source. Its `assetsUrl` identifies the physical asset container and defaults to
the configuration document's directory. Static resource consumers use `/_assets/<module-id>/...`, such as `/_assets/x/css/styles.css` and
`/_assets/test/pages/home.js`; the Service Worker maps these stable URLs to `assetsUrl`. The current worker supports expanded directories. A ZIP may
be used as a future physical representation, but ZIP-backed loading is not implemented. If more runtime objects are needed, modules can create
component instances, sessions, connections, or other objects below module level.

## Menu contributions and Areas

A module can declare reusable named menu contributions, independent of the application Area:

```jsonc
{ "modules": { "reports": { "menus": {
    "navigation": [{ "label": "Reports", "path": "/reports", "href": "/pages/report.js", "default": true }],
    "tools": [{ "label": "Export", "href": "/pages/export.js" }]
} } } }
```

Each named menu is either a static array or the name of a registered dynamic menu source:

```jsonc
{ "modules": { "reports": { "menus": {
    "navigation": "report-pages",
    "tools": [{ "label": "Export", "href": "/pages/export.js" }]
} } } }
```

The string is a runtime lookup identifier, not a URL or resolver entry. A module controller can register it with
`Areas.registerSource("report-pages", { resolve: () => menuItems })`. Its `resolve()` result supplies the complete named menu contribution.
This is distinct from an item's `childrenSource`, which supplies only that item's children. The module configuration remains declarative and
readonly; runtime menu data is not written back into `config.modules`. Static and dynamic menu items use the same navigation shape: `path` is an
optional friendly/public alias and `href` is the canonical XShell navigation target. Menu UIs use `path || href`, so href-only contributions
remain compatible. Filename-derived paths in the bundled `x-demo` dynamic source are a demo convention, not an XShell core requirement.

The root application lists module ids in `xshell.areas.definitions.<area-id>.modules`. Modules do not choose their Area. A module may be listed in
zero, one, or several Areas. Areas composes separate effective menus for those Areas in the listed order; the first navigation item marked
`default: true` in depth-first traversal provides each Area's home. Participation does not create routes, imports, or additional module instances.
See [Areas](../subsystems/areas.md) for prefix and resource ownership.

## Module controller and startup

A definition may specify `"controller": "./js/module.js"`. `Modules` loads it as `module:<controller>`. The JavaScript module's default export must
be constructable and provide `start()`:

```js
export default class {
    constructor({ navigation, bus, config, params }) {
        this.navigation = navigation;
        this.bus = bus;
        this.config = config;
        this.params = params;
    }

    async start() {
    }
}
```

`Modules.init()` iterates `config.modules` once, creating one live record per effective entry. It schedules every style fetch and controller load
concurrently, awaits all of them, and then calls all controller `start()` methods concurrently. Only after startup does it freeze the module records
and append the loaded stylesheets to `document.adoptedStyleSheets`. The proxy supplies `params` from the final module config; other requested names
are resolved through XShell's registered services, including `navigation`, `bus`, and `config`. The runtime does not resolve repeated-import
precedence.

XShell runs `Modules.init()` before `Areas.init()`. Controllers can therefore register dynamic menu sources before Areas reads module menu
contributions, composes effective Area menus, and starts Navigation.

The proxy also has `definition` and `timer` branches that currently reference unavailable identifiers; their injection behavior is not
established. A controller-free definition receives a fallback object with `onCommand()` but no `start()`, so startup can fail. The current module
lifecycle calls `start()`; legacy sample code using `onCommand("load", ...)` does not represent this lifecycle. `Modules.stop()` calls `stop()` on
each controller, but no automatic application-shutdown lifecycle currently invokes it.

## Public communication contract

The effective-configuration schema accepts `contract.events`, `contract.actions`, and `contract.intents`. Events declare published event names and
optional detail fields. Actions declare request/response capabilities with input fields and an optional output description. Intents declare
semantic fire-and-forget requests with input fields. This is declarative schema support only: the current runtime does not register, dispatch, or
enforce actions or intents, and it does not enforce event payload declarations. The Bus independently supports events and listeners. Module menu
contributions remain declarative contributions; they are not module imports or extra module instances.

See [Module Specification](../specifications/module.md), [Configuration](configuration.md), [Navigation](navigation.md), and
[Areas](../subsystems/areas.md).
