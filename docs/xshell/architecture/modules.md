# Modules

This document outlines how XShell discovers, configures, and initializes modules.

## Status

Draft.

## Configuration

Application keys shaped as `modules.<name>.src` identify module JSONC files. Bootstrap loads those files, applies defaults, normalizes module-relative URLs, and merges module settings into the combined configuration.

## Runtime initialization

The module runtime creates a module record, loads configured styles, optionally loads a JavaScript handler, and invokes the handler's `load` command after all module resources are ready.

## Resource conventions

Bootstrap currently creates resolver entries for a module's icons, layouts, components, pages, and JavaScript modules beneath the configured asset prefix.

## TODO

TODO: Define dependency ordering and failure semantics for `depends`; the current configuration contains the field, but its runtime contract is not established here.

## Related documentation

- [Architecture](index.md)
- [Module Specification](../specifications/module.md)
- [Resolvers](resolvers.md)

