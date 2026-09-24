# XTemplate Expressions

For the normative XTemplate expression contract, see the [XTemplate Language Specification](specification.md). This guide summarizes the restricted,
portable expression model used by XShell.

## Current model

XTemplate expressions are a small language owned by XTemplate. The syntax is JavaScript-like, but expressions are parsed into a portable AST and
evaluated against an explicit render context. They are not arbitrary JavaScript and cannot access host globals or object methods.

Typical expression-visible identifiers include `state`, explicitly supplied application/context values, and template-defined locals. `x-for` and
`x-recursive` introduce locals such as `item`, `index`, `indexAbsolute`, and `indent`. Values such as `i18n` and `renderCount` are expression-visible
only when the rendering environment explicitly inserts them into `ExpressionContext`.

Runtime/compiler infrastructure such as `handler`, `invalidate`, `utils`, and VDOM internals is not automatically available as expression
identifiers. An implementation may expose additional identifiers only by explicitly placing them in `ExpressionContext` and documenting that
context.

```html
<h1 x-text="state.title"></h1>
<span>{{ state.message }}</span>
<li x-for="(item,index) in state.items" x-class:selected="item.id == state.selectedId">
    {{ index + 1 }}. {{ item.label }}
</li>
```

Presentation formatters are the restricted pipeline extension:

```html
<p>{{ state.price | number(2) }}</p>
<p>{{ state.createdAt | date('dd/MM/yyyy') }}</p>
<p>{{ state.name | trim | upper }}</p>
<div x-attr:data-price="state.price | number(2)"></div>
```

Formatter arguments are full XTemplate expressions, but formatter names refer only to the specified built-in language operations. General calls and
method access remain invalid: `formatPrice(state.price)`, `state.price.toFixed(2)`, and `state.name.toUpperCase()` are not XTemplate expressions.

## Where expressions are used

Expressions provide values for interpolation, conditional directives, bindings, loop sources, class bindings, visibility, and model binding. A
formatter pipeline can be used in ordinary value-expression positions, including bindings such as `x-attr:data-price`; `x-model` still requires an
assignable expression and therefore cannot use a formatter as its write target.

```html
<div x-if="state.visible" x-attr:title="state.title"></div>
<input x-model="state.query">
```

Event binding is different: `x-on:event="command"` identifies a named command rather than arbitrary inline JavaScript.

## Security and portability

The restricted grammar does not permit arbitrary function calls, host globals, object methods, assignments, or statements. Formatters are pure,
side-effect-free language operations: they cannot execute user code, mutate state, perform I/O, or access DOM/browser APIs. JavaScript and C#
renderers must implement the same formatter semantics, including locale behavior, type checks, null propagation, and result kinds.

Raw scalar conversion remains invariant. Locale-sensitive output is explicit, for example `state.price | number(2)`; ordinary `{{ state.price }}`
continues to use invariant XTemplate conversion.

## Related documentation

- [X Templates](index.md)
- [XTemplate Language Specification](specification.md)
- [Syntax guide](syntax.md)
- [Compiler and runtime architecture](compiler.md)
