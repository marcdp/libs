# Components

XShell V0 loads native Web Component classes or definition objects. For definitions, `component-js` validates the named `contract` export and
the default implementation, selects engines, loads dependencies, and constructs and registers an `HTMLElement` subclass.

The contract supplies public-property defaults and accessors, state-backed properties, observed attributes, reflection, public method exposure,
and validation of engine-reported slots. Descriptive metadata does not enforce values or emit events.
Pages reuse the contract and state model with Page-specific query/context initialization and lifecycle; their public API has some
[implementation differences](../architecture/pages.md).

## Documents

- [Component Contract](manifest.md) — Supported schema and enforcement boundaries.
- [Properties](properties.md) — Public values, attributes, reflection, and Page query binding.
- [State](state.md) — Private defaults and the `none`, `plain`, and `proxy` engines.
- [Events](events.md) — DOM CustomEvents, Bus events, and X Template event modifiers.
- [Slots](slots.md) — Public composition metadata and engine-reported slot validation.
- [Lifecycle](lifecycle.md) — Instance and connection lifetimes.
- [Rendering Engines](rendering.md) — Output ownership and engine selection.

See [Component Architecture](../architecture/components.md), [Pages](../architecture/pages.md), and [X Templates](../extensions/x-templates/index.md).
