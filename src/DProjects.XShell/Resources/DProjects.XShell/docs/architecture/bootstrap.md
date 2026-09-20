# Bootstrap

The host HTML supplies `xshell.app_config_url` (the root module JSONC URL), `xshell.app_base_url`, and `xshell.sw_url`, then loads
`xshell/bootstrap.js`. The historical meta name `app_config_url` points to `module.jsonc`, not a separate application model. The optional
`xshell.app_params` meta value supplies URL query parameters for the root module.

```html
<meta name="xshell.app_base_url" content="">
<meta name="xshell.app_config_url" content="/_resources/DProjects.XShell/modules/test/module.jsonc">
<meta name="xshell.sw_url" content="/sw.js">
<script src="/_resources/DProjects.XShell/xshell/bootstrap.js"></script>
```

The host exposes framework and module files under `/_resources/DProjects.XShell/...`. The example points to the checked-in nested `test` root module;
the server command's current default still points to the legacy `samples/sample1/app.jsonc`.

## Phase 1: configuration

```text
host HTML → bootstrap.js → load xshell.jsonc and root module.jsonc concurrently
    → identify root module id → discover imports recursively
    → load available imported JSONC files concurrently → deduplicate definitions by URL
    → retain params from first registered import → normalize URLs and resource paths
    → merge into app, modules, xshell → generate default resolvers
```

Bootstrap takes the first key in the root file's `modules` object as the root id and stores it in `xshell.module.root`. It assigns parsed
`xshell.app_params` to the root definition's `params`. The default `xshell.module.rootParams` remains unpopulated by bootstrap. A module does not
need a `root` flag. Registration follows discovery order; the first registered occurrence of a URL keeps its params, even if later imports differ.

The intended merge precedence is defaults, dependencies, their importers, then root. Bootstrap actually merges framework defaults followed by
definitions in **reverse registration order**. This is not a topological sort and does not guarantee dependency-first precedence for every graph.
URL deduplication prevents repeated fetching, but cycles are not diagnosed. There is no JSON Schema validation step.

Bootstrap recursively calls `relativizePaths()` on each configuration fragment before merging. Authored module menu hrefs such as
`/pages/report.js` and Area homes such as `/pages/home.js` become `/_assets/<module-id>/pages/...` paths. Menus subsequently adds the Area
prefix to those normalized menu hrefs; it does not prepend `module.path` again. The same generic normalization also treats an Area's
leading-slash `prefix` as a module-relative resource, although the prefix is navigation metadata. A configured `/customers` in the root
fragment becomes `/_assets/<root-module-id>/customers`. This is a [current limit](../subsystems/areas.md#current-implementation-limits).

## Phase 2: runtime resources

Bootstrap installs and initializes the Service Worker, creates an import map from `xshell.resolver.import`, imports `xshell.js`, and calls
`xshell.init(deepFreeze(config))`. The effective configuration is deeply frozen before XShell receives it, although the import occurs first.
An uncontrolled first page is reloaded. `Modules.init()` schedules module
scripts and styles as concurrent load tasks. This is separate from JSONC discovery.

## Phase 3: startup

Runtime creates one module record per `config.modules` entry. Once resource tasks finish, it calls controller `start()` methods in parallel,
attaches loaded styles, then starts navigation and composes Menus per Area. Area participation does not create another module record.
A script-free module currently receives a fallback controller without `start()`, so
that startup path can fail.

See [Configuration](configuration.md), [Modules](modules.md), and [Service Worker](service-worker.md).
