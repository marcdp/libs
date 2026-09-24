# Component State

This document establishes state as internal reactive data for a component or Page. A configured state engine owns that reactive state; the loader owns
the component or Page lifecycle around it.

## Status

Draft.

## Conceptual contract

State supports component behavior and rendering and should generally not be considered public API. Properties and state are separate concepts; any
synchronization between them should be explicitly declared by the component model.

`definition.state` is the source of defaults for private/internal state entries only. Public property defaults always come from
`contract.properties[*].default`. A state-backed public property may be repeated in `definition.state` for readability only when its value is
structurally equal to the contract default; the contract value remains the runtime default. A non-state-backed public property must not be declared
in `definition.state`.

## Current runtime

The component loader builds a state skeleton from private `definition.state` entries and state-backed public-property defaults from the contract,
then delegates reactivity to a configured state engine. The loader connects state invalidation to rendering through `requestAnimationFrame`.

Current state entries may specify values and runtime flags such as `type`, `attr`, `prop`, and `reflect`. The exact stable schema remains to be
defined.

## State-engine boundary

A state engine is responsible only for reactive state:

- create and manage reactive state;
- detect state changes;
- notify state changes;
- request invalidation.

A state engine does not own `load`, `mount`, `unmount`, or `unload`; controller methods; public component or Page methods; contracts; DOM or
rendering; navigation; or general component or Page lifecycle. The loader owns those concerns and decides how a state-engine invalidation reaches the
render engine.

## TODO

TODO: Specify state-engine guarantees, nested mutation semantics, initialization order, and an explicit property-to-state mapping contract.

## Related documentation

- [Components](index.md)
- [Properties](properties.md)
- [Lifecycle](lifecycle.md)
- [Rendering Engines](rendering.md)
- [Properties and State ADR](../adr/0004-properties-and-state.md)
