# Configuration

XShell authors JSONC fragments and produces one normalized **effective configuration** with `app`, `modules`, and `xshell` sections.

## Authored module configuration

Every module configuration has exactly one local definition and zero or more external references:

```jsonc
{
    "modules": {
        "app": {
            "label": "Application",
            "version": "1.0.0",
            "copyright": "",
            "icon": "",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            }
        },
        "x": {
            "configUrl": "url:../x/module.jsonc",
            "params": { "mode": "compact" }
        }
    }
}
```

`app` is local because it has no `configUrl`. `x` is a reference. The example is valid only for the root application because it configures
dependency `params`; a reusable child module must omit those params.

## Effective configuration

After discovery and normalization, each reference is replaced through merging by its complete canonical definition:

```jsonc
{
    "modules": {
        "app": {
            "label": "Application",
            "configUrl": "https://example.test/modules/app/module.jsonc",
            "assetsUrl": "https://example.test/modules/app/"
        },
        "x": {
            "label": "X",
            "version": "1.0.0",
            "configUrl": "https://example.test/modules/x/module.jsonc",
            "assetsUrl": "https://example.test/modules/x/",
            "params": { "mode": "compact" }
        }
    }
}
```

The module key is the identity; duplicate `name`, `id`, or `moduleId` fields are unnecessary. `configUrl` locates `module.jsonc`, while `assetsUrl`
locates the physical resources. They are distinct from the virtual `/_assets/<id>/...` namespace. The local definition owns `assetsUrl`; references
cannot override it.

## Merge and precedence

| Values | Result |
| --- | --- |
| Plain object + plain object | Recursively merge properties. |
| Array + array | Concatenate in merge order. |
| Scalar or other value | Later value replaces earlier value. |

Merge order is deterministic:

```text
XShell defaults
    -> dependency module definitions
    -> their dependents
    -> root application configuration
```

Bootstrap derives that order from the dependency graph, sorts sibling references by id, and rejects cycles. Root composition therefore has final
authority over dependency params and permitted overrides; fetch completion order has no effect.

## Defaults, URLs, and validation

Every effective module requires `defaults.page` and `defaults.component`, each with non-empty `renderEngine` and `stateEngine`. These module defaults
are separate from global `xshell.ui` settings.

`url:` values resolve against the JSONC document where they were authored. Bootstrap gives each local definition its document URL as `configUrl`
and defaults `assetsUrl` to the document directory. It then maps module-relative runtime paths into `/_assets/<module-id>/...`.

The canonical schema describes the final merged object, not partial authored references. Effective validation occurs after bootstrap normalization.
Bootstrap deeply freezes the result before `xshell.init(config)`.

See [Modules](modules.md), [Bootstrap](bootstrap.md), and [Specifications](../specifications/).
