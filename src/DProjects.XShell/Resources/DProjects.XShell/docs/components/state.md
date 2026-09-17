# Component State

This document establishes state as internal reactive data owned by a component.

## Status

Draft.

## Conceptual contract

State supports component behavior and rendering and should generally not be considered public API. Properties and state are separate concepts; any synchronization between them should be explicitly declared by the component model.

## Current runtime

The component loader builds a state skeleton from the default definition's `state` entries and delegates reactivity to a configured state engine. State changes are collected and schedule rendering through `requestAnimationFrame`.

Current state entries may specify values and runtime flags such as `type`, `attr`, `prop`, and `reflect`. The exact stable schema remains to be defined.

## Implementation gap

The runtime currently generates public JavaScript accessors from implementation state entries marked `prop`, which defaults to true for attribute-backed entries. This couples state to the public surface more closely than the intended design direction.

## TODO

TODO: Specify state-engine guarantees, nested mutation semantics, initialization order, and an explicit property-to-state mapping contract.

## Related documentation

- [Components](index.md)
- [Properties](properties.md)
- [Lifecycle](lifecycle.md)
- [Properties and State ADR](../adr/0004-properties-and-state.md)
