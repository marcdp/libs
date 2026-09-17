# Loaders

This document describes XShell's dispatcher for resolved resources, its type-specific loader modules, and the concrete engine delegation used by pages
and components.

## Status

Draft.

## Responsibility and pipeline

`Loader` accepts one logical reference or an array of references. For each reference it asks `Resolver` for a source and definition, locates or imports
the definition's loader, and invokes that loader with the source plus resource and application context.

```text
logical resource reference
    -> Resolver.resolve()
    -> source URL + resource definition/loader metadata
    -> Loader dispatch
    -> resource-specific loader
    -> render/state engine when that loader uses one
    -> class, module value, or DOM node
```

The resolver does no I/O. `Loader` owns dispatch and coordination, while the selected resource-specific loader owns the concrete fetch, dynamic import,
parsing, or class construction.

## Current loader types

The repository contains loaders for component JavaScript, SVG icons, JavaScript modules, and JavaScript, HTML, and Markdown pages. Resolver configuration
selects the loader by name. Built-in names are imported from `xshell/loaders/<name>.js`; definitions may also identify a loader by URL or path.

The concrete results differ by loader:

- `icon-svg` fetches SVG text and returns its first DOM node;
- `module-js` dynamically imports a module and returns its default export;
- `page-js` imports a page definition and returns a `Page` subclass, or returns an exported class directly;
- `page-html` parses HTML into a page definition and delegates class creation to the `page-js` helper;
- `page-md` extracts simple front matter, marks the definition for Markdown rendering, and delegates class creation to the `page-js` helper;
- `component-js` imports a component definition and returns or constructs a custom-element class.

## Loader engines

There is no single generic loader-engine interface across every resource type. Page and component class creation select concrete state and render engines
from the assembled configuration and load them as `state-engine:<name>` and `render-engine:<name>` resources. A render-engine factory may report component
dependencies; the page/component loader asks the same `Loader` to load those dependencies before initializing the factory.

The checked-in render engines are `plain`, `markdown`, and `x`; the checked-in state engines are `none`, `plain`, and `proxy`. X Templates are therefore
one optional render-engine implementation, not the core loader or component model.

## Worked example: an XShell Help Markdown page

The XShell Help module provides `/pages/index.md`, which becomes the virtual asset path `/_assets/x-help/pages/index.md` during module configuration
normalization.

1. Navigation creates an `x-page` for that path. `x-page` requests `page:/_assets/x-help/pages/index.md`.
2. The module-generated page resolver matches the `.md` path and returns the source URL with `loader=page-md`, `module=x-help`, and the module
   asset path.
3. `Loader` imports the built-in `page-md` loader and dispatches the resolved source and context to it.
4. `page-md` fetches the Markdown, extracts its simple front matter, sets `meta.renderEngine` to `markdown`, and passes the definition to
   `createPageClassFromJsDefinition`.
5. The class builder loads the configured state engine and `render-engine:markdown`. The Markdown engine uses the `marked` import-map entry contributed by
   the core `x` module, converts Markdown to HTML, discovers any custom-element dependencies, and rewrites document URLs with module context.
6. The loader returns a `Page` subclass. `x-page` instantiates it, then its render engine mounts the concrete DOM content.

This example is the full resolver -> loader -> specialized engine path. Simpler loaders, such as `icon-svg`, return a result without an engine.

## Cache and registry

Definitions marked with `cache=true` reuse the stored load promise for the same logical reference. Returned DOM nodes are cloned when possible, and values
with a `clone()` method are cloned. The loader also maintains a read-only projection of resource, source, and status entries for diagnostic use.

## Errors and events

The loader waits for its dispatched or reused load tasks with `Promise.allSettled`. Load failures are converted to resource errors and collected into an
aggregate loader error, while the event bus receives fetch, loaded, and error notifications.

## TODO

TODO: Specify retry behavior and the exact ordering guarantees for mixed cached and newly loaded resource arrays.

## Related documentation

- [Architecture](index.md)
- [Resolvers](resolvers.md)
- [Component Lifecycle](../components/lifecycle.md)
- [Bootstrap](bootstrap.md)
