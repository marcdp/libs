# ADR-0002: JSONC Specifications

This record examines JSON with comments as the format for XShell application and module specifications.

## Status

Draft; current implementation uses JSONC, but the long-term compatibility decision is not declared final.

## Context

Application and module configurations are human-authored and benefit from explanation near settings. Checked-in files use `.jsonc`, and bootstrap removes line and block comments before calling `JSON.parse`.

## Considerations

### Comments

Comments allow maintainers to explain groups and alternatives directly in configuration, but standard JSON parsers cannot consume them without preprocessing.

### Human maintainability

JSONC remains visually close to JSON and is supported by many editors. The current flat, dotted keys can still become difficult to navigate as specifications grow.

### Tooling

Schema validation, formatting, editor completion, and non-JavaScript consumers must deliberately support JSONC and the repository's preprocessing rules.

### Compatibility

Changing to strict JSON would invalidate existing commented files; changing parsing rules could alter how comment-like text in strings is handled. Compatibility requirements need explicit tests.

## TODO

TODO: Decide whether JSONC is the normative format and select a documented parser and schema strategy before accepting this record.

## Related documentation

- [Specifications](../specifications/)
- [Application Specification](../specifications/application.md)
- [Module Specification](../specifications/module.md)
