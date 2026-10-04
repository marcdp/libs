# XShell V0

XShell is a browser runtime for applications composed from modules. ASP.NET serves bootstrap and resources; the browser composes configuration,
virtualizes assets, initializes services/modules, and activates Pages through Areas and Navigation.

## Core concepts

- [Modules](architecture/modules.md) and [Configuration](architecture/configuration.md) — Composition, defaults, and one instance per id.
- [Bootstrap](architecture/bootstrap.md) — Startup order and validation.
- [Components](components/index.md) and [Pages](architecture/pages.md) — Contracts, controllers, state, lifecycle, and presentation.
- [Resolvers](architecture/resolvers.md) and [Loaders](architecture/loaders.md) — Resource meaning versus obtaining concrete resources.
- [Services and service Contracts](architecture/services.md) — Fixed registry, eager class loading, lazy singletons.
- [Areas](subsystems/areas.md) and [Navigation](architecture/navigation.md) — Menus, routes, homes, browser URLs, and Page stacks.
- [State](components/state.md) and [Rendering Engines](components/rendering.md) — Separate engines coordinated by loaders.
- [Service Worker](architecture/service-worker.md) — `/_assets` and expanded resource mapping.
- [X Templates](extensions/x-templates/index.md) — Optional compiled rendering.
- [Hosting](architecture/hosting.md) and [Packaging](architecture/packaging.md) — ASP.NET, compilation, and inventories.
- [Subsystems](subsystems/index.md) — Bus, dialogs, i18n, Temp.

## Reference

[Architecture](architecture/index.md), [Specifications](specifications/index.md), [Extensions](extensions/index.md), and [ADRs](adr/index.md)
provide detailed reference and decision context.

Authentication/Identity, navigation intents, and runtime ZIP loading are outside V0.
Packaging ZIPs does not make them browser-loadable; descriptive contracts do not enforce every type or emit events.
