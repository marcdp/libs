# Modules

The application is the **root module**. Its `module.jsonc` imports dependencies recursively. A module definition can contribute metadata,
configuration, menus, resolvers, pages, styles, components, icons, and an optional module script. These declarative contributions belong to the
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
            "imports": [
                { "url": "url:../x/module.jsonc", "params": { "mode": "normal", "debug": false } },
                { "url": "url:../x/module.jsonc", "params": { "mode": "compact" } }
            ]
        }
    }
}
```

Bootstrap registers the resolved definition URL once and fetches its JSONC once. In this example, the first registered import gives
`config.modules.x.params` the values `{ "mode": "normal", "debug": false }`. The later import does not override or merge them. Runtime
creates one live `x` instance from `config.modules.x`. Registration order depends on bootstrap's discovery order across the import graph.

The resolved `url` on a definition records its `module.jsonc` source. Static resources use `/_assets/<module-id>/...`, such as
`/_assets/x/css/styles.css` and `/_assets/test/pages/home.js`. The Service Worker maps these stable URLs to physical locations. If more
runtime objects are needed, modules can create component instances, sessions, connections, or other objects below module level.

## Module script and startup

A definition may specify `"script": "./js/module.js"`. The script's default export must be a constructable ES class with `start()`:

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

`Modules.init()` iterates `config.modules` once. It schedules style and script loads concurrently, constructs each script controller with a
service-provider proxy, then awaits all loads and calls controller `start()` methods in parallel. The proxy supplies `params` from the final
module config; other requested names are resolved through XShell's registered services, including `navigation`, `bus`, and `config`.
The runtime does not resolve repeated-import precedence.

The proxy also has `definition` and `timer` branches that currently reference unavailable identifiers; their injection behavior is not
established. A script-free definition receives a fallback controller with no `start()`, so startup can fail. The current module lifecycle calls
`start()`; legacy sample code using `onCommand("load", ...)` does not represent this lifecycle. No stop or disposal call is wired into
`Modules.init()`.

## Public communication contract

The Bus supports events and listeners. A declarative module contract for accepted params, public events, methods, and navigation intents has been
proposed but is not parsed or enforced by the runtime. Exact syntax, validation, dispatch, and errors remain TODOs. Menus remain declarative
contributions; they are not module imports or extra module instances.

See [Module Specification](../specifications/module.md), [Configuration](configuration.md), and [Navigation](navigation.md).
