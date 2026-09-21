# Specifications

XShell authors configuration as JSONC. The root module uses the same `module.jsonc` composition model as imported modules; there is no separate
application file format. See [Root Module](application.md) for the application-specific `app` section and [Module](module.md) for definitions,
imports, params, and the proposed public contract.
The root also composes [Areas](../subsystems/areas.md) from module ids. Module menu contributions remain independent of that placement.

The main validation boundary is the single nested effective configuration after loading, URL normalization, and merging. Bootstrap does not perform
JSON Schema validation. The X module controller currently fetches `xshell/schemes/config.scheme.json` through its `/_assets/xshell/...` URL and
validates the effective configuration during `start()`, logging failures to `console.error`; this is not gated by environment.

The schema is transitional rather than a finalized authoritative contract: it supports module `defaults` while still permitting legacy top-level
module `page` and `component`, and its `$defs/modulePage` references a missing `$defs/dialog`.

See [Configuration](../architecture/configuration.md) and [ADR-0002](../adr/0002-jsonc-specifications.md).
