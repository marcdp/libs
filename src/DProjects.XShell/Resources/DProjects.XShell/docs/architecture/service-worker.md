# Service Worker

Bootstrap always installs the XShell Service Worker. It provides resource virtualization: a stable client-facing namespace for module files regardless
of where their source files reside.

The checked-in `xshell.assetsPrefix` is `_assets`, producing URLs such as `/_assets/x/components/x-button.js`. Bootstrap sends a mapping for each
canonical module definition and the XShell framework files. The worker rewrites matching requests to the source directory and fetches the resource.
Multiple live instances of one definition share the same mapping and static files.

```text
client: /_assets/<module>/<resource>
    → Service Worker mapping
    → source directory or remote URL
    → resource response
```

Sources can be local files or remote servers/CDNs, subject to browser fetch and host policies. ZIP-backed package loading is planned and not
implemented. The worker's role is resource delivery; module creation, configuration merge, methods, intents, and navigation belong to bootstrap and
XShell runtime.

See [Asset URL Namespace ADR](../adr/0001-asset-url-namespace.md) and [Modules](modules.md).
