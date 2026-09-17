# Pages Architecture

This document describes how XShell resolves, loads, constructs, hosts, and renders Pages at runtime.

Pages are part of the XShell runtime architecture. They are loaded resources that participate in navigation and are hosted by XShell page containers.

## Status

Draft.

## Overview

A Page begins as a logical page reference, usually produced by navigation.

The reference is resolved through the generic XShell `Resolver`, dispatched through the generic `Loader`, and handled by a page-specific loader according to the source type.

The page loader returns a `Page` class. XShell then instantiates that class, loads it, mounts it into a host, and allows its state and render engines to manage runtime behavior.

```text id="gdy7sk"
Navigation
    ↓
logical page reference
    ↓
Resolver
    ↓
resolved source + page loader metadata
    ↓
Loader
    ↓
page-js / page-html / page-md
    ↓
Page subclass
    ↓
page instance
    ↓
state engine + render engine
    ↓
mounted page content
```

This follows the same general resource pipeline used elsewhere in XShell:

```text id="whw1ym"
logical resource
    ↓
Resolver
    ↓
Loader
    ↓
resource-specific loader
    ↓
runtime result
```

See [Resolvers](resolvers.md) and [Loaders](loaders.md) for the generic resource-loading architecture.

## Module page resolution

Bootstrap creates conventional page resolvers for every configured module.

For a module named `catalog`, paths below that module's asset namespace can be resolved according to their extension.

The generated resolver set currently includes page mappings for:

```text id="dgzrmm"
.js
.html
.md
```

Conceptually:

```text id="qbu8za"
/<assetsPrefix>/catalog/{path}.js
    → page-js

/<assetsPrefix>/catalog/{path}.html
    → page-html

/<assetsPrefix>/catalog/{path}.md
    → page-md
```

Generated resolver metadata also includes the contributing module name and module asset path.

This context later allows page loaders, render engines, and URL processing to understand which module owns the page.

## Resolver boundary

The Resolver determines:

* which configured page pattern matches;
* the concrete source URL;
* which page loader should handle the resource;
* module and module-path metadata;
* other resolver metadata such as caching.

The Resolver does not fetch, import, parse, render, or instantiate the Page.

For example:

```text id="57on4z"
page:/_assets/help/pages/index.md
    ↓
Resolver
    ↓
src=/_assets/help/pages/index.md
loader=page-md
module=help
modulePath=/_assets/help
```

The resulting definition is then passed to the generic Loader.

## Loader dispatch

The generic `Loader` owns dispatch and load coordination.

For Pages, it selects one of the page-specific loaders based on resolver metadata.

Current page loader types include:

```text id="xh4sct"
page-js
page-html
page-md
```

Each loader is responsible for converting its particular source representation into a Page class.

The generic Loader does not need to understand JavaScript page definitions, HTML documents, Markdown, state engines, or render engines.

## JavaScript pages

`page-js` is the core Page class-construction mechanism.

It dynamically imports the resolved JavaScript module and inspects its default export.

The default export may be:

1. an existing class; or
2. an XShell page definition object.

If the default export is already a class, the loader returns it directly.

Otherwise it treats the value as a runtime page definition and constructs a class extending the XShell base `Page`.

The current runtime definition can contain fields such as:

```text id="csqldu"
meta
style
template
state
script
```

The definition is frozen and sealed before the Page class is constructed.

## HTML pages

HTML pages are handled by `page-html`.

The HTML loader converts the document into the runtime definition required by the JavaScript Page class builder, then delegates Page class construction to the same underlying mechanism used by `page-js`.

This means HTML is a source format, not a separate Page runtime model.

Conceptually:

```text id="fn23a9"
HTML source
    ↓
page-html
    ↓
runtime page definition
    ↓
page-js class builder
    ↓
Page subclass
```

See [Loaders](loaders.md) for detailed loader responsibilities.

## Markdown pages

Markdown pages are handled by `page-md`.

The Markdown loader reads the Markdown source, extracts supported metadata, creates a page definition, selects Markdown rendering, and delegates Page class construction to the same page class builder used by JavaScript pages.

Conceptually:

```text id="sdl1gb"
Markdown source
    ↓
page-md
    ↓
runtime page definition
    ↓
renderEngine = markdown
    ↓
page-js class builder
    ↓
Page subclass
```

Markdown therefore remains a Page source format rather than defining a separate Page abstraction.

## Page base class

Dynamically constructed Pages extend the XShell `Page` base class.

The base class owns common Page identity and lifecycle behavior.

Current Page instances expose runtime information including:

