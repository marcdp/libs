# Component Events

This document introduces events emitted by core XShell components.

## Status

Draft.

## Public events

Current component contract metadata can list named events, descriptions, and an optional typed `detail` shape. Components emit standard
`CustomEvent` instances directly.

For example, `x-datafields` declares `move`, `edit`, and `remove`; its implementation dispatches those events and includes a `direction` detail for
`move`.

## Optional X Templates integration

The optional X Templates extension recognizes `x-on:<event>` and the `@<event>` shorthand. Its compiled handler forwards the controller method name
and browser event to the component runtime's internal controller dispatcher. The matching controller method executes with the controller as
`this`; it is not looked up through a public Web Component method. Components do not depend on that extension to emit or handle standard DOM
events.

## Modifiers

Runtime event attachment parses dot-separated modifier names. TODO: Verify and test the complete supported modifier set before documenting it as a
stable language contract.

## Related documentation

- [Components](index.md)
- [X Template Bindings](../extensions/x-templates/bindings.md)
- [Component Contract](manifest.md)
