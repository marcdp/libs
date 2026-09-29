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
    -> normalize configUrl, assetsUrl, and module resource paths
    -> merge defaults, dependencies, dependents, and root
    -> add default resolvers and Service Worker mappings
    -> deep-freeze the effective config and initialize XShell
```

## Root and local module detection

The root is the document selected by `app.configPath`. Within it, as in every module document, bootstrap finds the one `modules.<id>` entry without
`configUrl`. It never uses the first property. Zero or multiple local definitions are configuration errors.

The optional host `xshell:app.params` value becomes both the root definition's params and `app.params`. A root reference may also provide params for
a dependency. References in non-root documents may not do so.

## Recursive discovery

For each external reference, bootstrap resolves `configUrl` relative to its owner document. A reference key is the expected identity, so `modules.x`
must load a document whose local definition is `modules.x`. The graph rejects:

- identity mismatches;
- one module id associated with different resolved URLs;
- an already loaded URL referenced under the wrong id; and
- dependency cycles.

Pending URLs are deduplicated within every discovery pass, and the loaded-config map prevents later passes from fetching them again. Shared
dependencies therefore load once.

## Normalization and merge

Each local definition receives its configuration document URL as `configUrl`. Its `assetsUrl` defaults to the same document's directory unless the
definition owns an explicit value. External reference URLs are resolved in the referencing document; they are not asset namespace URLs.

The dependency graph supplies a post-order merge sequence. Dependencies precede dependents, sibling references are ordered by id, and the root is
last. This produces deterministic root-authoritative composition independent of network completion order. The final `config.modules` entries are
full canonical definitions rather than unresolved stubs.

## Runtime handoff

Bootstrap installs the Service Worker, creates one mapping per effective module id, loads `xshell.js`, deep-freezes the effective configuration,
and calls `xshell.init(config)`. `xshell.modules` then creates one runtime module instance per canonical id.

See [Configuration](configuration.md), [Modules](modules.md), and [Service Worker](service-worker.md).
