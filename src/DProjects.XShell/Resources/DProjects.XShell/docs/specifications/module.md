# Module Specification

A `module.jsonc` file is declarative JSONC. Its `modules` object contains canonical definitions keyed by module id. A root file may also
provide `app` metadata. Any module file may contribute nested `xshell` settings. Bootstrap records each loaded definition's resolved
`configUrl` and `assetsUrl`; the module id does not need a duplicate `name` field.

```jsonc
{
    "modules": {
        "orders": {
            "label": "Orders",
            "version": "1.0.0",
            "copyright": "",
            "icon": "",
            "styles": ["/css/orders.css"],
            "controller": "./js/module.js",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            },
            "imports": [
                { "configUrl": "url:../customer/module.jsonc", "params": { "region": "eu" } }
            ],
            "menus": {
                "navigation": [{ "label": "Orders", "href": "/pages/orders.js", "default": true }],
                "tools": [{ "label": "New order", "href": "/pages/new-order.js" }]
            }
        }
    }
}
```

An import declares a dependency on the definition at `configUrl` and may provide params for that target module. Imports are array entries, not
local instance names. Bootstrap registers a resolved URL once, loads its JSONC once, and retains params from the **first registered import**.
Later imports of the same URL do not override or merge params. Discovery order determines which import registers first. One URL contributes one
canonical `config.modules` entry and one live runtime instance for its module id.

Definition metadata and contributions, including menus, resolvers, pages, and styles, belong to the canonical definition. Import params are
runtime input, not definition metadata. `configUrl` identifies the module configuration document. `assetsUrl` identifies its physical asset
container and defaults to the configuration document's directory. Module-relative static paths normalize into `/_assets/<module-id>/...`; the
Service Worker maps those virtual URLs to `assetsUrl`, hiding the physical representation from normal resource consumers. Expanded directories are
supported; ZIP-backed assets are not yet implemented.

`modules.<module-id>.menus.<menu-name>` is an area-independent contribution to a named menu slot. The root application selects participating
modules through `xshell.areas.definitions.<area-id>.modules`; a child module does not declare Area membership. One module can contribute to multiple
Areas without creating another runtime module instance. Menu entries are navigation data, not imports or route declarations. Bootstrap normalizes
authored module-relative hrefs such as `/pages/orders.js` into the module asset namespace before Areas applies an Area prefix. The first
navigation item marked `default: true` in depth-first Area module order determines that Area's home; absent such an item, home is null.

The optional `controller` points to a JavaScript module loaded through `module:<controller>`. Its default export must be constructable. Runtime
requests named XShell services through its constructor argument, supplies `params` from the final module config, and calls `start()` after controller
and style loads. `Modules.stop()` can call controller `stop()`, but no automatic application-shutdown lifecycle currently invokes it. See
[Modules](../architecture/modules.md) for current injection and controller-free startup limits.

`defaults.page`, `defaults.dialog`, and `defaults.component` are allowed module defaults in the checked-in schema. Page and Component render/state
engines use resource `meta` first, then their module default, then the XShell default. Dialog operations consume only `xshell.defaults.dialog`.
No current tooling uses render-engine defaults for publish-time X-template compilation.

The effective schema requires `label`, `version`, `copyright`, `icon`, `configUrl`, and `assetsUrl` for every resolved module. Bootstrap supplies the
two URLs during normalization. Optional fields are `params`, `styles`, `controller`, `imports`, `menus`, `defaults`, and `contract`.

## Public module contract

The schema explicitly supports:

- `events`: things the module publishes; an event may describe named fields in its `detail` payload;
- `actions`: request/response public capabilities; an action may describe named `input` fields and one typed `output`; and
- `intents`: semantic fire-and-forget requests; an intent may describe named `input` fields.

Typed fields use `string`, `number`, `integer`, `boolean`, `object`, or `array`; input/detail fields may also carry `required`, `default`, and
`description` metadata. These declarations are accepted and validated as configuration. The current browser runtime does not register, dispatch,
or enforce actions or intents, and it does not enforce declared event payloads. The Bus is a separate event mechanism.

See [Modules](../architecture/modules.md), [Configuration](../architecture/configuration.md), [Root Module](application.md), and
[Areas](../subsystems/areas.md).
