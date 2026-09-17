# Component Events

This document introduces events emitted by components and event handling inside X templates.

## Status

Draft.

## Public events

Current component declaration metadata can list named events, descriptions, and an optional typed `detail` shape. Components emit standard `CustomEvent` instances directly.

For example, `x-datafields` declares `move`, `edit`, and `remove`; its implementation dispatches those events and includes a `direction` detail for `move`.

## Template handlers

The X template compiler recognizes `x-on:<event>` and the `@<event>` shorthand. The compiled handler forwards the command name and browser event to the component's command handler.

## Modifiers

Runtime event attachment parses dot-separated modifier names. TODO: Verify and test the complete supported modifier set before documenting it as a stable language contract.

## Related documentation

- [Components](index.md)
- [Template Bindings](../templates/bindings.md)
- [Component Manifest](manifest.md)

