# Components

Documentation for the core XShell Web Component model. Components use standard custom elements and component modules; they do not require X Templates.

## Status

Draft.

## Documents

- [Component Manifest](manifest.md) — Declarative metadata and the current declaration/implementation split.
- [Properties](properties.md) — Public component API values.
- [State](state.md) — Internal reactive data and rendering invalidation.
- [Events](events.md) — DOM events emitted by components.
- [Lifecycle](lifecycle.md) — Loading, mounting, rendering, unmounting, and cleanup.

## Current implementation boundary

Component modules currently export a default runtime definition. Many checked-in components also export a named `declaration` describing their intended public contract. The runtime loader consumes the default definition; a complete integration contract for declaration metadata is still a TODO.

## Related documentation

- [XShell documentation](../../)
- [X Templates extension](../../extensions/x-templates/) — An optional integration layer for component rendering and event handlers.
- [Properties and State ADR](../../adr/0004-properties-and-state.md)
