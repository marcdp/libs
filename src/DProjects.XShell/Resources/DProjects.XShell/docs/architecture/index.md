# Architecture

This section introduces the major XShell runtime responsibilities and the flow from logical resources to loaded browser assets.

## Status

Draft.

## Overview

At a high level, bootstrap loads the XShell, application, and module JSONC configuration; module configuration contributes resolver definitions; the resolver maps logical resource names to URLs and loader metadata; and the loader dispatches each URL to a resource-specific loader. A service worker rewrites the stable asset namespace to the configured source locations.

## Documents

- [Modules](modules.md) — Module configuration, initialization, styles, and handlers.
- [Resolvers](resolvers.md) — Logical resource patterns and URL resolution.
- [Loaders](loaders.md) — Resource loading, caching, and type-specific handlers.
- [Service Worker](service-worker.md) — Asset URL rewriting performed by the service worker.
- [Navigation](navigation.md) — Current hash navigation and the incomplete path-mode branch.

## Open questions

TODO: Add a verified end-to-end sequence diagram after the bootstrap and loader contracts are covered by tests.

## Related documentation

- [XShell documentation](../)
- [Specifications](../specifications/)
- [Asset URL Namespace](../adr/0001-asset-url-namespace.md)
