# Specifications

XShell authors configuration as JSONC. The root module uses the same `module.jsonc` composition model as imported modules; there is no separate
application file format. See [Root Module](application.md) for the application-specific `app` section and [Module](module.md) for definitions,
imports, params, and the proposed public contract.
The root also composes [Areas](../subsystems/areas.md) from module ids. Module menu contributions remain independent of that placement.

The main validation boundary is the single nested effective configuration after loading, URL normalization, and merging. JSON Schema validation in
development is intended but not implemented. No formal schema or `schemes` directory exists yet.

See [Configuration](../architecture/configuration.md) and [ADR-0002](../adr/0002-jsonc-specifications.md).
