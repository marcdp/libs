# Components

Documentation for the XShell Web Component model.

## Status

Draft.

## Documents

- [Component Manifest](manifest.md) — Declarative metadata and the current declaration/implementation split.
- [Properties](properties.md) — Public component API values.
- [State](state.md) — Internal reactive data and rendering invalidation.
- [Events](events.md) — DOM events and template event handlers.
- [Lifecycle](lifecycle.md) — Loading, mounting, rendering, unmounting, and cleanup.

## Current implementation boundary

Component modules currently export a default runtime definition. Many checked-in components also export a named `declaration` describing their intended public contract. The runtime loader consumes the default definition; a complete integration contract for declaration metadata is still a TODO.

## Related documentation

- [XShell documentation](../)
- [Templates](../templates/)
- [Properties and State ADR](../adr/properties-and-state.md)

