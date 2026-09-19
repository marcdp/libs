# Configuration

XShell configuration is authored as JSONC with ordinary nested objects. The application is its root module; imported module definitions and framework
defaults contribute to one **effective configuration**.

```jsonc
{
    "app": { "name": "test-app", "label": "Test app", "version": "0.1.0" },
    "modules": {
        "test": { "label": "Test module", "imports": { "x": { "url": "url:../x/module.jsonc" } } }
    },
    "xshell": { "navigation": { "mode": "hash" } }
}
```

The example is a root module fragment, not a separate application file format. `app` holds root/application metadata; `modules` holds canonical
definitions keyed by their real identity; `xshell` holds framework settings and module contributions. Import keys and params describe prospective live
instances, not extra canonical definitions. The resolved `url` on a definition records its source location.

## Merge and precedence

| Earlier and later values | Result |
| --- | --- |
| Plain object + plain object | Recursively merge properties |
| Array + array | Concatenate in order |
| Scalar or other value | Later value replaces earlier value |

Order: framework defaults, deepest imported dependencies, their importers, then the root module. Dependencies contribute first; importers may override
them. A definition contributes once even when multiple imports reference it. Cycles must produce a bounded failure. **Current bootstrap** implements
the three value rules and URL deduplication, but its registration-order merge does not guarantee dependency-first precedence or report cycles.

## URLs, resolvers, and immutability

Bootstrap resolves `url:` references against their JSONC source and maps module-relative resource paths into the configured asset namespace (currently
`/_assets`). Resolver entries are structured objects:

```jsonc
{
    "xshell": {
        "resolver": {
            "component": {
                "ace-editor": { "url": "https://example.com/ace.js", "loader": "module-js" }
            }
        }
    }
}
```

The resolver chooses a URL and loader; the loader performs loading. The final merged configuration should be validated as one object with JSON Schema
in debug/development mode, then deeply frozen before XShell receives it. JSONC is the authoring format; browsers do not provide native JSON Schema
validation. There is currently no schema or `schemes` directory, no validation step, and no deep freeze. `Config` freezes only its top-level object
and retains dotted-key lookup methods, while several services use those methods. The nested model is therefore partially migrated.

See [Module Specification](../specifications/module.md), [Resolvers](resolvers.md), and [ADR-0002](../adr/0002-jsonc-specifications.md).
