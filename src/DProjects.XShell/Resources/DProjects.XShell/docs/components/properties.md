# Component Properties

This document establishes properties as the public programmatic API of an XShell component.

## Status

Draft.

## Conceptual contract

Properties are values that component consumers may read or write. They are distinct from internal state, even when a component explicitly
synchronizes a property with a state value.

## Attributes

Current contract metadata can mark a property with `attribute`. Runtime state definitions separately use `attr` to observe an HTML attribute and
`reflect` to propagate selected state changes back to an attribute.

## Property-to-state synchronization

A contract may mark a property with `state: true`, expressing an intended explicit relationship with internal state. This must not be interpreted
to mean that every property automatically maps to state.

The current runtime loader generates JavaScript properties from implementation `state` entries marked `prop`; `prop` defaults to true when `attr` is
true. This behavior does not yet enforce the intended public/internal separation.

## TODO

TODO: Define type conversion, nullability, attribute naming, reflection, and the authoritative source when a property and attribute change together.

## Related documentation

- [Components](index.md)
- [State](state.md)
- [Component Contract](manifest.md)
- [Properties and State ADR](../adr/0004-properties-and-state.md)
