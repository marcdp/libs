# ADR-0002: JSONC Specifications

## Status

Accepted for JSONC authoring, nested composition, and effective-configuration schema validation; module action/intent metadata does not implement
dispatch.

## Context

XShell's human-authored configuration benefits from comments. Earlier checked-in application and module files used flat dotted keys and a separate
`app.jsonc` convention. Current root and core module definitions use nested objects. That legacy context is not the current configuration model.

## Decision

Use JSONC for root and dependency module definitions. The application is the root module. Each document has one local `modules.<id>` definition
without `configUrl`; its other module entries are external references. Compose nested `app`, `modules`, and `xshell` objects into one effective
configuration. Merge plain objects recursively, concatenate arrays, and let later scalar values replace earlier ones. Dependencies precede their
dependents, with the root last. References may contribute configuration to their canonical effective modules; `assetsUrl` remains owned by the
referenced local definition.

Use JSON Schema as the validation language for the final merged object. Fragments may be partial; the effective configuration is the main validation
boundary. Deeply freeze it before handing it to XShell. No browser-native schema validator is assumed.

## Consequences and current status

Bootstrap parses JSONC, validates local and referenced module identities, deduplicates referenced definitions by URL, rejects conflicting identities
and cycles, and merges nested data in deterministic dependency-first order with the root last. After worker mapping, inventory loading, and resolver
generation, bootstrap awaits XShell's effective schema validation, deeply freezes the object, and calls init. Init validates again before runtime
services are constructed. The X module controller is empty; validation is not environment-gated. The canonical schema is
`xshell/schemas/config.schema.json`; it requires module defaults for definition-based Page and Component engines and defines XShell UI defaults
for layouts, lazy/error components, and standard dialog pages, plus `contract.events`/`actions`/`intents`. Its `$id` still uses the stale
`https://xshell.dev/schemes/config.scheme.json` identifier. The server default points to `modules/x-demo/module.jsonc`. Browser JSONC supports
comments but not trailing commas.

See [Configuration](../architecture/configuration.md) and [Specifications](../specifications/).
