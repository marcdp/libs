# XShell

XShell is a browser-native modular application framework built around Web Components, modules, resource resolution, and pluggable loading. X Templates
are optional.

The application is the **root module**. Bootstrap discovers its imports, prepares one nested effective configuration, installs resource mappings, then
starts XShell services, live modules, and navigation. See [Architecture](architecture/) for the flow and implementation limits.
The root composes navigation Areas from reusable module menu contributions; each canonical module still has one live runtime instance.

## Documentation

- [Architecture](architecture/) — Bootstrap, configuration, modules, resources, pages, and navigation.
- [Components](components/) — The Web Component model and public contracts.
- [Subsystems](subsystems/) — Areas, authentication, identity, and internationalization.
- [Extensions](extensions/) — Optional capabilities, including X Templates.
- [Specifications](specifications/) — The root-module and module JSONC model.
- [Architecture Decision Records](adr/) — Decisions and their rationale.

These documents distinguish implemented behavior from intended architecture. The browser resources and documentation live together under
`Resources/DProjects.XShell`.
