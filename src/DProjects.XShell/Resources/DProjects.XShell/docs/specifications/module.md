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
            "script": "./js/module.js",
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

The optional `script` points to a module script whose default export is a constructable class. Runtime requests named XShell services through
its constructor argument, supplies `params` from the final module config, and calls `start()` after script and style loads. See
[Modules](../architecture/modules.md) for current injection and script-free startup limits.

## Public module contract (planned)

A declarative contract for accepted params, Bus events, methods, and navigation intents remains a proposal. Its JSONC descriptor syntax, type
vocabulary, validation, dispatch, and failure behavior are TODOs; the runtime does not parse or enforce such a contract. Do not treat those
categories as supported schema fields yet.

See [Modules](../architecture/modules.md), [Configuration](../architecture/configuration.md), [Root Module](application.md), and
[Areas](../subsystems/areas.md).
