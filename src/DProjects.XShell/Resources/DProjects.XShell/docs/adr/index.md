# Architecture Decision Records

This section records significant XShell design decisions, their context, and implementation status.

## Status

Draft.

## Records

- [ADR-0001: Asset URL Namespace](0001-asset-url-namespace.md) — Providing stable application URLs for service-worker-managed assets.
- [ADR-0002: JSONC Specifications](0002-jsonc-specifications.md) — JSONC authoring, nested composition, and effective-config validation.
- [ADR-0003: Navigation](0003-navigation.md) — Implemented hash/path modes and planned public-intent dispatch.
- [ADR-0004: Properties and State](0004-properties-and-state.md) — Separating public component properties from internal reactive state.
- [ADR-0005: Area and Menu Composition](0005-area-menu-composition.md) — Application-owned Areas and reusable module menu slots.

## Conventions

New ADRs receive the next sequential number. ADR numbers are permanent: they are never reused, and accepted or superseded ADRs are not renumbered.
Filenames use `NNNN-short-description.md`.

Each record states its decision status explicitly; a draft record must not be treated as a final architectural decision.

## Related documentation

- [XShell documentation](../)
- [Architecture](../architecture/)
