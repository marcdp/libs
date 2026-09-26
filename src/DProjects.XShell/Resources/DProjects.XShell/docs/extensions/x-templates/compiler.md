# XTemplate Compiler and Runtime Architecture

This document describes the current XShell implementation architecture. It does not define XTemplate language semantics; those are normative in
the [XTemplate Language Specification](specification.md).

## Conceptual pipeline

```text
XTemplate source
    ↓
HTML fragment parsing
    ↓
Template AST / parsed structure
    ↓
validation
    ├── template dependency structure
    ├── declared slots
    └── render-program generation
    ↓
expression AST evaluation/code generation, including restricted transformer pipelines
    ↓
compiled XTemplate artifact: templateRenderer { render, dependencies, slots }
    ↓
browser x.js → factory metadata + VDOM renderer
```

This pipeline separates parsing, validation, dependency discovery, and rendering concerns. An implementation can choose a different internal
representation or renderer as long as it preserves the language contract.

Transformer syntax is parsed into the same target-neutral expression AST as the rest of the restricted language. A backend evaluates the source,
evaluates transformer arguments when required, applies the specified built-in transformer semantics, and passes the result to the next pipeline stage.
The parser MUST give the pipeline lower precedence than `?:`, while retaining right-associative conditionals; an unparenthesized pipeline therefore
receives the complete preceding conditional. Transformer arguments remain full expression ASTs inside their parentheses, so nested transformer pipelines
are valid where the resulting value kind is accepted. It must not turn transformer names into arbitrary JavaScript or .NET calls. This keeps the browser
and server paths equivalent and CSP-compatible.

Locale-sensitive transformer output MUST follow the XTemplate locale conformance profile (`en-US`, `es-ES`, and `tr-TR`) for the cases defined by the
specification. Compatible locale data SHOULD be used for other locales, but byte-identical output is not required across differing CLDR/ICU versions
outside that profile.

## Current XShell implementation

The current XShell path is conceptually:

```text
template
    → compiled render function
    → VDOM
    → DOM reconciliation
```

The render function produces XShell virtual nodes. The runtime creates DOM on the first render and reconciles later output. VDOM behavior is an
implementation backend, not a requirement for every XTemplate implementation; compiler authors targeting the current XShell runtime should use
the ABI requirements in the specification.

Custom-element dependency discovery is also an implementation concern. The current compiler discovers applicable component dependencies so the
general loader can load them before rendering.

These are template dependencies: resources discovered from XTemplate structure and carried in `templateRenderer.dependencies`. They are distinct from
definition dependencies, which are explicitly declared on the Component or Page definition and exposed to its controller.

## Ahead-of-time compilation direction

The development/build server can compile XTemplate before browser execution:

```text
development/build server
    → compiles XTemplate ahead of browser execution

generated JavaScript
    → contains the compiled XTemplate artifact: templateRenderer { render, dependencies, slots }

browser runtime
    → exposes precompiled dependencies and slots
    → executes the precompiled render function
```

This direction removes the need for runtime template compilation and improves Content Security Policy compatibility. `new Function(...)` is a
historical/current mechanism of the browser-side compiler, not an XTemplate language feature or a requirement of precompiled templates.

The conforming path parses every expression on the server and emits JavaScript only from the validated XTemplate expression AST. Generated renderers
call the private `utils.expr` semantic helpers for member access, arithmetic, truthiness, transformation, and collection handling. The browser does not
parse XTemplate expressions, receive serialized expression ASTs, or compile template source dynamically; it only executes the precompiled
`templateRenderer` with the trusted runtime helpers.

Collection-taking directives use equivalent normalization in the browser `utils.expr.collection` helper and the C# server renderer. In particular,
`null` normalizes to an empty collection for `x-for`, the root `x-recursive` source, and recursive child sources. Arrays and enumerables yield their
items, finite non-negative integers yield `1..N`, strings yield Unicode code points, and objects yield exposed member names; booleans, invalid numbers,
and unsupported values remain errors. Templates therefore use the collection expression directly without a template-side null-coalescing workaround.

The common render-engine factory signature remains `(template, context, templateRenderer)`. The first argument is retained for consistency with plain,
Markdown, and other engines. `x.js` does not parse that source: it interprets the third argument, exposes immutable normalized `dependencies` and
`slots`, and executes `render`. Dependency metadata retains ancestor paths so `x.js` can apply the configured `context.componentLazy` boundary without
scanning DOM. Slots remain static metadata even below lazy or structural directives. Descendants of `x-pre` are opaque and contribute neither kind of
metadata.

The browser runtime requires the complete artifact; renderer-function-only artifacts fail with a compatibility error and must be regenerated. It has
no raw-XTemplate `template.innerHTML` path, source compiler, expression parser, `eval`, or `new Function` fallback. URL-bearing attributes are rewritten
by generated `utils.rewriteAttribute(...)` calls using their static sibling attributes, not by mutating a temporary template DOM. This keeps the normal
browser path compatible with `script-src 'self'`, `style-src 'self'`, and `style-src-attr 'none'` without `unsafe-eval` or `unsafe-inline`.

## Literal inline-style pipeline

Literal XTemplate style declarations follow a separate compiler path from ordinary attributes:

```text
style declaration text
    → C# declaration-list parser
    → structured { value, priority } entries in generated JavaScript
    → VNode.styles
    → browser x.js
    → CSSStyleDeclaration.setProperty()/removeProperty()
```

The C# parser recognizes declaration separators only at the top level, accounting for quoted strings, escapes, brackets, and nested functions. It
preserves CSS property names such as `margin-top` and `--custom-value`, and separates a trailing `!important` into the entry's `priority` field. The
browser never receives or reparses the original declaration string.

On initial creation, the browser applies each declaration with `setProperty`. During reconciliation it updates added declarations, changed values, and
changed priorities; it removes only properties present in the preceding XTemplate VNode and absent from the new VNode. Unrelated CSSOM properties are
left intact. In the browser XTemplate target, generic `x-attr` paths reject the reserved case-insensitive name `style`, including spread and runtime-derived names.

This path never calls `setAttribute("style", ...)` or assigns `style.cssText`, so it remains compatible with strict policies including
`style-src-attr 'none'` and does not require `'unsafe-inline'`.

`XTemplateRenderer` targets serialized server HTML rather than a browser CSSOM. It rejects a resolved style attribute by default from every static,
bound, spread, dynamic, or x-pre raw content source because serializing that value creates `style="..."`. An embedding environment may explicitly enable
style-attribute serialization through `XTemplateRendererOptions.AllowStyleAttributes`; the caller is then responsible for deploying a CSP compatible
with emitted inline style attributes. The option does not affect `<style>` elements or the browser's structured CSSOM path.

## Browser `x-model` assignment policy

The browser's plain-object XTemplate model requires the final object member named by an `x-model` assignment to already exist and be writable.
Implicit member creation is not supported. This is intentional: XTemplate permits creation only when the active context adapter explicitly allows it,
and the browser plain-object adapter exposes no such capability. Intermediate members must likewise exist and be writable objects or valid collection
entries.

## Related documentation

- [X Templates](index.md)
- [XTemplate Language Specification](specification.md)
- [Expressions](expressions.md)
- [Loaders](../../architecture/loaders.md)
