# XTemplate Syntax Guide

This is a concise guide to commonly used XTemplate syntax. For normative syntax and semantics, including validation rules and edge cases, see the
[XTemplate Language Specification](specification.md).

## Content

```html
<p>Hello {{ state.name }}</p>
<span x-text="state.label"></span>
<div x-html="state.html"></div>
<span x-children="state.iconNode"></span>
```

`{{ expression }}` and `x-text` render text. `x-html` renders raw HTML, and `x-children` renders DOM-node content.

## Transformers

Use the restricted transformer pipeline for explicit presentation formatting and string predicates. Transformers are available anywhere an ordinary value expression is
accepted, including attribute bindings:

```html
<p>{{ state.price | number(2) }}</p>
<p>{{ state.total | currency('EUR') }}</p>
<p>{{ state.ratio | percent(1) }}</p>
<p>{{ state.createdAt | date('dd/MM/yyyy') }}</p>
<p>{{ state.createdAt | datetime('dd/MM/yyyy HH:mm') }}</p>
<p>{{ state.name | trim | upper }}</p>
<span x-if="state.type | endsWith('_i18n')">Languages</span>
<div x-attr:data-price="state.price | number(2)"></div>
```

Pipelines run left to right and have lower precedence than `?:`. Therefore `state.ok ? 'yes' : 'no' | upper` transforms the complete conditional;
parenthesize a branch when only that branch should be transformed. Locale-sensitive transformers use the active XShell/i18n locale, while
ordinary scalar conversion remains invariant. General function calls and object methods remain invalid; use `number(2)` instead of `toFixed(2)` and
`upper` instead of `toUpperCase()`. The JavaScript and C# backends share the `en-US`, `es-ES`, and `tr-TR` locale conformance profile; see the
specification for its required numeric, percent, currency, month-name, and casing cases.

## Attributes and properties

```html
<a x-attr:href="state.url">Open</a>
<div x-attr="state.attributes"></div>
<x-datafield x-prop:domain="state.domain"></x-datafield>
```

`x-attr:name` binds an attribute, while `x-prop:name` binds a DOM or custom-element property. `x-attr` expands an attribute object.

Literal `style="display:none; width:100%"` is a dedicated browser-target feature: the compiler produces structured styles and the renderer applies
them through CSSOM. `style` is not available through `x-attr` in the browser target. The C# server HTML renderer rejects style attributes by default,
including in raw `x-pre` content, and can explicitly allow them through `XTemplateRendererOptions.AllowStyleAttributes`.

The supported shorthand forms are `:name` for `x-attr:name`, `:` for `x-attr`, `.name` for `x-prop:name`, and `@event` for `x-on:event`.
Canonical documentation and new templates should prefer the long `x-*` forms.

## Events, classes, and visibility

```html
<button x-on:click="save">Save</button>
<li x-class:selected="state.selected"></li>
<section x-show="state.expanded">Details</section>
```

`x-on:event` binds a named command. `x-class:name` conditionally adds a class. `x-show` keeps the element in the rendered structure while
controlling its visibility.

## Structure

```html
<x-spinner x-if="state.loading"></x-spinner>
<x-error x-elseif="state.error"></x-error>
<main x-else>Ready</main>

<li x-for="(item,index) in state.items" x-key="id">
    {{ index + 1 }}. {{ item.label }}
</li>
```

Use `x-if`, `x-elseif`, and `x-else` for conditional structural rendering. Use `x-for` to repeat an element and `x-key` to supply item identity.

```html
<li x-recursive="item in state.menu" x-key="href" x-recursive-wrapper="ul">
    <span x-text="item.label"></span>
</li>
```

`x-recursive` repeats through a tree; `x-recursive-wrapper` supplies a wrapper for child levels.

## Model and render controls

```html
<input x-model="state.query">
<section x-once>Rendered once</section>
<code x-pre>{{ literalBraces }}</code>
```

`x-model` provides read/write model binding. `x-once` preserves content after its first render, and `x-pre` leaves its child subtree literal.

## Related documentation

- [X Templates](index.md)
- [XTemplate Language Specification](specification.md)
- [Expressions](expressions.md)
- [Bindings](bindings.md)
