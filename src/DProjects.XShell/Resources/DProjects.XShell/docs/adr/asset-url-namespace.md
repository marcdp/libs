# Asset URL Namespace

This record describes the architectural role of a stable URL namespace for assets managed by the XShell service worker.

## Status

Draft; the concept and current prefix are implemented, but the namespace is not yet documented as a permanent public contract.

## Context

Framework and module resources can originate from different directories or URLs. Stable application-visible paths let imports, pages, styles, components, and icons refer to a consistent namespace while the service worker maps requests to their actual source directories.

## Current implementation

The namespace is controlled by `xshell.assetsPrefix`. The checked-in XShell configuration uses `_assets`, producing application-relative prefixes such as `/_assets/xshell` and `/_assets/<module>`. Bootstrap sends corresponding source-to-destination rules to the service worker.

The suggested name `/_cdn` is not used by the inspected configuration and should not replace `_assets` without a separate compatibility decision.

## Consequences

The prefix affects resolver definitions, import maps, module paths, service-worker scope behavior, and bookmarked or cached resource URLs. Renaming it may be breaking.

## TODO

TODO: Define prefix validation, collision handling, deployment beneath non-root base paths, cache-version behavior, and compatibility guarantees.

## Related documentation

- [Service Worker](../architecture/service-worker.md)
- [Modules](../architecture/modules.md)
- [Resolvers](../architecture/resolvers.md)

