# Service Worker

Bootstrap always installs the XShell Service Worker. It provides resource virtualization: a stable client-facing namespace for module files regardless
of where their source files reside.

The checked-in `xshell.assetsPrefix` is `_assets`, producing URLs such as `/_assets/x/components/x-button.js`. Bootstrap sends a mapping for each
canonical module definition and the XShell framework files. The worker rewrites matching requests to the source directory and fetches the resource.
Repeated imports of a definition share one mapping; they do not create additional live module instances.

```text
client: /_assets/<module-id>/<resource>
    → Service Worker mapping
    → source directory or remote URL
    → resource response
```

The namespace is intended to support local files, remote hosts/CDNs, and future ZIP-backed packages. Current worker code rewrites to the mapped
source URL but fetches with `mode: "same-origin"`; cross-origin sources are therefore not established as working. ZIP-backed package loading is not
implemented. The worker stores bootstrap's initialization payload in IndexedDB and reloads it when handling requests after its in-memory state has
been lost. The worker's role is resource delivery; module creation and configuration merge belong to bootstrap and XShell runtime.

See [Asset URL Namespace ADR](../adr/0001-asset-url-namespace.md) and [Modules](modules.md).
