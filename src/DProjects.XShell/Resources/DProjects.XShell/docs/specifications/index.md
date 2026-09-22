# Specifications

XShell authors configuration as JSONC. The root module uses the same `module.jsonc` composition model as imported modules; there is no separate
application file format. See [Root Module](application.md) for the application-specific `app` section and [Module](module.md) for definitions,
imports, params, asset locations, and the declarative public contract.
The root also composes [Areas](../subsystems/areas.md) from module ids. Module menu contributions remain independent of that placement.

The main validation boundary is the single nested effective configuration after loading, URL normalization, and merging. Bootstrap does not perform
JSON Schema validation. The X module controller loads `schema:config.schema.json`, resolved from the canonical
`xshell/schemas/config.schema.json` resource, and validates the effective configuration during `start()`; validation failure throws and is not gated
by environment.

The schema defines `app`, `modules`, and `xshell`, including module `configUrl`, `assetsUrl`, defaults, menus, and declarative
`contract.events`/`actions`/`intents`. The schema's `$id` still contains the stale `https://xshell.dev/schemes/config.scheme.json` identifier; the
repository path above is canonical.

See [Configuration](../architecture/configuration.md) and [ADR-0002](../adr/0002-jsonc-specifications.md).
