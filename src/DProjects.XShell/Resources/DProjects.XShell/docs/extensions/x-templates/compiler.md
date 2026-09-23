# X Template Compiler

This document summarizes how XShell compiles and renders templates.

## Status

Draft.

## Compilation

During development, the server locates the `template` property in an exported JavaScript component definition, parses its static template literal, and inserts a
`templateHandler` function without regenerating the rest of the module. The browser uses this precompiled handler directly, so this path does not require `eval` or
`new Function`. An isolated browser compiler remains as a compatibility fallback for modules that were not transformed by the server.

The source scanner understands JavaScript strings, template literals, comments, regular expressions, and nested braces, brackets, and parentheses. It therefore does
not confuse text such as ``"template: `...`"`` or a nested object's `template` property with the exported component template. Recompilation replaces an existing
`templateHandler`, making the transformation idempotent.

The compiler preserves the existing X Template render semantics:

- `{{ expression }}`, `x-text`, `x-html`, and `x-children` select text, HTML, and DOM-node children;
- `x-attr`, `x-attr:name`, `:`, and `:name` bind attribute maps, named attributes, and dynamic attribute names;
- named `x-prop:name`/`.name` bindings target properties; bulk `x-prop`/`.` retains the current browser compiler behavior of spreading its map into attributes;
- `x-on:event` and `@event` create command handlers while retaining dot-separated event modifiers;
- `x-if`, `x-elseif`, and `x-else` retain comment placeholders used by reconciliation;
- `x-for` and `x-key` retain positional/keyed start and end markers;
- `x-recursive` and `x-recursive-wrapper` retain recursive child rendering;
- `x-show`, `x-class:name`, `x-model`, `x-once`, and `x-pre` preserve the browser compiler's visibility, class, form, render-once, and literal-content behavior.

JavaScript expressions are emitted unchanged. Restricting or interpreting the expression language is outside this compiler version.

## Dependencies

Custom element tags containing a hyphen are collected as component dependencies, except that descendants of `x-lazy` are not eagerly collected. The component loader asks the general loader to load discovered dependencies.

## Virtual DOM

The render function produces virtual nodes containing tag, attributes, properties, events, options, and children. The renderer creates DOM on first render and applies later differences, with separate list handling for keyed and positional loops.

## TODO

TODO: Define stable reconciliation semantics, DOM identity guarantees, duplicate-key behavior, whitespace rules, and supported browser prerequisites.

## Related documentation

- [X Templates](index.md)
- [Syntax](syntax.md)
- [Loaders](../../architecture/loaders.md)
