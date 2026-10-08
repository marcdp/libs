# XTemplate Syntax Guide

This is a concise guide to commonly used XTemplate syntax. For normative syntax and semantics, including validation rules and edge cases, see the
[XTemplate Language Specification](10-specification.md).

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
<code>{{ state.value | json_stringify }}</code>
<span x-text="state.json | json_parse | json_stringify"></span>
<span x-if="state.type | endsWith('_i18n')">Languages</span>
<div x-attr:data-price="state.price | number(2)"></div>
```

Pipelines run left to right and have lower precedence than `?:`. Therefore `state.ok ? 'yes' : 'no' | upper` transforms the complete conditional;
parenthesize a branch when only that branch should be transformed. Locale-sensitive transformers use the active XShell/i18n locale, while
ordinary scalar conversion remains invariant. General function calls and object methods remain invalid; use `number(2)` instead of `toFixed(2)` and
`upper` instead of `toUpperCase()`. The JavaScript and C# backends share the `en-US`, `es-ES`, and `tr-TR` locale conformance profile; see the
specification for its required numeric, percent, currency, month-name, and casing cases.

`json_stringify` accepts any JSON-compatible XTemplate value, takes no arguments, and returns compact JSON. `json_parse` accepts a JSON string, takes no
arguments, and returns the corresponding XTemplate value. Parsed objects and arrays participate in ordinary member access, indexing, and collection
operations. For example, an object value `{ name: "Marc", enabled: true }` stringifies as `{"name":"Marc","enabled":true}`, and
`state.value | json_stringify | json_parse` round-trips a JSON-compatible value. Unlike other transformers, `json_stringify` serializes `null` as
the string `null`; `json_parse` converts the JSON string `"null"` back to the XTemplate value `null`.

## Attributes and properties

```html
<a x-attr:href="state.url">Open</a>
<div x-attr="state.attributes"></div>
<x-datafield x-prop:domain="state.domain"></x-datafield>
<x-grid x-prop="state.gridProperties"></x-grid>
```

`x-attr:name` binds one attribute, while `x-prop:name` binds one DOM or custom-element property. The bracketed forms
`x-attr:[nameExpression]` and `x-prop:[nameExpression]` bind one dynamically named value. Whole-object `x-attr` expands an attribute object;
whole-object `x-prop` expands an object into the property map and preserves its values.

Literal `style="display:none; width:100%"` is valid XTemplate source. In the browser target, the compiler produces structured styles / `VNode.styles`,
which the renderer applies through CSSOM without materializing an HTML style attribute; generic `x-attr` bindings MUST NOT target `style`. The C# server
HTML renderer rejects style attributes by default, including in x-pre raw content, and can serialize them only when
`XTemplateRendererOptions.AllowStyleAttributes` is enabled.

## Server-only style and script elements

The browser `XTemplateCompiler` always rejects `<style>` and `<script>` elements. This rule has no compiler option and applies even inside `x-pre`.
The server `XTemplateRenderer` also rejects both by default, but an embedding application may independently opt in with
`XTemplateRendererOptions.AllowStyleElements` and `XTemplateRendererOptions.AllowScriptElements`. These options do not enable
`style=""` attributes; those remain independently controlled by `AllowStyleAttributes`.

When enabled for server rendering, `<style>` and `<script>` element bodies are raw text. Their enclosing elements can use structural directives such
as `x-if`, but their bodies do not parse HTML, directives, or `{{ expression }}` interpolation. `x-pre` does not bypass the element or attribute
policies. Callers that enable style or script output are responsible for using an appropriate CSP and for the security implications of their output;
these options are not a sandbox or a security boundary.

Named dynamic styles use `x-style:<css-property>="expression"`. The property name remains CSS syntax (`margin-top` is not camel-cased, and `--accent`
is a custom property), values use scalar conversion, and `null` omits the declaration. Browser output remains structured `VNode.styles`/CSSOM; server
serialization follows `XTemplateRendererOptions.AllowStyleAttributes`. Later literal, whole-object, or named declarations win by source order.

Whole-object structured styles use `x-style="expression"`:

```html
<div x-style="state.styles"></div>
```

The source must be a non-array object. Own enumerable string-keyed members are validated as CSS property names; scalar string, finite number,
boolean, and date/time values are converted with normal XTemplate scalar rules. `null` members are omitted, and object or collection members are
errors. Whole-object values always have an empty priority, so runtime `!important` text is not parsed. The browser writes the resulting `VNode.styles`
through CSSOM, while server
serialization follows `XTemplateRendererOptions.AllowStyleAttributes`. Dynamic-name `x-style:[...]` remains unsupported.

Shorthand `:name`, `:`, `.name`, and `@event` bindings are unsupported. Use the explicit `x-*` forms.

## Events, classes, and visibility

```html
<button x-on:click="save">Save</button>
<li x-class:selected="state.selected"></li>
<section x-show="state.expanded">Details</section>
```

`x-on:event` binds a named command. `x-class:name` conditionally adds a class. `x-show` keeps the element in the rendered structure while
using `display: none` for a falsy expression and restoring the prior inline display value when it becomes truthy.

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

`x-model` provides read/write model binding. `x-once` preserves content after its first render, and `x-pre` produces x-pre raw content from its literal child subtree.

## Related documentation

- [X Templates](index.md)
- [XTemplate Language Specification](10-specification.md)
- [Expressions](30-expressions.md)
- [Bindings](40-bindings.md)
