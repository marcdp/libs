# ADR-0004: Properties and State

This record captures the intended distinction between a component's public properties and its internal reactive state.

## Status

Accepted.

## Context

Consumers need a stable public component API, while implementations need internal reactive data that can evolve without becoming a compatibility
commitment. Treating all state entries as public properties collapses those two concerns.

## Current direction

Properties are public API. State is internal implementation data and should generally remain private to the component. A property may synchronize
with state only when that relationship is explicitly declared; synchronization is not automatic merely because names match.

## Current implementation

Component contracts distinguish `properties`, and property metadata can include `state: true`. `contract.properties[*].default` is the canonical
default for every public property. `definition.state` supplies defaults for private/internal state. A state-backed property may repeat its name in
`definition.state` only for readability and only with a structurally equal value; loaders validate the duplicate and retain the contract default.
A non-state-backed public property may not appear in `definition.state`.

## Consequences

This preserves a single public-property default source, permits readable state-backed definitions without ambiguity, and prevents accidental
exposure of internal state.

## Related documentation

- [Properties](../components/properties.md)
- [State](../components/state.md)
- [Component Contract](../components/manifest.md)
