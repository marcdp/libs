# ADR-0002: JSONC Specifications

## Status

Accepted as an authoring and composition direction; final schema validation remains planned.

## Context

XShell's human-authored configuration benefits from comments. Earlier checked-in application and module files used flat dotted keys and a separate
`app.jsonc` convention. The newer root and core module definitions use nested objects, while runtime consumers are still being migrated.

## Decision

Use JSONC for root and imported module definitions. The application is the root module. Compose nested `app`, `modules`, and `xshell` objects into one
effective configuration. Merge plain objects recursively, concatenate arrays, and let later scalar values replace earlier ones. Dependencies should
precede importers, with the root last.

Use JSON Schema as the intended validation language for the final merged object in debug/development mode. Fragments may be partial; the effective
configuration is the main validation boundary. Freeze it before handing it to XShell. No browser-native schema validator is assumed.

## Consequences and current status

Bootstrap parses JSONC and merges nested data, but it currently merges in registration order. It performs no JSON Schema validation or deep freeze.
The `Config`, `Resolver`, and other services still expect dotted keys. The legacy sample and server default still point to `app.jsonc`. These must be
reconciled before the nested model is an end-to-end runtime contract.

See [Configuration](../architecture/configuration.md) and [Specifications](../specifications/).
