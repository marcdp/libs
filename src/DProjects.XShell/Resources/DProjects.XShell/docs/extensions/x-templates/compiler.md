# X Template Compiler

This document summarizes how XShell compiles and renders templates.

## Status

Draft.

## Compilation

A string template is parsed through an HTML `template` element. Interpolation markers are converted to `x:text` elements, custom-element dependencies are discovered, and recursive compilation produces JavaScript for a render function.

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
