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
    ↓
dependency discovery
    ↓
render-program generation
    ↓
runtime renderer
```

This pipeline separates parsing, validation, dependency discovery, and rendering concerns. An implementation can choose a different internal
representation or renderer as long as it preserves the language contract.

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

## Ahead-of-time compilation direction

The development/build server can compile XTemplate before browser execution:

```text
development/build server
    → compiles XTemplate ahead of browser execution

generated JavaScript
    → contains templateHandler

browser runtime
    → executes the precompiled render function
```

This direction removes the need for runtime template compilation and improves Content Security Policy compatibility. `new Function(...)` is a
historical/current mechanism of the browser-side compiler, not an XTemplate language feature or a requirement of precompiled templates.

## Related documentation

- [X Templates](index.md)
- [XTemplate Language Specification](specification.md)
- [Expressions](expressions.md)
- [Loaders](../../architecture/loaders.md)
