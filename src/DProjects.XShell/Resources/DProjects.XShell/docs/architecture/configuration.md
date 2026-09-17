# Configuration Architecture

This document explains how runtime configuration is assembled and exposed. It does not define every valid application or module field; those contracts
belong in the [Application Specification](../specifications/application.md) and [Module Specification](../specifications/module.md).

## Status

Draft.

## Sources and precedence

Bootstrap builds a single flat map whose keys commonly use dotted namespaces.

| Stage | Source | Effect |
| --- | --- | --- |
| 1 | Framework `xshell.jsonc` | Supplies navigation, engine, resolver, identity, and other defaults. |
| 2 | Application JSONC | Replaces framework values with the same key and declares modules through `modules.<name>.*`. |
| 3 | Each module JSONC in turn | Writes ordinary fields under `modules.<name>.*` and removes `global.` from global contributions. |
| 4 | Resolvers generated immediately for that module | Adds its conventional icon, layout, component, page, and module mappings. |

Assignment is direct: a value written by a later stage replaces an existing value with the same key. Modules are processed in the order in which their names
are first encountered while iterating the assembled application configuration. A later module contribution can replace the same key written by an earlier
module, including an earlier generated resolver. This iteration order is current behavior, not a declared dependency or override contract.

The `depends` module field is defaulted but is not used to order configuration loading or runtime initialization.

## Application and module namespaces

The application configuration supplies application-wide values and module entries. `modules.<name>.src` points to a module JSONC document, while
`modules.<name>.params.*` remains in the combined map and is passed to the module handler's `load` command.

For a discovered module named `catalog`, a module field such as `styles` becomes `modules.catalog.styles`. Bootstrap sets the merged module `name` to the
alias discovered from the application configuration, regardless of the module document's original `name` value.

A module key such as `global.page.layout.main` becomes `page.layout.main`. A key such as `global.resolver.import:library` becomes
`resolver.import:library`; resolver contributions are also augmented with the contributing module name and module asset path. Consequently, module
contributions can be consumed through the same global configuration, resolver, and import-map mechanisms as framework and application values.

## URL normalization

Normalization walks strings recursively through objects and arrays before a source is merged. Its base differs by source:

- framework defaults use `/<assetsPrefix>/xshell`;
- application values use the resolved application JSONC URL;
- module values use `/<assetsPrefix>/<module-name>`.

The current normalizer treats values as follows:

| Form | Current treatment |
| --- | --- |
| `url:<value>` | Removes `url:` and resolves root- or dot-relative values with the current base. |
| `/value` | Prefixes the current normalization base. In module JSONC this produces a module-relative asset URL. |
| `./value`, `../value`, or `.` | Combines the value with the current base. |
| Other strings | Leaves the main value unchanged. |

It also normalizes semicolon-delimited attribute values that contain `=/`. Resolver definitions use this form when metadata embeds a URL-like value.

The distinction between `url:` and an unmarked leading slash is significant. For example, the sample application uses
`url:../../modules/x/module.jsonc` to resolve a module source against `app.jsonc`, while module documents use `/pages/...` and `/css/...` values that are
placed beneath that module's asset path.

See [ADR-0002](../adr/0002-jsonc-specifications.md) for the current JSONC format decision and its unresolved compatibility questions.

## Runtime availability

All framework, application, module, global, and generated resolver values are assembled before the import map is created and before `xshell` is imported.
The runtime then wraps the map in `Config`, freezes the top-level map, and supplies that service to the other runtime objects.

Module handlers see the final map when their `load` command runs. A module's non-global fields are available through `modules.<name>.*`; a `global.*`
contribution is already visible at its prefix-free key.

`Config` provides exact lookup, prefix enumeration, and projections such as `getAsObject` and `getAsObjects`. These projections do not perform another
merge or normalization pass.

## Architecture versus specification

This page documents loading, normalization, precedence, and runtime visibility. Field names, types, and requirement status are documented conservatively in
the [Application Specification](../specifications/application.md) and [Module Specification](../specifications/module.md).

## TODO

TODO: Define a stable collision policy and ordering contract for multiple modules that contribute the same global key.

TODO: Specify validation and diagnostics for missing module sources, non-string `global.*` contributions, and malformed JSONC.

## Related documentation

- [Bootstrap](bootstrap.md)
- [Modules](modules.md)
- [Resolvers](resolvers.md)
- [JSONC Specifications ADR](../adr/0002-jsonc-specifications.md)
