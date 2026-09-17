# Application Specification

This document provides a conservative outline of the XShell application JSONC configuration.

## Status

Draft.

## Loading

Bootstrap obtains the application configuration URL from the `xshell.app_config_url` meta element, fetches the file, removes JavaScript-style comments, parses JSON, normalizes URL-like values, and merges it over framework defaults.

## Confirmed groups

The sample application uses `app.*` metadata, `modules.<name>.src` module references, module parameters, XShell debugging and identity settings, and area configuration. These examples do not yet constitute a complete schema.

## URL values

The current normalizer gives special treatment to values prefixed with `url:` and to root-relative or dot-relative strings. The precise normalization contract remains a TODO.

## TODO

TODO: Define required fields, allowed types, unknown-key handling, environment overrides, validation errors, and version compatibility.

## Related documentation

- [Specifications](index.md)
- [Module Specification](module.md)
- [JSONC Specifications ADR](../adr/jsonc-specifications.md)

