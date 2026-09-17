# XShell

XShell is a modular application framework built around Web Components, resource resolution, and pluggable loading. Optional extensions, including X Templates, add capabilities on top of that core.

This documentation area provides a conservative map of the framework and its design direction. Detailed contracts remain TODOs until they can be verified against runtime code and tests.

## Status

Draft.

## Documentation

- [Architecture](architecture/) — How XShell is designed: runtime structure, resource resolution, loading, service-worker rewriting, and navigation.
- [Components](components/) — The XShell Web Component programming model, manifests, properties, state, events, and lifecycle.
- [Subsystems](subsystems/) — Runtime services including authentication, identity, and internationalization.
- [Extensions](extensions/) — Optional capabilities layered on XShell, including X Templates.
- [Specifications](specifications/) — Formal application and module configuration contracts.
- [Architecture Decision Records](adr/) — Architectural decisions and their rationale.

## Scope

These pages establish the documentation structure and record only behavior confirmed in the repository. They are not yet a complete user guide or API reference.

## Source of truth

The implementation under `src/DProjects.XShell/Resources/DProjects.XShell` remains authoritative while these documents are drafts.
