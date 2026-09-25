# Loaders

Loaders are responsible for loading resources after they have been resolved.

The Resolver decides **where** a resource is and **which loader** should handle it.

The Loader coordinates the actual load.

## Responsibilities

The Loader is responsible for:

* asking the Resolver to resolve a logical resource;
* selecting the resource-specific loader;
* invoking that loader with the resolved URL and context;
* returning the loaded runtime result.

The resource-specific loader is responsible for understanding the resource format.

For example:

```text
icon-svg
    → fetch SVG and return an SVG element

module-js
    → import an ES module

component-js
    → load or build a Web Component class

page-js
    → load or build a Page component
```

## Flow

```text
logical resource
    ↓
Resolver
    ↓
URL + loader metadata
    ↓
Loader
    ↓
resource-specific loader
    ↓
runtime result
```

## Examples

Loading an icon:

```text
icon:x-bell
    ↓
/_assets/x/icons/bell.svg
loader=icon-svg
    ↓
SVG element
```

Loading an ES module:

```text
module:/_assets/module1/module.js
    ↓
/_assets/module1/module.js
loader=module-js
    ↓
ES module export
```

Loading a component:

```text
component:x-button
    ↓
/_assets/x/components/x-button.js
loader=component-js
    ↓
Web Component class
```

Loading a page:

```text
page:/_assets/module1/pages/page1.js
    ↓
/_assets/module1/pages/page1.js
loader=page-js
    ↓
Page component
```

## Loader vs resource-specific loader

It is useful to distinguish the two responsibilities:

```text
Loader
    coordinates loading

resource-specific loader
    knows how to load a particular resource type
```

The generic Loader does not need to know how SVG, JavaScript, Components, or Pages are implemented.

It only dispatches the resolved resource to the appropriate loader.

## Declarative component and Page dependencies

A definition-based Component or Page can declare `dependencies` as an object of ordinary XShell resource references. The Loader accepts that
object, resolves and loads each value through its normal path, and returns an object with the same keys and the loaded values. The corresponding
loader exposes that result to the controller as `controller({ dependencies })`.

```text
dependencies: { fileIcon: "icon:x-file" }
    ↓
Resolver → URL + loader metadata
    ↓
Loader → loaded icon
    ↓
controller({ dependencies }) → dependencies.fileIcon
```

This feature does not add a dependency container or a second loading model. It declares resources known when a Component or Page definition is
loaded. Controller code can still use its `loader` service and `loader.load(...)` for dynamic/runtime resource loading.

If a resource in a declared dependency object cannot be resolved, the Loader rejects the request. If resolution succeeds but one or more loads
fail, the Loader waits for its requested loads to settle and then rejects. The enclosing Component or Page loader cannot finish creating its class
or controller in either case.

## Engines

Some resource-specific loaders use additional engines.

For example, Components and Pages can use:

```text
state engine
render engine
```

Conceptually:

```text
definition-based component or Page loader
 ├─ contract / properties / public API
 ├─ controller / lifecycle
 ├─ state engine → reactive state
 └─ render engine → rendered output
```

The loader is the orchestrator. The state engine creates, observes, and notifies reactive state and requests invalidation. The render engine creates,
mounts, updates, and unmounts rendered output. Neither engine owns contracts, public APIs, controllers, lifecycle, services, property or attribute
semantics, or navigation. Not every loader needs an engine.

When a render engine emits template commands through a handler callback, that callback is a loader-provided bridge. The loader decides how the command
is handled; the callback does not transfer controller or lifecycle ownership to the render engine.

## Diagram

```mermaid
flowchart LR
    A["Logical resource<br/>icon:x-bell"]
        --> B[Resolver]

    B --> C["Resolved URL<br/>/_assets/x/icons/bell.svg<br/>loader=icon-svg"]

    C --> D[Loader]

    D --> E{Resource-specific loader}

    E --> F["icon-svg<br/>SVG element"]
    E --> G["module-js<br/>ES module"]
    E --> H["component-js<br/>Web Component"]
    E --> I["page-js<br/>Page Component"]

    H --> J["State engine<br/>+ Render engine"]
    I --> J
```

## Related documentation

* [Resolvers](resolvers.md)
* [Components](components.md)
* [Pages](pages.md)
* [Configuration](configuration.md)
