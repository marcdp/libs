# Specifications

XShell authors configuration as JSONC. The root module uses the same `module.jsonc` composition model as dependency modules; there is no separate
application file format. See [Root Module](application.md) for the application-specific `app` section and [Module](module.md) for local definitions,
external reference contributions, asset locations, and the declarative public contract.
The root also composes [Areas](../subsystems/10-areas.md) from module ids. Module menu contributions remain independent of that placement.

The validation boundary is the complete effective configuration after discovery, normalization, merge, worker mapping, inventory loading, and
resolver generation. Bootstrap imports XShell and awaits `xshell.validateConfig(config)` against `xshell/schemas/config.schema.json` before deep
freeze and `xshell.init(config)`. Init validates again before runtime service construction. Failure aborts startup in every environment.
The X module has no module controller; XShell validates configuration during bootstrap and initialization.

The schema defines `app`, `modules`, and `xshell`, including module `configUrl`, `assetsUrl`, defaults, menus, optional service `requires`, optional
declarative routes, descriptive module `contract.events`, named `xshell.services`, and the required `xshell.i18n` language, formatting,
and translation configuration.

Every resolved module requires **module defaults** at `modules.<id>.defaults`: both `page` and `component` require non-empty render-engine and
state-engine names. These select how the owning module's definition-based resources execute after a resource `meta` override. The separate
**XShell defaults** object, `xshell.ui`, requires `layout`, `component`, and `dialog` groups for global UI infrastructure: layout contexts,
lazy/error/Markdown components, and standard dialog pages. It does not provide render or state engines for modules.

See [Configuration](../architecture/20-configuration.md) and [ADR-0002](../adr/0002-jsonc-specifications.md).

## Public contract schemas

[Component Contract](../components/10-manifest.md) describes `component.contract.schema.json`.
[Services](../architecture/100-services.md) describes the distinct `contract.schema.json` service contract.
[Component Architecture](../architecture/60-components.md) describes the default implementation checked by `component.schema.json`.
