# X Templates

X Templates are an optional XShell extension that provides the declarative template language used by the `x` render engine. They integrate with
XShell Components, but are not part of the core component model.

## Documentation hierarchy

[XTemplate Language Specification](specification.md) is the normative source of truth for XTemplate syntax and semantics. The other documents in
this section are explanatory, tutorial-oriented, or implementation-oriented guides derived from that specification. If another document differs
from the specification, the specification takes precedence.

## Documents

- [XTemplate Language Specification](specification.md) — Normative XTemplate language contract, including the current XShell VDOM ABI where
  compatibility requires it.
- [Syntax guide](syntax.md) — Concise, human-oriented quick reference for common template constructs.
- [Expressions](expressions.md) — Explanation of the current JavaScript expression model and render scope.
- [Bindings](bindings.md) — Practical guide to attributes, properties, events, classes, visibility, and model binding.
- [Compiler and runtime architecture](compiler.md) — XShell compiler/runtime architecture and implementation guidance.

Readers learning XTemplate can begin with the [syntax guide](syntax.md). Implementers of an XTemplate compiler or interpreter should begin with the
[language specification](specification.md). Readers investigating the current XShell implementation should use the
[compiler and runtime architecture](compiler.md) alongside the specification.

## Related documentation

- [Extensions](../)
- [XShell Components](../../components/)
