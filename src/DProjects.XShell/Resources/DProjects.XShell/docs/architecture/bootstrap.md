# Bootstrap

The ASP.NET host writes four configuration values as HTML meta elements, then loads `xshell/bootstrap.js`. `xshell:` is the HTML meta namespace;
the remainder of each name identifies the corresponding effective-configuration concept.

```html
<meta name="xshell:app.basePath" content="">
<meta name="xshell:app.configPath" content="/_resources/DProjects.XShell/modules/test/module.jsonc">
<meta name="xshell:app.params" content="mode=compact">
<meta name="xshell:xshell.environment" content="Development">
<script src="/_resources/DProjects.XShell/xshell/bootstrap.js"></script>
```

`app.configPath` identifies the root module JSONC. Bootstrap constructs `app.basePath` as `document.location.origin` plus the value of
`xshell:app.basePath`. It parses `xshell:app.params` as a query string. The host writes `xshell:xshell.environment` as `development` when the ASP.NET
environment is development or a debugger is attached, and as `production` otherwise; bootstrap copies it to `xshell.environment`. No runtime
behavior is currently gated by that value.

There is no Service Worker URL meta setting. Bootstrap registers `appBasePath + "/sw.js"`, and the ASP.NET host maps that URL to the framework worker.
The host exposes framework and module files under `/_resources/DProjects.XShell/...`. With no explicit application config, the server command uses
`/_resources/DProjects.XShell/modules/test/module.jsonc` (prefixed by the configured resource base).

## Phase 1: configuration

```text
host HTML → bootstrap.js → load xshell.jsonc and root module.jsonc concurrently
    → identify root module id → discover imports recursively
    → load available imported JSONC files concurrently → deduplicate definitions by URL
    → retain params from first registered import → normalize URLs and resource paths
    → merge into app, modules, xshell → generate default resolvers
```

Bootstrap treats the first key in the root file's `modules` object as the root module id. It assigns parsed `xshell:app.params` to that module's
`params` and to `app.params`. It does not persist a separate root id or root-params object in the effective configuration; there is no
`xshell.module` section. A module does not need a `root` flag. Registration follows discovery order; the first registered occurrence of a URL keeps
its params, even if later imports differ.

The intended merge precedence is defaults, dependencies, their importers, then root. Bootstrap actually merges framework defaults followed by
definitions in **reverse registration order**. This is not a topological sort and does not guarantee dependency-first precedence for every graph.
URL deduplication prevents repeated fetching, but cycles are not diagnosed. Bootstrap does not validate against JSON Schema. Later, the checked-in
X module controller fetches `/_assets/xshell/schemes/config.scheme.json` during `start()`, validates the effective configuration, and writes failures
to `console.error`. That validation is not conditional on `xshell.environment`.

Bootstrap recursively normalizes module resource paths in each configuration fragment before merging. Authored module menu hrefs such as
`/pages/report.js` become `/_assets/<module-id>/pages/report.js`. Area prefixes are preserved because they are navigation metadata. Areas
subsequently adds the Area prefix to the already-normalized menu href; it does not prepend `module.path` again.

## Phase 2: runtime resources

Bootstrap always installs and initializes the Service Worker, creates an import map from `xshell.resolver.import`, imports `xshell.js`, and calls
`xshell.init(deepFreeze(config))`. The effective configuration is deeply frozen before XShell receives it, although the import occurs first.
An uncontrolled first page is reloaded. `Modules.init()` schedules module controllers and styles as concurrent load tasks. This is separate from
JSONC discovery.

## Phase 3: startup

Runtime creates one module record per `config.modules` entry. Once resource tasks finish, it calls controller `start()` methods in parallel,
attaches loaded styles, then Areas composes menus and homes before Navigation starts. Area participation does not create another module record.
A module without a configured controller currently receives a fallback object containing only `onCommand()`, so startup can fail when it calls the
missing `start()` method. `Modules.stop()` calls controller `stop()` methods, but no automatic application-shutdown path invokes it.

See [Configuration](configuration.md), [Modules](modules.md), and [Service Worker](service-worker.md).
