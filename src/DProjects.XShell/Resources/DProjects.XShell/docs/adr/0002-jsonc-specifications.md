# ADR-0002: JSONC Specifications

## Status

Accepted for JSONC authoring and nested composition; final schema validation remains planned.

## Context

XShell's human-authored configuration benefits from comments. Earlier checked-in application and module files used flat dotted keys and a separate
`app.jsonc` convention. Current root and core module definitions use nested objects. That legacy context is not the current configuration model.

## Decision

Use JSONC for root and imported module definitions. The application is the root module. Compose nested `app`, `modules`, and `xshell` objects into one
effective configuration. Merge plain objects recursively, concatenate arrays, and let later scalar values replace earlier ones. Dependencies should
precede importers, with the root last.

Use JSON Schema as the intended validation language for the final merged object in debug/development mode. Fragments may be partial; the effective
configuration is the main validation boundary. Deeply freeze it before handing it to XShell. No browser-native schema validator is assumed.

## Consequences and current status

Bootstrap parses JSONC, deduplicates imported definitions by URL, and merges nested data in reverse registration order. The first registered
import retains its params; later imports do not replace or merge them. Reverse registration order does not guarantee dependency-first precedence
for every graph. Bootstrap deeply freezes the merged configuration before XShell receives it, but performs no JSON Schema validation. The legacy
sample and server default still point to `app.jsonc` and need migration.

See [Configuration](../architecture/configuration.md) and [Specifications](../specifications/).
