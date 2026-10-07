# Bootstrap

The ASP.NET host writes `xshell:` meta values and loads `xshell/bootstrap.js`. The important application input is
`xshell:app.configPath`, which points to the root module configuration document.

```text
host HTML
    -> load xshell.jsonc and root module.jsonc
    -> find the root file's one local module definition
    -> discover modules.<id>.configUrl references recursively
    -> validate reference ids and canonical URLs
    -> reject dependency cycles
    -> resolve xshell.assetsBasePath against the application base and normalize configUrl, assetsUrl, and module resource paths
    -> merge defaults, dependencies, dependents, and root
    -> generate assetsPath for every effective module and XShell
    -> install and initialize Service Worker mappings
    -> load every module.files.json through the virtual resource namespace
    -> normalize and attach module and XShell file inventories
    -> generate resolver rules, including exact contract:<id> inventory entries
    -> load and validate the XShell runtime
    -> deep-freeze the effective config and initialize XShell
```

## Root and local module detection

The root is the document selected by `app.configPath`. Within it, as in every module document, bootstrap finds the one `modules.<id>` entry without
`configUrl`. It never uses the first property. Zero or multiple local definitions are configuration errors.

The optional host `xshell:app.params` value becomes both the root definition's params and `app.params`. References in both root and non-root
documents may contribute params or other configuration to a dependency.

## Recursive discovery

For each external reference, bootstrap resolves `url:` in `configUrl` relative to its physical owner document and `app:` relative to the fixed
application base URL supplied at bootstrap. Plain module paths use the owner's logical module coordinates. A reference key is the expected
identity, so `modules.x`
must load a document whose local definition is `modules.x`. The graph rejects:

- identity mismatches;
- one module id associated with different resolved URLs;
- an already loaded URL referenced under the wrong id; and
- dependency cycles.

Pending URLs are deduplicated within every discovery pass, and the loaded-config map prevents later passes from fetching them again. Shared
dependencies therefore load once.

## Normalization and merge

Each local definition receives its configuration document URL as `configUrl`. Its `assetsUrl` defaults to the same document's directory unless the
definition owns an explicit value. Plain module-relative reference URLs use the owner's virtual asset namespace; `url:` explicitly selects the
physical source document. Bootstrap uses the physical source URL to load a referenced configuration before the Service Worker is available.

Bootstrap normalizes the configured `xshell.assetsBasePath` once. The checked-in `app:/_assets` resolves relative to the application base URL and is
stored as the application-relative virtual path `/_assets`. Bootstrap derives `assetsPath` from it: `/_assets/x` for module `x` and
`/_assets/xshell` for the framework. With an application hosted under `/myapp/`, these are served under `/myapp/_assets/...`.
`assetsPath` is generated runtime metadata; `assetsUrl` remains the separate physical backing location.

The dependency graph supplies a post-order merge sequence. Dependencies precede dependents, reference contributions merge after the referenced
local definition, sibling references retain declaration/discovery order, and the root is last. This produces deterministic root-authoritative
composition independent of network completion order. The final
`config.modules` entries are full canonical definitions rather than unresolved stubs.

## Runtime handoff

Bootstrap installs and initializes the Service Worker before requesting inventories. It concurrently requests `module.files.json` for every canonical
module through `/_assets/<module-id>/module.files.json` and for the framework through `/_assets/xshell/module.files.json`. Any missing or failed
inventory aborts bootstrap with the package id, requested URL, HTTP status, and status text.

The physical inventory remains package-relative:

```json
{ "path": "/components/x-button.js", "size": 1234, "hash": "..." }
```

Bootstrap converts only the effective-config copy into the virtual application resource namespace:

```json
{ "path": "/_assets/x/components/x-button.js", "size": 1234, "hash": "..." }
```

The same conversion produces paths such as `/_assets/xshell/xshell.js` in `config.xshell.files`. These paths follow the same absolute or
application-root-relative URL convention as the rest of the effective configuration; `size` and `hash` are unchanged. Bootstrap then loads the
XShell runtime, validates the complete enriched configuration, deeply freezes it, and calls `xshell.init(config)`. `xshell.init` validates again,
constructs runtime services, awaits i18n and Contracts initialization, registers core instances, and finalizes
configured Services. Modules checks requirements before loading any controller and then initializes one instance per canonical id.
Areas composes menus, ordered routes, and homes after module startup; Navigation starts last.

See [Configuration](20-configuration.md), [Services](100-services.md), [Modules](30-modules.md), and [Service Worker](110-service-worker.md).

## JSONC boundary

Browser bootstrap accepts JSONC comments and trailing commas in framework, root, and dependency configuration documents. Its scanner preserves
quoted content while removing comments and commas before closing object or array delimiters. Server/build descriptor parsing accepts the same dialect.

See [Hosting](40-hosting.md) for meta inputs and generated worker files.
