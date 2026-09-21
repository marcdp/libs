# Module Specification

A `module.jsonc` file is declarative JSONC. Its `modules` object contains canonical definitions keyed by module id. A root file may also
provide `app` metadata. Any module file may contribute nested `xshell` settings. Bootstrap records each loaded definition's resolved
`url` as source provenance; the module id does not need a duplicate `name` field.

```jsonc
{
    "modules": {
        "orders": {
            "label": "Orders",
            "version": "1.0.0",
            "styles": ["/css/orders.css"],
            "controller": "./js/module.js",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            },
            "imports": [
                { "url": "url:../customer/module.jsonc", "params": { "region": "eu" } }
            ],
            "menus": {
                "navigation": [{ "label": "Orders", "href": "/pages/orders.js", "default": true }],
                "tools": [{ "label": "New order", "href": "/pages/new-order.js" }]
            }
        }
    }
}
```

An import declares a dependency on the definition at `url` and may provide params for that target module. Imports are array entries, not
local instance names. Bootstrap registers a resolved URL once, loads its JSONC once, and retains params from the **first registered import**.
Later imports of the same URL do not override or merge params. Discovery order determines which import registers first. One URL contributes one
canonical `config.modules` entry and one live runtime instance for its module id.

Definition metadata and contributions, including menus, resolvers, pages, and styles, belong to the canonical definition. Import params are
runtime input, not definition metadata. Module-relative static paths normalize into `/_assets/<module-id>/...`; the Service Worker maps those
virtual URLs to the definition's physical resource location.

`modules.<module-id>.menus.<menu-name>` is an area-independent contribution to a named menu slot. The root application selects participating
modules through `xshell.areas.definitions.<area-id>.modules`; a child module does not declare Area membership. One module can contribute to multiple
Areas without creating another runtime module instance. Menu entries are navigation data, not imports or route declarations. Bootstrap normalizes
authored module-relative hrefs such as `/pages/orders.js` into the module asset namespace before Areas applies an Area prefix. The first
top-level navigation item marked `default: true` in Area module order determines that Area's home; absent such an item, home is null.

The optional `controller` points to a JavaScript module loaded through `module:<controller>`. Its default export must be constructable. Runtime
requests named XShell services through its constructor argument, supplies `params` from the final module config, and calls `start()` after controller
and style loads. `Modules.stop()` can call controller `stop()`, but no automatic application-shutdown lifecycle currently invokes it. See
[Modules](../architecture/modules.md) for current injection and controller-free startup limits.

`defaults.page`, `defaults.dialog`, and `defaults.component` are allowed module defaults in the checked-in schema. Component render/state engines
consume `defaults.component`. The checked-in Page loader still consumes legacy top-level `page` rather than `defaults.page`, despite the X module
already authoring the new location. Dialog operations consume only `xshell.defaults.dialog`. These are current migration limitations, not additional
supported precedence rules.

The effective-configuration schema still accepts legacy top-level module `page` and `component` objects. It also contains a stale
`$defs/modulePage` reference to missing `#/$defs/dialog`; therefore it is not yet a finalized module contract.

## Public module contract (planned)

A declarative contract for accepted params, Bus events, methods, and navigation intents remains a proposal. Its JSONC descriptor syntax, type
vocabulary, validation, dispatch, and failure behavior are TODOs; the runtime does not parse or enforce such a contract. Do not treat those
categories as supported schema fields yet.

See [Modules](../architecture/modules.md), [Configuration](../architecture/configuration.md), [Root Module](application.md), and
[Areas](../subsystems/areas.md).
