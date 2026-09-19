# Modules

The application is a **root module**. Its `module.jsonc` imports other modules recursively. A module can provide configuration, components, pages,
layouts, styles, icons, and an optional `module.js` implementation.

## Definition, import, instance

| Concept | Location | Meaning |
| --- | --- | --- |
| Module definition | `config.modules` | Canonical declarative data from one `module.jsonc`, keyed by real module identity. |
| Module import | A definition's `imports` | A local name, URL, and optional params identifying a desired live instance. |
| Live module instance | `xshell.modules` | Runtime object with separate params and mutable state. |

```jsonc
{
    "modules": {
        "test": {
            "imports": {
                "x": { "url": "url:../x/module.jsonc", "params": { "var1": 1111 } },
                "x2": { "url": "url:../x/module.jsonc", "params": { "var1": 333 } }
            }
        }
    }
}
```

Both imports refer to the single canonical `config.modules.x` definition. The intended runtime has `xshell.modules.x` and `xshell.modules.x2` as
separate live instances. Their source files and `/_assets/x/...` resources are shared. Instance separation applies to params and state, not static
assets. `url` on the definition records the resolved source of `module.jsonc`.

**Current state:** bootstrap deduplicates definition URLs and merges `modules.x` once. `Modules.init()` still reads dotted config keys, iterates
definitions rather than imports, and creates no separate instance for `x2`. The `xshell.modules` property exposes a `Modules` service rather than the
proposed keyed instance map. Repeated imports with distinct params are not implemented end to end.

## Configuration and implementation

Definitions may contribute nested `xshell` settings directly. Dependency contributions should merge before their importers; current bootstrap does not
guarantee that order. Imports must be cycle-safe; current URL deduplication prevents endless fetching but does not report a cycle.

A module may have a constructable implementation in `module.js`. A single source class can create separate live objects with separate runtime state.
The checked-in sample exports a class and current `Modules` creates it with `new`, then calls `onCommand("load", ...)`. That command is current
behavior, not a settled instance lifecycle API. Creation, start, stop, and disposal responsibilities remain to be specified.

## Public module contract and communication

The intended `module.jsonc` contract is declarative and separate from metadata such as label, version, icon, description, and tags. Its four public
boundaries are:

| Contract part | Meaning |
| --- | --- |
| `params` | Accepted inputs when a live instance is created, including documented types, defaults, or required status where needed. Params belong to the import/instance. |
| `events` | Public notifications emitted by an instance onto the XShell-wide Bus, distinct from internal DOM events. |
| `methods` | Public operations requested through XShell, which resolves, validates, and dispatches to a target instance; async calls should be possible. |
| `intents` | Public navigation capabilities such as `customer.detail` with `customerId`, independent of private URLs and menu items. |

```text
instance A ──event──────────────→ Bus
instance A ──method request─────→ XShell mediator → instance B
instance A ──navigation intent──→ Navigation → owning module/page
```

XShell mediation can provide target resolution, contract checks, consistent failures, logging, and future policy without direct cross-module object
references. Menus remain presentation configuration owned by a module or application. The Bus currently supports `emit` and listeners; method and
intent dispatch and module contract enforcement are planned. Exact invocation names, validation rules, and errors remain TODOs.

See [Module Specification](../specifications/module.md), [Configuration](configuration.md), and [Navigation](navigation.md).
