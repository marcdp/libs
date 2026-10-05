# Architecture

XShell starts from the application's root module definition and recursively discovers module references. The flow is:

```text
HTML/bootstrap inputs → root module → recursive module references → canonical module definitions
    → URL normalization → nested effective configuration (app, modules, xshell)
    → Service Worker mappings → file inventories → resolver rules (including service contracts)
    → import XShell → validate effective config → deep freeze → init (validate again)
    → initialize i18n and Contracts → register core services → finalize service registry → validate module service requirements → one live module instance per module id
    → controllers register runtime sources → Areas compose menus, routes, and homes → Navigation starts
```

`config.modules` contains canonical definitions keyed by module id. `xshell.modules` is the runtime service. Repeated references to one definition
URL are fetched once and produce one live module instance. Bootstrap merges dependencies before dependents and the root last; cycles are rejected.
References can configure canonical dependency modules; the root still merges last. Optional module controllers are constructed once and started
during module initialization. See [Bootstrap](10-bootstrap.md).

Module resources use `/_assets/<module>/...` with the checked-in `_assets` prefix. The Service Worker maps that namespace to source files. Resource
resolution selects a URL and loader; the loader obtains the resource. A Page uses the normal Component model plus Navigation.
Module defaults define the render and state engines for their own definition-based Pages and Components. XShell defaults are separate global UI
infrastructure for layout contexts, lazy/error/Markdown components, and standard dialog pages.

For a definition-based Component or Page, its loader is the orchestrator: it owns the contract, properties, public API, controller, lifecycle,
services, and coordination. The state engine owns reactive state only; the render engine owns rendered output only. See [Loaders](90-loaders.md) and
[Component Lifecycle](../20-components/60-lifecycle.md).

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
and route contributions; Areas define application composition. Home uses the depth-first default-item result, falling back to the first depth-first
`path || href` target.
There is no separate visibility predicate in that fallback. This composition does not duplicate module instances. Navigation consumes Area routes for
public-to-canonical and
canonical-to-public resolution. A named menu contribution can be a static array or a registered dynamic menu source; see
[Areas](../30-subsystems/10-areas.md) for composition behavior and [Navigation](120-navigation.md) for route resolution.

## Documents

- [Bootstrap](10-bootstrap.md) — Startup and preparation versus runtime initialization.
- [Configuration](20-configuration.md) — Nested effective configuration and merge rules.
- [Modules](30-modules.md) — Definitions, references, root composition, params, and live instances.
- [Hosting](40-hosting.md) — ASP.NET middleware, generated bootstrap, and SPA fallback.
- [Packaging](50-packaging.md) — Development inventories and immutable module ZIP creation.
- [Components](60-components.md) — The Web Component model.
- [Pages](70-pages.md) — Pages and layouts.
- [Resolvers](80-resolvers.md) — Logical resource resolution.
- [Loaders](90-loaders.md) — Resource loading.
- [Services](100-services.md) — Contracts, configured implementations, immutable registry topology, and lazy singleton resolution.
- [Service Worker](110-service-worker.md) — Resource virtualization.
- [Navigation](120-navigation.md) — Hash and path navigation plus Area context.

## Related documentation

- [Specifications](../50-specifications/)
- [Subsystems](../30-subsystems/)
- [Asset URL Namespace](../60-adr/0001-asset-url-namespace.md)
- [Areas](../30-subsystems/10-areas.md)
