# XTemplate Expressions

For the normative XTemplate expression contract, see the [XTemplate Language Specification](specification.md). This guide explains the current
expression model used by XShell.

## Current model

XTemplate expressions use JavaScript expression syntax. They execute in the XTemplate render context, rather than in a separate portable
expression language.

Typical names available to an expression include `state`, `i18n`, and `renderCount`. Render infrastructure can also provide names such as
`utils`, `handler`, and `invalidate`. `x-for` and `x-recursive` introduce template-defined local variables, such as `item`, `index`,
`indexAbsolute`, and `indent`.

```html
<h1 x-text="state.title"></h1>
<span>{{ i18n.t(state.messageKey) }}</span>
<li x-for="(item,index) in state.items" x-class:selected="item.id === state.selectedId">
    {{ index + 1 }}. {{ item.label }}
</li>
```

## Where expressions are used

Expressions provide values for interpolation, conditional directives, bindings, loop sources, class bindings, visibility, and model binding.

```html
<div x-if="state.visible" x-attr:title="state.title"></div>
<input x-model="state.query">
```

Event binding is different: `x-on:event="command"` identifies a named command rather than arbitrary inline JavaScript.

## Security and future direction

The current language does not define a sandboxed or portable expression language. Treat template source as trusted executable input, particularly
where expressions or `x-html` values may originate outside application code.

A restricted, cross-language expression grammar is possible future work. It is not implemented by the current XTemplate language and must not be
assumed by templates or compilers targeting this version.

## Related documentation

- [X Templates](index.md)
- [XTemplate Language Specification](specification.md)
- [Syntax guide](syntax.md)
- [Compiler and runtime architecture](compiler.md)
