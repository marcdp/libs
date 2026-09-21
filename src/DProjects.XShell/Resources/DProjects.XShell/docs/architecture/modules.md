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

## Menu contributions and Areas

A module can declare reusable named menu contributions, independent of the application Area:

```jsonc
{ "modules": { "reports": { "menus": {
    "navigation": [{ "label": "Reports", "href": "/pages/report.js", "default": true }],
    "tools": [{ "label": "Export", "href": "/pages/export.js" }]
} } } }
```

The root application lists module ids in `xshell.areas.definitions.<area-id>.modules`. Modules do not choose their Area. A module may be listed in
zero, one, or several Areas. Areas composes separate effective menus for those Areas in the listed order; the first top-level navigation item
marked `default: true` provides each Area's home. Participation does not create routes, imports, or additional module instances.
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

`Modules.init()` iterates `config.modules` once. It schedules style and controller loads concurrently, constructs each controller with a
service-provider proxy, then awaits all loads and calls controller `start()` methods in parallel. The proxy supplies `params` from the final
module config; other requested names are resolved through XShell's registered services, including `navigation`, `bus`, and `config`.
The runtime does not resolve repeated-import precedence.

The proxy also has `definition` and `timer` branches that currently reference unavailable identifiers; their injection behavior is not
established. A controller-free definition receives a fallback object with `onCommand()` but no `start()`, so startup can fail. The current module
lifecycle calls `start()`; legacy sample code using `onCommand("load", ...)` does not represent this lifecycle. `Modules.stop()` calls `stop()` on
each controller, but no automatic application-shutdown lifecycle currently invokes it.

## Public communication contract

The Bus supports events and listeners. A declarative module contract for accepted params, public events, methods, and navigation intents has been
proposed but is not parsed or enforced by the runtime. Module menu contributions remain declarative
contributions; they are not module imports or extra module instances.

See [Module Specification](../specifications/module.md), [Configuration](configuration.md), [Navigation](navigation.md), and
[Areas](../subsystems/areas.md).
