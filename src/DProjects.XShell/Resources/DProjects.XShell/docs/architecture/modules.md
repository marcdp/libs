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
declarative, and Navigation does not consume routes yet.

## Menu contributions and Areas

A module can declare named menu contributions. Each is either a static array or the name of a dynamic source registered with
`Areas.registerSource()`. Areas compose these contributions from module ids; Area membership does not create dependencies or additional runtime
module instances. See [Areas](../subsystems/areas.md).

## Module controller and runtime instance

`Modules.init()` iterates the effective `config.modules` map once. It creates one record per canonical id, loads styles and an optional controller,
supplies the final `params` to that controller, and exposes the effective module's normalized `routes` declarations. Repeated dependency references
therefore still produce one runtime instance. `module.routes` is the effective configuration object itself; when routes are absent it is a frozen empty
object. The effective configuration is immutable by the time runtime modules are initialized.

Retrieve a runtime module with `xshell.modules.getModuleById(id)` or enumerate instances with `xshell.modules.getModules()`. `module.routes` is
declarative metadata only: Areas will compose routes and Navigation will interpret them in later work.

See [Module Specification](../specifications/module.md), [Configuration](configuration.md), [Bootstrap](bootstrap.md), and
[Service Worker](service-worker.md).
