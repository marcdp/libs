# Service Worker

Bootstrap always installs the XShell Service Worker. It provides resource virtualization: a stable client-facing namespace for module files regardless
of where their source files reside.

The checked-in `xshell.assetsPrefix` is `_assets`, producing URLs such as `/_assets/x/components/x-button.js`. Bootstrap sends a mapping for each
canonical module definition and the XShell framework files. Each rule maps the virtual prefix to `assetsUrl` and excludes `configUrl`, keeping the
configuration document distinct from the asset namespace. The worker rewrites matching requests and fetches the physical resource. Repeated
references to a definition share one mapping; they do not create additional live module instances.

```text
client: /_assets/<module-id>/<resource>
    → Service Worker mapping
    → assetsUrl (currently an expanded directory)
    → resource response
```

`configUrl` identifies a module configuration document. `assetsUrl` identifies the physical directory or package holding its assets. Normal clients
must use `/_assets/<module-id>/...` and remain independent of whether storage is expanded or packaged. Current mapping and fetch behavior supports
expanded directories. Although the model allows a future package URL and remote/CDN locations, ZIP-backed loading is not implemented, and the
worker fetches with `mode: "same-origin"`, so cross-origin sources are not established as working.

The worker stores bootstrap's initialization payload in IndexedDB and reloads it when handling requests after its in-memory state has been lost.
After the worker is ready and acknowledges its mappings, bootstrap concurrently loads every normal module inventory from
`/_assets/<module-id>/module.files.json` and the framework inventory from `/_assets/xshell/module.files.json`. The worker only virtualizes those
requests; inventory discovery, path normalization, and attachment to the effective configuration remain bootstrap responsibilities.

Every physical inventory uses the filename `module.files.json` and package-relative paths such as `/pages/home.js`. Bootstrap exposes those entries
as virtual application resource paths such as `/_assets/x/pages/home.js` in `config.modules.x.files` or `/_assets/xshell/xshell.js` in
`config.xshell.files` before validation and XShell initialization.

See [Asset URL Namespace ADR](../adr/0001-asset-url-namespace.md) and [Modules](modules.md).