```text id="c4wrua"
id
src
label
description
breadcrumb
icon
result
host
refs
```

A Page receives its source and resource context at construction time.

The base lifecycle consists of:

```text id="tc277v"
load()
mount()
unmount()
unload()
```

These lifecycle methods delegate to Page commands and perform common runtime bookkeeping.

## Page lifecycle

At a high level, the current lifecycle is:

```text id="kpv9dm"
Page class loaded
    ↓
Page instantiated
    ↓
load()
    ↓
load command
    ↓
page load event
    ↓
mount()
    ↓
render engine mounts
    ↓
mount command
    ↓
render / invalidate cycles
    ↓
unmount()
    ↓
unmount command
    ↓
render engine unmount
    ↓
unload()
    ↓
unload command
```

Loading a Page class and running a Page instance lifecycle are therefore separate operations.

The Loader produces the class.

Navigation and the Page host manage instances of that class.

## Page host

Pages are not custom elements themselves.

A Page is a runtime object derived from the XShell `Page` class and is mounted into a host.

The XShell page host is responsible for connecting navigation to the Page runtime:

```text id="6wehvx"
Navigation
    ↓
page host
    ↓
load Page class
    ↓
instantiate Page
    ↓
load
    ↓
mount into host DOM
```

This is one of the important differences between Pages and Components.

Components become browser custom-element classes.

Pages remain XShell Page objects hosted by the navigation/page-container infrastructure.

## State construction

For definition-based Pages, `page-js` builds an initial state skeleton from:

```text id="y592io"
definition.state
```

Each state entry can currently include runtime metadata such as:

```text id="m5rfw9"
value
type
qs
reflect
context
```

The initial value becomes part of the state skeleton passed to the selected state engine.

State metadata can also connect Page state to navigation query parameters or Page context.

## State-engine selection

Page state-engine selection follows configuration precedence.

The current lookup is:

```text id="km0dzw"
page.stateEngine
    ↓
modules.<module>.page.stateEngine
    ↓
definition.meta.stateEngine
```

The most specific configured value is selected.

The engine is then loaded through:

```text id="f5a0g0"
state-engine:<name>
```

The resulting factory creates a state object for each Page instance.

Module configuration can therefore establish default state behavior for all Pages belonging to that module while individual Page definitions can override it.

## Render-engine selection

Render engines follow the same configuration pattern:

```text id="ljxc7t"
page.renderEngine
    ↓
modules.<module>.page.renderEngine
    ↓
definition.meta.renderEngine
```

The selected engine is loaded as:

```text id="rbc14r"
render-engine:<name>
```

The render-engine factory receives:

* the Page template;
* scoped Page styles;
* resource context.

The Page loader then loads any dependencies reported by that render-engine factory before initializing it.

The selected render engine is therefore independent of the Page source loader.

For example, a JavaScript or HTML Page can potentially use the same render engine if configured accordingly.

## Render-engine dependencies

A render-engine factory may report dependencies.

The Page class builder loads those dependencies through the same generic XShell Loader before initializing the render engine.

Conceptually:

```text id="u8co7c"
Page definition
    ↓
render engine selected
    ↓
render-engine factory
    ↓
dependencies discovered
    ↓
Loader.load(dependencies)
    ↓
render engine initialized
```

This keeps component/template dependency discovery inside the rendering mechanism rather than the generic Page loader.

## X Templates

X Templates can act as a Page render engine.

They are not the Page model itself.

The relationship is:

```text id="y3vcb1"
Page
    ↓
configured render engine
    ├── plain
    ├── markdown
    ├── x
    └── other configured engines
```

X Templates therefore remain an optional extension layered onto the core Page architecture.

See [X Templates](../extensions/x-templates/).

## Styles

Definition-based Pages may provide inline styles through:

```text id="zhch1e"
definition.style
```

The Page class builder converts those styles to scoped `<style>` blocks and combines them with the Page template before creating the render engine.

The current implementation uses CSS `@scope` to scope these styles to the rendered Page host.

## Runtime scripts and services

A Page definition may contain:

```text id="1gjy7y"
definition.script
```

When a Page instance is created, the runtime invokes this function with a service provider.

The provider currently gives Page scripts access to concepts including:

* the runtime Page definition;
* Page state;
* Page context;
* timer helpers;
* event helpers;
* registered XShell services.

The object returned by `definition.script` is assigned to the Page instance.

This is how definition-based Pages can provide command handlers and other instance methods.

## Query-string state

Page state entries marked with:

```text id="hj01td"
qs: true
```

can initialize their value from URL query parameters.

The runtime converts basic Boolean and numeric values wher
