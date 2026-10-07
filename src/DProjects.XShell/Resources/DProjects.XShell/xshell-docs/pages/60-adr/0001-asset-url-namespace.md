# ADR-0001: Asset URL Namespace

This record describes the architectural role of a stable URL namespace for assets managed by the XShell service worker.

## Status

Implemented; the checked-in namespace is a compatibility boundary.

## Context

Framework and module resources can originate from different directories or URLs. Stable application-visible paths let imports, pages, styles, components, and icons refer to a consistent namespace while the service worker maps requests to their actual source directories.

## Current implementation

The namespace is controlled by `xshell.assetsBase`. The checked-in XShell configuration uses `app:/_assets`. Bootstrap resolves it against the application base URL once and derives application-relative paths such as `/_assets/xshell` and `/_assets/<module>`. It sends corresponding source-to-destination rules to the service worker.

The `/_assets` namespace must remain stable unless a separate compatibility decision changes it.

## Consequences

The base affects resolver definitions, module paths, service-worker scope behavior, and bookmarked or cached resource URLs. Changing it may be
breaking.

## Implementation boundaries

App base paths and generated worker mappings are implemented. Bootstrap does not create an import map.
Prefix validation/collision policy and resource cache versioning are not established V0 guarantees.
The worker serves expanded directories; immutable ZIP packaging does not provide runtime ZIP loading.

## Related documentation

- [Service Worker](../architecture/110-service-worker.md)
- [Modules](../architecture/30-modules.md)
- [Resolvers](../architecture/80-resolvers.md)
