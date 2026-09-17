# X Template Syntax

This document lists template constructs confirmed in the current X template compiler.

## Status

Draft.

## Text

Double-brace text such as `{{state.label}}` is transformed into an `x:text` node before compilation. The `x-text` directive sets text content, while `x-html` sets HTML content.

## Conditional rendering

The compiler recognizes `x-if`, `x-elseif`, and `x-else`. It also recognizes `x-show`, which controls an inline display style rather than removing the node.

## Repetition

The compiler recognizes `x-for="item in state.items"`, optional tuple variables, and an optional `x-key`. It also contains support for `x-recursive` and `x-recursive-wrapper`.

## Render controls

Confirmed controls include `x-once`, `x-pre`, and `x-children`. TODO: Specify their edge cases after focused tests exist.

## Unsupported syntax

Unknown `x-` attributes and unknown `x:` elements currently produce compiler errors. Similar syntax from other template languages must not be assumed to work.

## Related documentation

- [X Templates](index.md)
- [Expressions](expressions.md)
- [Bindings](bindings.md)

