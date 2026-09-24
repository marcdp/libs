# Component Properties

This document establishes properties as the public programmatic API of an XShell component.

## Status

Draft.

## Conceptual contract

Properties are values that component consumers may read or write. They are distinct from internal state, even when a component explicitly
synchronizes a property with a state value.

`contract.properties[*].default` is the canonical default for every public property. A definition must not supply a competing public-property
default through `definition.state`.

## Attributes

Current contract metadata can mark a property with `attribute`. Runtime state definitions separately use `attr` to observe an HTML attribute and
`reflect` to propagate selected state changes back to an attribute.

## Property-to-state synchronization

A contract may mark a property with `state: true`, expressing an intended explicit relationship with internal state. This must not be interpreted
to mean that every property automatically maps to state.

For a state-backed property, `definition.state` may repeat the property name only as a readability aid. The repeated value must be structurally
equal to the contract default, including nested arrays and plain objects. The loader validates that equality and always initializes runtime state
from `contract.properties[*].default`; it ignores the duplicate definition value after validation. A property without `state: true` must not appear
in `definition.state`.

The runtime loader creates public accessors from the contract. State-backed properties use the corresponding runtime state entry; other public
properties retain their contract default independently of internal state.

## TODO

TODO: Define type conversion, nullability, attribute naming, reflection, and the authoritative source when a property and attribute change together.

## Related documentation

- [Components](index.md)
- [State](state.md)
- [Component Contract](manifest.md)
- [Properties and State ADR](../adr/0004-properties-and-state.md)
