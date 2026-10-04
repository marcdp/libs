# Components

XShell V0 loads native Web Component classes or definition objects. For definitions, `component-js` validates the named `contract` export and
the default implementation, selects engines, loads dependencies, and constructs and registers an `HTMLElement` subclass.

The contract supplies public-property defaults and accessors, state-backed properties, observed attributes, reflection, public method exposure,
and validation of engine-reported slots. Descriptive metadata does not enforce values or emit events.
Pages reuse the contract and state model with Page-specific query/context initialization and lifecycle; their public API has some
[implementation differences](../10-architecture/70-pages.md).

## Documents

- [Component Contract](10-manifest.md) — Supported schema and enforcement boundaries.
- [Properties](20-properties.md) — Public values, attributes, reflection, and Page query binding.
- [State](30-state.md) — Private defaults and the `none`, `plain`, and `proxy` engines.
- [Events](40-events.md) — DOM CustomEvents, Bus events, and X Template event modifiers.
- [Slots](50-slots.md) — Public composition metadata and engine-reported slot validation.
- [Lifecycle](60-lifecycle.md) — Instance and connection lifetimes.
- [Rendering Engines](70-rendering.md) — Output ownership and engine selection.

See [Component Architecture](../10-architecture/60-components.md), [Pages](../10-architecture/70-pages.md), and [X Templates](../40-extensions/x-templates/index.md).
