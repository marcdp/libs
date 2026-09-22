# Service Worker

Bootstrap always installs the XShell Service Worker. It provides resource virtualization: a stable client-facing namespace for module files regardless
of where their source files reside.

The checked-in `xshell.assetsPrefix` is `_assets`, producing URLs such as `/_assets/x/components/x-button.js`. Bootstrap sends a mapping for each
canonical module definition and the XShell framework files. Each rule maps the virtual prefix to `assetsUrl` and excludes `configUrl`, keeping the
configuration document distinct from the asset namespace. The worker rewrites matching requests and fetches the physical resource. Repeated imports
of a definition share one mapping; they do not create additional live module instances.

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

The worker stores bootstrap's initialization payload in IndexedDB and reloads it when handling requests after its in-memory state has been lost. It
does not load `module.files.json` or `modules.files.json` during startup. A module file manifest is a physical inventory to be loaded on demand by
future mechanisms that need package information. The worker's role is resource delivery; module creation and configuration merge belong to
bootstrap and XShell runtime.

See [Asset URL Namespace ADR](../adr/0001-asset-url-namespace.md) and [Modules](modules.md).
