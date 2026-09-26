# XTemplate Bindings

This guide introduces practical XTemplate binding concepts. For exact semantics, supported controls, advanced dynamic bindings, and known
compatibility issues, see the [XTemplate Language Specification](specification.md).

## Attributes

Use `x-attr:*` to bind DOM attributes. Attributes are serialized values suitable for HTML concerns such as links, labels, and ARIA metadata.

```html
<a x-attr:href="state.url" x-attr:aria-label="state.label">Open</a>
<button x-attr:disabled="state.saving">Save</button>
<div x-attr:data-price="state.price | number(2)"></div>
```

`x-attr` accepts an attribute object when several attributes are derived together. Its object member names must be strings; dictionary-backed values
must use string keys and non-string keys are not converted with `key.ToString()`. `:name` and `:` are supported shorthand forms, but the long
forms are preferred in canonical templates.

The name `style` is reserved and is invalid in named, dynamic-name, and object-spread `x-attr` bindings.

The transformer pipeline is valid in this value-expression position. Presentation transformers produce locale-aware strings, while predicate
transformers produce booleans; raw scalar conversion without a transformer remains invariant.

## Properties

Use `x-prop:*` to assign a DOM or custom-element property. This preserves objects, arrays, and other values that should not be serialized as
HTML attributes.

```html
<x-datafield x-prop:domain="state.domain" x-prop:value="state.value"></x-datafield>
```

In short: `x-attr:*` targets DOM attributes; `x-prop:*` targets DOM properties. `.name` is the supported shorthand for `x-prop:name`.

## Literal styles

Literal style declaration syntax is compiled to structured browser CSSOM operations rather than an HTML style attribute:

```html
<div style="display: none; margin-top: 8px"></div>
```

The browser target applies these declarations with `setProperty` and reconciles removals with `removeProperty`; it does not parse the CSS text at
runtime. Dynamic style strings and interpolation are not supported. The C# server HTML renderer rejects literal, generic, and raw `x-pre` style
attributes by default because it cannot serialize them without producing a CSP-sensitive inline attribute. An embedding environment may explicitly
enable their serialization through `XTemplateRendererOptions.AllowStyleAttributes` and provide a compatible CSP.

## Events

`x-on:event="command"` dispatches a named command; it is not arbitrary inline JavaScript.

```html
<button x-on:click="save">Save</button>
<input x-on:keydown.enter="submit">
<a x-on:click.prevent="open">Open</a>
```

Common modifiers include `.stop`, `.prevent`, keyboard filters such as `.enter` and `.escape`, and mouse or modifier-key filters. `@event` is the
supported shorthand. Use the specification for the complete modifier contract.

## Classes

`x-class:name` conditionally adds a CSS class while retaining authored static classes.

```html
<li class="menuitem" x-class:selected="item.id == state.selectedId"></li>
```

## Visibility

Use `x-if` when a condition should structurally render or omit content. Use `x-show` when the content should remain structurally present while
its visibility changes.

```html
<x-spinner x-if="state.loading"></x-spinner>
<aside x-show="state.detailsVisible">Details</aside>
```

## Model binding

`x-model` conceptually reads an assignable expression into a control and writes user changes back before requesting a new render.

```html
<input x-model="state.query">
<x-datafield x-model="state.selection"></x-datafield>
```

Use the specification when working with control-specific behavior, advanced model targets, or compatibility constraints.

## Related documentation

- [X Templates](index.md)
- [XTemplate Language Specification](specification.md)
- [Syntax guide](syntax.md)
- [Component Events](../../components/events.md)
