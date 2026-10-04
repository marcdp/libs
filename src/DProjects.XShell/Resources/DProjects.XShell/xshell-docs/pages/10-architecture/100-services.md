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
The implementation is validated against the contract's declared methods and properties when that singleton is first created.

`getServiceItems()` exposes live runtime/diagnostic metadata. Callers must treat its returned records as read-only observational data; mutating them
is unsupported. The registry topology remains immutable after `Services.init()`, while an individual record's internal `state` and `instance` can
change as a configured service is resolved.

For a declared contract property, V0 validation succeeds when a descriptor with that name exists on the instance or its prototype chain. Validation
checks presence only: it does not read values, invoke getters, or validate runtime property types. `readonly: true` is contract metadata, and V0
does not require non-readonly properties to have a setter or writable descriptor; mutability semantics are not enforced.

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
    "requires": ["toast"]
}
```

XShell checks these names with `Services.has()` before loading or starting any module controller. A requirement validates availability without
constructing the service. These concepts remain distinct:

- `module.requires` declares services the module expects the runtime to provide;
- implementation `dependencies` declares concrete resources for a Component or Page and loads them through Resolver and Loader; and
- `controller({ serviceName })` resolves and injects an existing runtime service when the controller accesses it.

See [Modules](30-modules.md), [Module Specification](../specifications/module.md), and [Loaders](90-loaders.md).

## Contract discovery and identity

`/contracts/*.json` resources (including nested paths) enter the module inventory. Bootstrap converts their relative path without `.json` into a
global id and generates an exact `contract:<id>` rule. Resolver selects `object-json`; Loader reads JSON; Contracts validates it against
`contract.schema.json` and records physical module ownership. Duplicate global ids or conflicting exact resolver entries fail bootstrap.
A module prefix is not required unless it is part of the chosen id. Registration names are independent: multiple services can use one contract.

For example, `x/contracts/toast.json` defines id `toast` and registration `xshell.services.toast.contract = "toast"`.
The implementation URL is imported through `module:` and must default-export a class.

## Service contract schema

A contract requires string `label`; optional top-level fields are `icon`, `description`, `properties`, `events`, and `methods`.
Additional fields are rejected. Type labels are `string`, `number`, `integer`, `boolean`, `object`, `array`, `function`, `date`, and `any`
(there is no `void` label in this schema).

- Properties require `type`; optional fields are `description`, `default`, `readonly`, and unique string `enum`.
- Events accept `description` and a `detail` field map; each detail entry requires `type` with optional `description`/`required`.
- Methods accept `description`, `parameters`, and `returns`. Parameters require `name`/`type` and may set `description`, `required`, `enum`, `default`.
  Returns require `type` and may set `description`.

There are no generics, inheritance, unions, nested object/array schemas, parameter checks, version negotiation, scopes, or selectable lifetimes.

## Resolution validation and core services

Declared methods must resolve to functions. Property validation finds descriptors on the instance/prototype chain, excluding Object.prototype;
it does not evaluate property getters. Defaults, enums, readonly, and property types are descriptive, as are method parameters/returns and events.
An implementation must emit any promised events itself.

Construction receives a proxy resolving other services by name. Resolving a service already being constructed detects a circular constructor
dependency and reports the resolution chain. A failed construction/validation resets that record so a later resolve can retry.
Duplicate registrations, including collisions with core names, fail.

The registered core names are `areas`, `bus`, `config`, `container`, `dialog`, `i18n`, `loader`, `modules`, `navigation`, `resolver`, `runtime`,
`contracts`, `services`, `temp`, and `urlRewriter`. There is no auth/identity registration. A Diagnostics getter is not a registered service contract.

See [Bus](../subsystems/20-bus.md), [Dialogs](../subsystems/30-dialogs.md), and [Temp](../subsystems/50-temp.md).
