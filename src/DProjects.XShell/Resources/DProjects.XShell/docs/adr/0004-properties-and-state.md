# ADR-0004: Properties and State

This record captures the intended distinction between a component's public properties and its internal reactive state.

## Status

Draft; direction proposed, runtime alignment incomplete.

## Context

Consumers need a stable public component API, while implementations need internal reactive data that can evolve without becoming a compatibility
commitment. Treating all state entries as public properties collapses those two concerns.

## Current direction

Properties are public API. State is internal implementation data and should generally remain private to the component. A property may synchronize
with state only when that relationship is explicitly declared; synchronization is not automatic merely because names match.

## Current implementation

Component contracts now distinguish `properties`, and property metadata can include `state: true`. However, the active component loader still
derives JavaScript properties from runtime `state` entries marked `prop`, with attribute-backed entries receiving that flag by default. The loader
reads the named `contract` export but does not yet use it to enforce this separation.

## Consequences

A future alignment must preserve component compatibility while introducing an explicit mapping and preventing accidental exposure of internal
state. Migration and validation rules are still required.

## TODO

TODO: Finalize the metadata name, mapping semantics, attribute interaction, migration plan, and enforcement tests before accepting this decision.

## Related documentation

- [Properties](../components/properties.md)
- [State](../components/state.md)
- [Component Contract](../components/manifest.md)
