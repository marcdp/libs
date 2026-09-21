# Components

Documentation for the core XShell Web Component model. Components use standard custom elements and component modules; they do not require X Templates.

## Status

Draft.

## Documents

- [Component Contract](manifest.md) — Current contract metadata and the future manifest concept.
- [Properties](properties.md) — Public component API values.
- [State](state.md) — Internal reactive data and rendering invalidation.
- [Events](events.md) — DOM events emitted by components.
- [Lifecycle](lifecycle.md) — Loading, mounting, rendering, unmounting, and cleanup.

## Current implementation boundary

Component modules currently export a default runtime definition. Representative checked-in components also export a named `contract` describing
their intended public surface. The runtime loader reads that export but does not substantially apply it while building the custom element; a complete
integration contract is still a TODO.

## Related documentation

- [XShell documentation](../)
- [X Templates extension](../extensions/x-templates/) — An optional integration layer for component rendering and event handlers.
- [Properties and State ADR](../adr/0004-properties-and-state.md)
