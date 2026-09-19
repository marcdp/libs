# Bootstrap

The host HTML supplies `xshell.app_config_url` (the root module JSONC URL), `xshell.app_base_url`, and `xshell.sw_url`, then loads
`xshell/bootstrap.js`. The meta name `app_config_url` is retained in the host API; it does not imply a separate application configuration model.

```html
<meta name="xshell.app_base_url" content="">
<meta name="xshell.app_config_url" content="/_resources/DProjects.XShell/modules/test/module.jsonc">
<meta name="xshell.sw_url" content="/sw.js">
<script src="/_resources/DProjects.XShell/xshell/bootstrap.js"></script>
```

The host exposes framework and module files under `/_resources/DProjects.XShell/...`. The example points to the checked-in nested `test` root module;
the server command's current default still points to the legacy `samples/sample1/app.jsonc`.

## Preparation and execution

```text
bootstrap: load xshell.jsonc defaults and root module.jsonc
    → discover imported module.jsonc files recursively
    → normalize URLs and prepare /_assets mappings
    → merge dependency contributions before importers, root last
    → validate final effective configuration in development (planned)
    → deeply freeze effective configuration (planned)
    → install/initialize Service Worker and create import map
    → import xshell.js
runtime: initialize services → create live module instances → navigation → menus
```

The current bootstrap loads framework defaults and the root concurrently, discovers imports by URL, normalizes paths, merges nested objects, installs
the Service Worker, creates an import map from `xshell.resolver.import`, and calls `xshell.init(config)`. It registers a URL only once, so repeated
imports fetch one definition. The Service Worker is required before XShell starts; an uncontrolled first page is reloaded.

**Current gaps:** merge iteration puts the root before its dependencies, so importer precedence is not guaranteed. No cycle error or dependency-order
traversal exists. The bootstrap does not validate against JSON Schema or freeze the config. `Config` later freezes only its top-level copy. Runtime
services still expect dotted keys, so the nested bootstrap output is not yet fully consumable.

See [Configuration](configuration.md), [Modules](modules.md), and [Service Worker](service-worker.md).
