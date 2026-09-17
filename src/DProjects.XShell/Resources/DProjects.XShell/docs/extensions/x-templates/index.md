# X Templates

This section documents X Templates, an optional XShell extension that provides the declarative template language used by the `x` render engine. It integrates with XShell Components but is not part of the core component model.

## Status

Draft.

## Documents

- [Syntax](syntax.md) — Confirmed template directives and structural forms.
- [Expressions](expressions.md) — JavaScript expressions evaluated by compiled templates.
- [Bindings](bindings.md) — Attribute, property, event, class, and model bindings.
- [Compiler](compiler.md) — Compilation to a virtual DOM render function and DOM reconciliation.

## Evidence boundary

The initial outline is based on the current `x-template.js` compiler and templates under the X module. Syntax not handled by that compiler is intentionally omitted or marked TODO.

## Related documentation

- [Extensions](../)
- [XShell Components](../../components/)
