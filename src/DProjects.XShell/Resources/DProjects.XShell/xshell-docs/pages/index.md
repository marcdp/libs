# XShell V0

XShell is a browser runtime for applications composed from modules. ASP.NET serves bootstrap and resources; the browser composes configuration,
virtualizes assets, initializes services/modules, and activates Pages through Areas and Navigation.

## Core concepts

- [Modules](10-architecture/30-modules.md) and [Configuration](10-architecture/20-configuration.md) — Composition, defaults, and one instance per id.
- [Bootstrap](10-architecture/10-bootstrap.md) — Startup order and validation.
- [Browser Platform Baseline](10-architecture/15-browser-platform.md) — Baseline 2026, secure-context, and V0 compatibility contract.
- [Components](20-components/index.md) and [Pages](10-architecture/70-pages.md) — Contracts, controllers, state, lifecycle, and presentation.
- [Resolvers](10-architecture/80-resolvers.md) and [Loaders](10-architecture/90-loaders.md) — Resource meaning versus obtaining concrete resources.
- [Services and service Contracts](10-architecture/100-services.md) — Fixed registry, eager class loading, lazy singletons.
- [Areas](30-subsystems/10-areas.md) and [Navigation](10-architecture/120-navigation.md) — Menus, routes, homes, browser URLs, and Page stacks.
- [State](20-components/30-state.md) and [Rendering Engines](20-components/70-rendering.md) — Separate engines coordinated by loaders.
- [Service Worker](10-architecture/110-service-worker.md) — `/_assets` and expanded resource mapping.
- [X Templates](40-extensions/x-templates/index.md) — Optional compiled rendering.
- [Hosting](10-architecture/40-hosting.md) and [Packaging](10-architecture/50-packaging.md) — ASP.NET, compilation, and inventories.
- [Subsystems](30-subsystems/index.md) — Bus, dialogs, i18n, Temp.

## Reference

[Architecture](10-architecture/index.md), [Specifications](50-specifications/index.md), [Extensions](40-extensions/index.md), and [ADRs](60-adr/index.md)
provide detailed reference and decision context.

Authentication/Identity, navigation intents, and runtime ZIP loading are outside V0.
Packaging ZIPs does not make them browser-loadable; descriptive contracts do not enforce every type or emit events.
