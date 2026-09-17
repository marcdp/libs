# XShell

XShell is a modular application framework built around Web Components,
declarative templates, resource resolution, and pluggable loading.

This documentation area provides a conservative map of the framework and its design direction. Detailed contracts remain TODOs until they can be verified against runtime code and tests.

## Status

Draft.

## Documentation

- [Architecture](architecture/) — Runtime structure, resource resolution, loading, service-worker rewriting, and navigation.
- [Components](components/) — Web Component declarations, public properties, internal state, events, and lifecycle.
- [X Templates](templates/) — The current declarative template syntax and compiler.
- [Specifications](specifications/) — Application and module JSONC configuration formats.
- [Subsystems](subsystems/) — Authentication, identity, and internationalization.
- [Architecture Decision Records](adr/) — Design questions and architectural direction that should remain visible over time.

## Scope

These pages establish the documentation structure and record only behavior confirmed in the repository. They are not yet a complete user guide or API reference.

## Source of truth

The implementation under `src/DProjects.XShell/Resources/DProjects.XShell` remains authoritative while these documents are drafts.

