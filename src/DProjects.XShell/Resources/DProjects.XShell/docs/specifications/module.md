# Module Specification

This document provides a conservative outline of the XShell module JSONC configuration.

## Status

Draft.

## Loading and merge

Bootstrap discovers module sources from the application configuration, parses each JSONC document, assigns defaults, normalizes URLs against the module asset path, and merges fields beneath `modules.<name>.*`.

## Confirmed fields

Checked-in modules use `name`, `label`, `icon`, `version`, `depends`, `styles`, and optional `handler`. They also configure page and component engines, menus, dialog pages, resolver entries, and other global values.

## Global contributions

Keys prefixed with `global.` are merged into the global configuration after removing that prefix. Resolver contributions receive module and module-path metadata.

## TODO

TODO: Define dependency semantics, handler commands, menu schema, override precedence, extension points, and compatibility rules before publishing a formal schema.

## Related documentation

- [Specifications](index.md)
- [Application Specification](application.md)
- [Modules](../architecture/modules.md)

