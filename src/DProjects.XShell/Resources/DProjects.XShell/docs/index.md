# XShell

XShell is a browser-native modular application framework built around Web Components, modules, resource resolution, pluggable loaders, render engines, state engines, and Service Worker resource virtualization.

X Templates are an optional rendering extension built on top of the core Component and Page model.

The application itself is the **root module**. From it, XShell discovers imported modules, builds the effective configuration, prepares resource mappings, initializes the runtime, starts module instances, composes Areas, and starts navigation.

## Documentation

- [Architecture](architecture/) — Bootstrap, configuration, modules, Components, Pages, resource resolution, loaders, Service Worker behavior, packaging, and navigation.
- [Components](components/) — Public component contracts, properties, state, events, slots, lifecycle, and rendering.
- [Subsystems](subsystems/) — Areas, authentication, identity, internationalization, and other runtime subsystems.
- [Extensions](extensions/) — Optional XShell extensions, including X Templates.
- [Specifications](specifications/) — Formal application and module configuration contracts.
- [Architecture Decision Records](adr/) — Architectural decisions and their rationale.

## Core concepts

### Modules

Modules are the main unit of composition and distribution.

A module can contribute resources such as Components, Pages, styles, menus, configuration, resolvers, and runtime behavior.

The application is also represented as a module: the **root module**.

See [Modules](architecture/modules.md) and [Module Specification](specifications/module.md).

### Components

XShell builds on standard Web Components.

A Component can be implemented directly as a Web Component class or as an XShell definition composed of a public contract and an implementation.

Definition-based Components can use pluggable state and render engines.

See [Components](components/) and [Component Architecture](architecture/components.md).

### Pages

A Page uses the same Component model while adding navigation-oriented behavior.

```text
Page = Component + Navigation
```

See [Pages](architecture/pages.md).

### Resources

Logical resource references are handled through separate Resolver and Loader layers.

```text
logical resource
    ↓
Resolver
    ↓
URL + Loader
    ↓
resource
```

The Service Worker exposes module resources through a stable virtual namespace such as:

```text
/_assets/<module-id>/...
```

See [Resolvers](architecture/resolvers.md), [Loaders](architecture/loaders.md), and [Service Worker](architecture/service-worker.md).

### Areas and navigation

Modules provide reusable navigation contributions.

Areas compose participating modules into application navigation contexts, including menus and home destinations.

See [Areas](subsystems/areas.md) and [Navigation](architecture/navigation.md).

## Documentation conventions

These documents describe both implemented behavior and architectural direction.

When a feature is incomplete or still being designed, the documentation marks it explicitly with terms such as **Draft**, **TODO**, or **future work**.

Documentation under this directory is part of the XShell runtime resources and is intended to be browsable directly by XShell itself.