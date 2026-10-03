# Services

XShell services are named runtime capabilities. A contract is the stable public abstraction for a capability, while an implementation is the
concrete ES module class selected by `xshell.services`.

```jsonc
"xshell": {
    "services": {
        "toast": {
            "contract": "toast",
            "implementation": "/_assets/x/services/toast-default.js"
        }
    }
}
```

During startup, XShell discovers and validates contracts, registers its core/runtime service instances, and calls `Services.init()`. That method
validates every configured contract reference, eagerly loads each configured implementation class, registers the implementation metadata, and then
finalizes the registry. Configured service instances are not constructed during initialization.

`Services.register()` is therefore an initialization-time API. Core/runtime services are registered before `Services.init()`, and configured
services from `xshell.services` are registered during it. Once initialization completes, the registry topology is immutable: modules, Components,
and Pages can resolve existing services but cannot dynamically add or replace providers after startup. A later `Services.register()` call throws a
registry-immutability error. Service objects may still own mutable runtime state; freezing the registry does not freeze those objects.

This fixed topology provides deterministic service resolution and prevents provider mutation while the application is running.

## Lookup and construction

`Services.has(name)` reports whether a runtime or configured service name is registered. It only checks registry membership and never constructs a
configured service. `Services.resolve(name)` returns an existing runtime instance or lazily constructs the configured implementation as a singleton.
The implementation is validated against the contract's declared methods and properties when that singleton is first created. Property validation
uses descriptors on the instance and its prototype chain, so it does not read values or invoke getters.

```text
startup
    -> register core/runtime instances
    -> Services.init()
    -> load configured implementation classes
    -> register implementation metadata
    -> finalize registry topology

runtime
    -> Services.has(name)       existence only
    -> Services.resolve(name)   lazy singleton construction and contract validation
```

## Module requirements and controller access

A module may declare `requires` as service names that must already exist in the finalized registry:

```jsonc
"orders": {
    "requires": ["toast", "identity"]
}
```

XShell checks these names with `Services.has()` before loading or starting any module controller. A requirement validates availability without
constructing the service. These concepts remain distinct:

- `module.requires` declares services the module expects the runtime to provide;
- implementation `dependencies` declares concrete resources for a Component or Page and loads them through Resolver and Loader; and
- `controller({ serviceName })` resolves and injects an existing runtime service when the controller accesses it.

See [Modules](modules.md), [Module Specification](../specifications/module.md), and [Loaders](loaders.md).
