# Configuration

XShell authors nested JSONC. Framework defaults, the root module, and imported module definitions contribute to one **effective configuration**:

```jsonc
{
    "app": { "name": "test-app", "label": "Test app" },
    "modules": {
        "test": {
            "label": "Test module",
            "menus": { "navigation": [{ "label": "Home", "href": "/pages/home.js", "default": true }] },
            "imports": [
                { "url": "url:../x/module.jsonc", "params": { "mode": "compact" } }
            ]
        }
    },
    "xshell": {
        "navigation": { "mode": "hash" },
        "areas": {
            "default": "main",
            "definitions": {
                "main": { "prefix": "", "modules": ["test"] }
            }
        }
    }
}
```

`app` holds application metadata. `modules` holds canonical definitions keyed by module id. `xshell` holds runtime configuration and shared
contributions. This is a root `module.jsonc` fragment, not a separate application format. Bootstrap records each resolved definition's source
`url` and params on its `modules.<module-id>` entry. The module id is the key; a duplicate `name` field is unnecessary.

`modules.<id>.menus.<name>` holds reusable, area-independent menu contributions. `xshell.areas.default` selects a default Area and
`xshell.areas.definitions.<id>` describes application composition, including `prefix` and the participating `modules` array. Areas derives
`home` from the first top-level navigation item marked `default: true`; it does not use a configured Area home.
Root ownership of Area composition is an architectural convention; imported fragments can technically contribute `xshell` settings.

## Merge and precedence

| Earlier and later values | Result |
| --- | --- |
| Plain object + plain object | Recursively merge properties |
| Array + array | Concatenate in order |
| Scalar or other value | Later value replaces earlier value |

The intended order is XShell defaults, imported dependencies, their importers, then the root module. The current bootstrap instead uses reverse
registration order, which does not guarantee dependency-first precedence for every graph. See [Bootstrap](bootstrap.md).

Repeated-import params follow a different rule from configuration merging: **the first registered import wins**. Later imports of the same
definition URL neither replace nor merge its params. That URL produces one canonical definition and one live module instance. Registration order
depends on bootstrap's module discovery order.

Because `area.modules` is an array, contributions to the same Area from multiple fragments concatenate under the generic merge rule. There is
no Area-specific replacement rule.

## URLs and immutability

Bootstrap resolves `url:` references against each JSONC source and maps module-relative paths into the configured asset namespace, currently
`/_assets/<module-id>/...`. Resolver entries are nested objects; the resolver selects a URL and loader, and the loader obtains the resource.
Area prefixes are preserved as navigation metadata during this normalization.

Bootstrap deeply freezes the effective configuration before passing it to XShell. Configuration is distinct from mutable runtime state. JSON
Schema validation in development remains intended but unimplemented. A checked-in schema exists under `xshell/schemes/`, but bootstrap does not
run it.

See [Module Specification](../specifications/module.md), [Resolvers](resolvers.md), and [ADR-0002](../adr/0002-jsonc-specifications.md).
