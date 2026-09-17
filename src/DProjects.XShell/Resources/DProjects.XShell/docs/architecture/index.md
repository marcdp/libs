# Architecture

This section introduces the XShell startup path and the runtime flow from configuration and logical resource references to concrete browser results.

## Status

Draft.

## Overview

The ASP.NET host generates an HTML entry point containing the application base, configuration URL, service-worker URL, and bootstrap script. Bootstrap
combines framework, application, and module configuration, initializes stable asset rewrites and the import map, then creates runtime services, initializes
modules, and starts navigation. Resource loading continues through a separate resolver and loader pipeline.

```text
host HTML -> bootstrap -> combined configuration -> service worker/import map -> runtime services -> modules -> navigation
                                                                                                  |
logical resource -> resolver -> resolved URL and metadata -> loader -> type-specific loader -> optional engine -> runtime result
```

## Documents

- [Bootstrap](bootstrap.md) — Host integration, configuration loading, service-worker/import-map setup, and runtime initialization.
- [Configuration](configuration.md) — Runtime configuration sources, precedence, URL normalization, and global module contributions.
- [Modules](modules.md) — Module configuration, initialization, styles, and handlers.
- [Components](components.md) — Components.
- [Pages](pages.md) — Pages.
- [Resolvers](resolvers.md) — Logical resource patterns and URL resolution.
- [Loaders](loaders.md) — Resource loading, caching, and type-specific handlers.
- [Service Worker](service-worker.md) — Asset URL rewriting performed by the service worker.
- [Navigation](navigation.md) — Current hash navigation and the incomplete path-mode branch.

## Related documentation

- [XShell documentation](../)
- [Specifications](../specifications/)
- [Asset URL Namespace](../adr/0001-asset-url-namespace.md)
