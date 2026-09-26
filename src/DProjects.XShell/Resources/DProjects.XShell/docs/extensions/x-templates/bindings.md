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

For the browser XTemplate target, the name `style` is reserved and is invalid in named, dynamic-name, and object-spread `x-attr` bindings. The C#
server HTML renderer rejects style attributes by default and may serialize them only when `XTemplateRendererOptions.AllowStyleAttributes` is enabled.

The transformer pipeline is valid in this value-expression position. Presentation transformers produce locale-aware strings, while predicate
transformers produce booleans; raw scalar conversion without a transformer remains invariant.

## Properties

Use `x-prop:*` to assign a DOM or custom-element property. This preserves objects, arrays, and other values that should not be serialized as
HTML attributes.

```html
<x-datafield x-prop:domain="state.domain" x-prop:value="state.value"></x-datafield>
```

Use the whole-object form when several properties are derived together:

```html
<x-grid x-prop="state.gridProperties"></x-grid>
```

The expression must evaluate to an object; each enumerable string-keyed member is expanded into the property map. Values remain values, including
objects and arrays. In short: `x-attr="..."` expands attributes, while `x-prop="..."` expands properties. `x-prop:[nameExpression]` binds one
dynamically named property, and `.name` is the supported shorthand for `x-prop:name`.

## Literal styles

Literal `style="..."` syntax is valid XTemplate source. In the browser target it is compiled to structured styles / `VNode.styles`, applied through CSSOM,
and never materialized as an HTML style attribute. The C# server HTML renderer rejects it by default and serializes it only when
`XTemplateRendererOptions.AllowStyleAttributes` is enabled:

```html
<div style="display: none; margin-top: 8px"></div>
```

The browser target applies these declarations with `setProperty` and reconciles removals with `removeProperty`; it does not parse the CSS text at
runtime. Dynamic style strings and interpolation are not supported. The C# server HTML renderer rejects literal, generic, and x-pre raw content style
attributes by default because it cannot serialize them without producing a CSP-sensitive inline attribute. An embedding environment may explicitly
enable their serialization through `XTemplateRendererOptions.AllowStyleAttributes` and provide a compatible CSP.

## Named dynamic styles

Use `x-style:<css-property>="expression"` for one dynamic declaration:

```html
<div x-style:border="state.border" x-style:margin-top="state.margin" x-style:--accent-color="state.accent"></div>
```

Only the named form is supported. CSS property names remain CSS syntax, including hyphenated names and custom properties beginning with `--`; they
are not camel-cased. Strings, numbers, and booleans use normal XTemplate scalar conversion; objects and collections are errors. `null` omits the
declaration, and dynamic values do not parse `!important` or set a priority.

Browser named styles contribute to `VNode.styles` and are applied through CSSOM. Server serialization follows `XTemplateRendererOptions.AllowStyleAttributes`;
when enabled, effective literal and dynamic declarations are merged into one style attribute. Declarations follow source order, with later declarations
for the same property winning. Whole-object `x-style` and dynamic-name `x-style:[...]` forms are not supported.

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
