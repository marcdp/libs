# ADR-0001: Asset URL Namespace

This record describes the architectural role of a stable URL namespace for assets managed by the XShell service worker.

## Status

Implemented; the checked-in namespace is a compatibility boundary.

## Context

Framework and module resources can originate from different directories or URLs. Stable application-visible paths let imports, pages, styles, components, and icons refer to a consistent namespace while the service worker maps requests to their actual source directories.

## Current implementation

The namespace is controlled by `xshell.assetsBasePath`. The checked-in XShell configuration uses `app:/_assets`. Bootstrap resolves it against the
application base URL and derives paths such as `/_assets/xshell/0.9.0.dev` and `/_assets/<module-id>/<generation>`. It sends corresponding rules to
the service worker.

The `/_assets` namespace must remain stable unless a separate compatibility decision changes it.

## Implemented V0 generation identity contract

Development generations are mutable and named `<version>.dev`; they do not require a package content hash. Published generations are immutable and
named `<version>.<hash>`, where `hash` is the generated package-level identity. The runtime asset URL form is
`/_assets/<module-id>/<generation>/...`, including `xshell` as a module id for framework assets.

Bootstrap selects `.dev` only when the effective environment is Development, ignoring any hash. Other environments require the descriptor's
non-empty package hash. Service Worker rule sources use the resulting generation-qualified paths.

The Service Worker owns one persistent application-scope registry. Registration is additive and serialized. Canonical `src` identifies an
immutable mapping: the same `src` and `dst` is idempotent, while the same `src` with another `dst` rejects the complete registration batch.
Different generation paths coexist across tabs and worker restarts. V0 has no per-tab mapping state and does not expire or garbage-collect old
generations.

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
