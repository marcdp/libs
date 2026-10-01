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

`app` is local because it has no `configUrl`. `x` is a reference. Both root and child references may contribute `params` or other configuration
to the canonical effective module.

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

Bootstrap derives that order from the dependency graph, retains sibling declaration/discovery order, and rejects cycles. Reference contributions
merge after the referenced local definition; the root merges last and therefore has final authority. Fetch completion order has no effect.

## Defaults, URLs, and validation

Every effective module requires `defaults.page` and `defaults.component`, each with non-empty `renderEngine` and `stateEngine`. These module defaults
are separate from global `xshell.ui` settings.

`url:` values resolve against the JSONC document where they were authored. Bootstrap gives each local definition its document URL as `configUrl`
and defaults `assetsUrl` to the document directory. It then maps module-relative runtime paths into `/_assets/<module-id>/...`.

The canonical schema describes the final merged object, not partial authored references. Effective validation occurs after bootstrap normalization.
The required `xshell.i18n` section supplies the current language, available languages, date/time formats, and translation dictionaries. The X module
provides the baseline values, and application composition may override them through the normal merge precedence.
An effective module may optionally declare `routes`, an object mapping friendly application URL patterns to module-relative Page targets. During
bootstrap, route target values are normalized into the owning module's `/_assets/<module-id>/...` namespace; route keys remain application-facing
patterns unchanged. Routes are distinct from module `menus`: menus describe user-visible navigation declarations, while routes are application
URL-to-Page declarations. Runtime modules expose the normalized object as `module.routes`; Areas compose ordered `area.routes` from participating
modules without adding their prefixes, and Navigation performs forward and reverse route resolution. See [Modules](modules.md),
[Areas](../subsystems/areas.md), and [Navigation](navigation.md).
Bootstrap deeply freezes the result before `xshell.init(config)`.

See [Modules](modules.md), [Bootstrap](bootstrap.md), and [Specifications](../specifications/).
