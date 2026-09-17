# Components Architecture

This document describes how XShell resolves, loads, constructs, registers, and instantiates Web Components at runtime.

It complements the author-facing [Components](../components/) documentation, which describes manifests/declarations, properties, state, events, and lifecycle from the component author's perspective.

## Status

Draft.

## Overview

XShell Components are standard browser custom elements whose classes can be produced dynamically from module resources.

Modules receive conventional component resolver definitions during bootstrap. A component request is resolved to its JavaScript source, dispatched through the generic `Loader`, and handled by the `component-js` loader.

For runtime definition objects, the component loader selects state and render engines, loads render-engine dependencies, creates an `HTMLElement` subclass, and registers that class with `customElements`.

```text
logical component reference
    ↓
Resolver
    ↓
resolved JavaScript source + component metadata
    ↓
Loader
    ↓
component-js
    ↓
runtime component definition
    ↓
state engine + render engine
    ↓
HTMLElement subclass
    ↓
customElements.define()
    ↓
component instance
```

A component module may also export an existing class directly. In that case the loader returns that class instead of constructing one from a runtime definition.

## Module component resolution

Bootstrap creates a conventional resolver for each configured module.

For a module named `catalog`, the generated component pattern is conceptually:

```text
component:catalog-{name}
```

and maps to:

```text
/<assetsPrefix>/catalog/components/catalog-{name}.js
```

with metadata including:

```text
loader=component-js
cache=true
module=catalog
modulePath=/<assetsPrefix>/catalog
```

This means a logical reference such as:

```text
component:catalog-product
```

can resolve to the JavaScript file that implements the `catalog-product` custom element.

Layouts use the same `component-js` loader through a separate generated `layout:` resolver.

See [Resolvers](resolvers.md) for the generic resolution model and [Configuration](configuration.md) for generated module resolver configuration.

## Loader dispatch

The generic XShell `Loader` owns resolution, dispatch, caching, events, and load coordination.

After `Resolver` identifies a component resource, `Loader` selects `component-js` from the resolved definition and passes the resolved source together with resource context.

The component-specific loader then owns the component import and class-construction behavior.

This separation is important:

```text
Resolver
    determines where the component resource is

Loader
    dispatches the resolved resource

component-js
    understands how an XShell component module becomes a class
```

See [Loaders](loaders.md) for the generic loader pipeline.

## Component module shapes

The current component loader supports two main runtime forms.

### Existing class

If the module's default export is a JavaScript class, `component-js` returns that class directly.

In this case XShell does not construct a new component class from the declarative runtime definition.

### Runtime definition object

Otherwise the default export is treated as an XShell component definition.

The loader currently consumes runtime fields including:

```text
meta
style
template
state
script
```

If `meta.name` is missing, the loader derives the custom-element name from the JavaScript filename.

The runtime definition is frozen and sealed before class construction.

## Public declaration versus runtime definition

Many XShell components also expose a named `declaration` describing their intended public contract.

That declaration can describe concepts such as:

* public properties;
* events;
* methods;
* descriptions;
* type information.

The current `component-js` loader does **not** consume the named declaration when constructing the custom element. It consumes the module's default runtime definition instead.

The two concepts therefore currently have different roles:

```text
declaration 
    public component contract

implementation
    component implementation consumed by component-js
```

The relationship between these representations is not yet a complete runtime contract.

See [Component Manifest](../components/manifest.md).

## State construction

For a runtime definition, `component-js` builds an initial state skeleton from `definition.state`.

Each state entry currently supports runtime metadata such as:

```text
value
type
attr
prop
reflect
```

Defaults are applied when some of these fields are absent.

The loader also derives information used for:

* observed at
