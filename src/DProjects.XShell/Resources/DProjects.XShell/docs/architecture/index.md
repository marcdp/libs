# Architecture

XShell is a modular application framework built around configuration, modules, resource resolution, loading, Web Components, and browser navigation.

At a high level:

```text
Initial HTML
    ↓
Bootstrap
    ↓
Runtime configuration
    ↓
Modules
    ↓
Service Worker + /_assets
    ↓
Resolvers
    ↓
Loaders
    ↓
Components / Pages
    ↓
Navigation
```

## Overview

The host page loads `bootstrap.js` and provides the initial XShell configuration.

Bootstrap then:

```text
loads configuration
    ↓
loads module definitions
    ↓
assembles the flat runtime configuration
    ↓
installs the Service Worker
    ↓
creates the import map
    ↓
imports XShell
    ↓
initializes services, modules, navigation, and menus
```

Applications are composed from modules:

```text
Application = Module 1 + Module 2 + ... + Module N
```

Each module contributes configuration and static resources.

Module resources are exposed to the browser through a uniform namespace:

```text
/_assets/<module>/...
```

The Service Worker hides where those resources are physically stored.

Resource loading then follows the standard pipeline:

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

Components and Pages use the same component programming model.

Conceptually:

```text
Component
    ├── regular UI component
    └── Page
            +
        Navigation
```

## Documents

* [Bootstrap](bootstrap.md) — How the browser starts XShell and initializes the runtime.
* [Configuration](configuration.md) — The flat, read-only runtime configuration assembled during bootstrap.
* [Modules](modules.md) — How modules compose an application and contribute configuration and resources.
* [Components](components.md) — The Web Component declaration and implementation model.
* [Pages](pages.md) — Components used as navigation destinations, including query-string properties.
* [Resolvers](resolvers.md) — Conversion of logical resource names into concrete resource URLs and loader metadata.
* [Loaders](loaders.md) — Dispatching resolved resources to resource-specific loaders.
* [Service Worker](service-worker.md) — Uniform access to module resources through the `/_assets` namespace.
* [Navigation](navigation.md) — Path and hash navigation.

## Related documentation

* [XShell documentation](../)
* [Specifications](../specifications/)
* [Subsystems](../subsystems/)
* [Asset URL Namespace](../adr/0001-asset-url-namespace.md)
