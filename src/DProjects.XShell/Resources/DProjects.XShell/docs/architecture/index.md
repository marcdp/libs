# Architecture

XShell starts from the application's root module definition and recursively discovers module references. The flow is:

```text
HTML/bootstrap inputs → root module → recursive module references → canonical module definitions
    → URL normalization → nested effective configuration (app, modules, xshell)
    → default resolvers → Service Worker mappings → import map → import XShell → deep freeze → init
    → finalize service registry → validate module service requirements → one live module instance per module id
    → controllers register runtime sources → Areas compose menus, routes, and homes → Navigation starts
```

`config.modules` contains canonical definitions keyed by module id. `xshell.modules` is the runtime service. Repeated references to one definition
URL are fetched once and produce one live module instance. Bootstrap merges dependencies before dependents and the root last; cycles are rejected.
References can configure canonical dependency modules; the root still merges last. Optional module controllers are constructed once and started
during module initialization. See [Bootstrap](bootstrap.md).

Module resources use `/_assets/<module>/...` with the checked-in `_assets` prefix. The Service Worker maps that namespace to source files. Resource
resolution selects a URL and loader; the loader obtains the resource. A Page uses the normal Component model plus Navigation.
Module defaults define the render and state engines for their own definition-based Pages and Components. XShell defaults are separate global UI
infrastructure for layout contexts, lazy/error components, and standard dialog pages.

For a definition-based Component or Page, its loader is the orchestrator: it owns the contract, properties, public API, controller, lifecycle,
services, and coordination. The state engine owns reactive state only; the render engine owns rendered output only. See [Loaders](loaders.md) and
[Component Lifecycle](../components/lifecycle.md).

When a definition declares `dependencies`, its loading flow is:

```text
component or Page definition
    ↓
read dependencies
    ↓
Resolver maps each reference
    ↓
Loader obtains each resource
    ↓
create controller with controller({ dependencies })
```

An Area is a navigation context composed from participating modules, including their effective menus and ordered routes. Modules define reusable menu
and route contributions; Areas define application composition. The first navigation item marked `default: true` in depth-first traversal provides
the Area home. This composition does not duplicate module instances. Navigation consumes Area routes for public-to-canonical and
canonical-to-public resolution. A named menu contribution can be a static array or a registered dynamic menu source; see
[Areas](../subsystems/areas.md) for composition behavior and [Navigation](navigation.md) for route resolution.

## Documents

- [Bootstrap](bootstrap.md) — Startup and preparation versus runtime initialization.
- [Configuration](configuration.md) — Nested effective configuration and merge rules.
- [Modules](modules.md) — Definitions, references, root composition, params, and live instances.
- [Packaging](packaging.md) — Development manifests and immutable module ZIP creation.
- [Components](components.md) — The Web Component model.
- [Pages](pages.md) — Pages and layouts.
- [Resolvers](resolvers.md) — Logical resource resolution.
- [Loaders](loaders.md) — Resource loading.
- [Services](services.md) — Contracts, configured implementations, immutable registry topology, and lazy singleton resolution.
- [Service Worker](service-worker.md) — Resource virtualization.
- [Navigation](navigation.md) — Hash and path navigation plus Area context.

## Related documentation

- [Specifications](../specifications/)
- [Subsystems](../subsystems/)
- [Asset URL Namespace](../adr/0001-asset-url-namespace.md)
- [Areas](../subsystems/areas.md)
