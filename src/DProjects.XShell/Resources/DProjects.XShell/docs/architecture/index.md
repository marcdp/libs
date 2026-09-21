# Architecture

XShell starts from the application's root module definition and recursively discovers module imports. The flow is:

```text
HTML/bootstrap inputs → root module → recursive imports → canonical module definitions
    → URL normalization → nested effective configuration (app, modules, xshell)
    → default resolvers → Service Worker mappings → import map → import XShell → deep freeze → init
    → one live module instance per module id → Areas compose menus and homes → Navigation starts
```

`config.modules` contains canonical definitions keyed by module id. `xshell.modules` is the runtime service. Repeated imports of one definition URL
retain the first registered import's params and produce one live module instance. Bootstrap currently merges in reverse registration order, which
does not guarantee dependency-first precedence for every graph. Optional module controllers are constructed once and started during module
initialization. See [Bootstrap](bootstrap.md).

Module resources use `/_assets/<module>/...` with the checked-in `_assets` prefix. The Service Worker maps that namespace to source files. Resource
resolution selects a URL and loader; the loader obtains the resource. A Page uses the normal Component model plus Navigation.
An Area is a navigation context composed from participating modules, including their effective menus. Modules define reusable menu contributions;
Areas define application composition. The first top-level navigation item marked `default: true` provides the Area home. This composition does not
duplicate module instances.

## Documents

- [Bootstrap](bootstrap.md) — Startup and preparation versus runtime initialization.
- [Configuration](configuration.md) — Nested effective configuration and merge rules.
- [Modules](modules.md) — Definitions, imports, params, and live instances.
- [Components](components.md) — The Web Component model.
- [Pages](pages.md) — Pages and layouts.
- [Resolvers](resolvers.md) — Logical resource resolution.
- [Loaders](loaders.md) — Resource loading.
- [Service Worker](service-worker.md) — Resource virtualization.
- [Navigation](navigation.md) — Hash navigation, planned path navigation, and Area context.

## Related documentation

- [Specifications](../specifications/)
- [Subsystems](../subsystems/)
- [Asset URL Namespace](../adr/0001-asset-url-namespace.md)
- [Areas](../subsystems/areas.md)
