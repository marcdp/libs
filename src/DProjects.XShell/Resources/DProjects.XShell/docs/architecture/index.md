# Architecture

XShell starts from a root module definition and recursively discovers module imports. The desired flow is:

```text
HTML/bootstrap inputs → root module → recursive imports → canonical module definitions
    → URL normalization and Service Worker resource mappings
    → dependency-first merge → nested effective configuration (app, modules, xshell)
    → validation in development → freeze → XShell
    → live module instances → navigation and services
```

`config.modules` contains declarative definitions. `xshell.modules` is the runtime service for live instances. Two imports may refer to one definition
while carrying separate params and runtime state. The current implementation has gaps in this flow; each document identifies the relevant ones.

Module resources use `/_assets/<module>/...` with the checked-in `_assets` prefix. The Service Worker maps that namespace to source files. Resource
resolution selects a URL and loader; the loader obtains the resource. A Page uses the normal Component model plus Navigation.

## Documents

- [Bootstrap](bootstrap.md) — Startup and preparation versus runtime initialization.
- [Configuration](configuration.md) — Nested effective configuration and merge rules.
- [Modules](modules.md) — Definitions, imports, live instances, and communication.
- [Components](components.md) — The Web Component model.
- [Pages](pages.md) — Pages and layouts.
- [Resolvers](resolvers.md) — Logical resource resolution.
- [Loaders](loaders.md) — Resource loading.
- [Service Worker](service-worker.md) — Resource virtualization.
- [Navigation](navigation.md) — Hash navigation and planned path navigation.

## Related documentation

- [Specifications](../specifications/)
- [Subsystems](../subsystems/)
- [Asset URL Namespace](../adr/0001-asset-url-namespace.md)
