# Modules

The application is the **root module**. Each `module.jsonc` contains one local module definition and zero or more references to external module
definitions. The `modules` object is both the module identity registry and the dependency graph; there is no separate dependency collection.

## Definition, reference, effective module, and instance

| Concept | Location | Meaning |
| --- | --- | --- |
| Local module definition | The one `modules.<id>` entry without `configUrl` | The module owned by the current configuration document. |
| External module reference | A `modules.<id>` entry with `configUrl` | A dependency whose key is the expected canonical module id. |
| Reference contribution | Properties on an external reference besides `configUrl` | Configuration merged into the canonical effective module. |
| Canonical effective module | Final `config.modules.<id>` | The resolved definition plus root composition values and normalized URLs. |
| Runtime module instance | `xshell.modules` | One runtime record per canonical effective module id. |

```jsonc
{
    "modules": {
        "orders": {
            "label": "Orders",
            "version": "1.0.0",
            "copyright": "",
            "icon": "",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            }
        },
        "x": {
            "configUrl": "url:../x/module.jsonc"
        }
    }
}
```

Here `orders` is local and `x` is a dependency. Property order has no meaning. Bootstrap requires exactly one entry without `configUrl`; it rejects
documents with zero or several local definitions.

The key is authoritative identity. A reference named `x` must resolve to a document whose local definition is also `modules.x`. Bootstrap rejects an
identity mismatch, one id mapped to different URLs, or two ids that point at a document defining only one of them.

## Reference contributions and root composition

Any reference may contribute configuration in addition to its canonical id and `configUrl`. The root file is identified because the host's
`xshell:app.configPath` points to it:

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

The root requires no `root`, `isRoot`, or module-type flag. Its status comes only from `app.configPath`.

## Discovery and canonical identity

Bootstrap follows references recursively, resolves each `configUrl` against the document where it was authored, and deduplicates by resolved URL.
References introduced in one discovery pass are collected in a `Set`-equivalent map, so `A -> X` and `B -> X` fetch X once. The final effective
configuration contains the full resolved definition, not the lightweight reference stub.

Dependencies are merged before their dependents, with the root last. A reference contribution merges after the referenced local definition and
before its owner module's own configuration. Sibling traversal follows declaration/discovery order, so fetch completion timing cannot select
precedence. Dependency cycles are rejected because they make this precedence ambiguous.

The normalized `configUrl` records the definition document. `assetsUrl` identifies its physical resource container and defaults to that document's
directory. References cannot override `assetsUrl`; physical resource ownership stays with the local definition. Runtime resources use
`/_assets/<module-id>/...`; the Service Worker maps that stable namespace to `assetsUrl`.

The optional module `routes` object follows the same normalization boundary: its values are module-relative Page targets authored in the module
configuration and become `/_assets/<module-id>/...` paths during bootstrap. Its application-facing keys are not normalized. Route placeholders remain
declarative module metadata; Navigation interprets them only after Areas compose the participating modules' routes.

## Menu contributions and Areas

A module can declare named menu contributions. Each is either a static array or the name of a dynamic source registered with
`Areas.registerSource()`. Areas compose these contributions from module ids; Area membership does not create dependencies or additional runtime
module instances. See [Areas](../subsystems/10-areas.md).

## Required services

The optional module-level `requires` array names services that must be present for the module to function:

```jsonc
"orders": {
    "requires": ["toast"]
}
```

After `Services.init()` has finalized the service registry, `Modules.init()` checks all module requirements with `Services.has()` before loading or
starting any module controller. A missing service fails startup with the module id and service name. The check does not resolve or construct a
configured lazy service.

`module.requires` is service availability metadata. It is distinct from a Component or Page implementation's `dependencies`, which names concrete
resources loaded through Resolver and Loader, and from `controller({ serviceName })`, which resolves an existing service for runtime use. See
[Services](100-services.md).

## Module controller and runtime instance

`Modules.init()` iterates the effective `config.modules` map once. It creates one record per canonical id, loads styles and an optional controller,
makes final params available as `moduleConfig.params` to that controller, and exposes the effective module's normalized `routes` declarations.
Repeated dependency references
therefore still produce one runtime instance. `module.routes` is the effective configuration object itself; when routes are absent it is a frozen empty
object. Before creating those instances, XShell validates every module's service requirements. The effective configuration is immutable by the time
runtime modules are initialized.

Retrieve a runtime module with `xshell.modules.getModuleById(id)` or enumerate instances with the frozen snapshot `xshell.modules.registry`.
`module.routes` is declarative metadata: Areas compose it, and Navigation uses the resulting ordered Area routes for forward and reverse resolution.

See [Module Specification](../specifications/module.md), [Configuration](20-configuration.md), [Bootstrap](10-bootstrap.md), and
[Service Worker](110-service-worker.md).

## Controller access and startup order

Module constructors receive `moduleAssetsPath`, `moduleConfig` (including `id` and params), or named registered services via the provider proxy.
There is no direct injected `params` value.
`Modules.start()` awaits controllers sequentially in reverse runtime-record order; on failure it stops already-started records in reverse start order.
Normal `stop()` visits records in forward order. Controller constructors are created while iterating the effective module map, not on service lookup.
The optional module-wide `/styles/index.css` is loaded only when its path appears in the module inventory. `Modules` requests it as a `style:`
resource through Resolver → Loader → `style-css`, retains the resulting `CSSStyleSheet` in `module.styles`, and adopts it after successful startup.
Loader caching ensures the conventional stylesheet is fetched once per module. Relative CSS `@import` and `url(...)` references resolve against the
stylesheet containing them, including recursively imported stylesheets.
