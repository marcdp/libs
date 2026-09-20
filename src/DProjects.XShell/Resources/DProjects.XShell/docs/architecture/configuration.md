# Configuration

XShell authors nested JSONC. Framework defaults, the root module, and imported module definitions contribute to one **effective configuration**:

```jsonc
{
    "app": { "name": "test-app", "label": "Test app" },
    "modules": {
        "test": {
            "label": "Test module",
            "imports": [
                { "url": "url:../x/module.jsonc", "params": { "mode": "compact" } }
            ]
        }
    },
    "xshell": { "navigation": { "mode": "hash" } }
}
```

`app` holds application metadata. `modules` holds canonical definitions keyed by module id. `xshell` holds runtime configuration and shared
contributions. This is a root `module.jsonc` fragment, not a separate application format. Bootstrap records each resolved definition's source
`url` and params on its `modules.<module-id>` entry. The module id is the key; a duplicate `name` field is unnecessary.

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

## URLs and immutability

Bootstrap resolves `url:` references against each JSONC source and maps module-relative paths into the configured asset namespace, currently
`/_assets/<module-id>/...`. Resolver entries are nested objects; the resolver selects a URL and loader, and the loader obtains the resource.

Bootstrap deeply freezes the effective configuration before passing it to XShell. Configuration is distinct from mutable runtime state. JSON
Schema validation in development remains intended but unimplemented; no validation step or schema exists.

See [Module Specification](../specifications/module.md), [Resolvers](resolvers.md), and [ADR-0002](../adr/0002-jsonc-specifications.md).
