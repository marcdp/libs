# Service Worker

Bootstrap always installs the XShell Service Worker. It provides resource virtualization: a stable client-facing namespace for module files regardless
of where their source files reside.

The checked-in value is normalized as follows:

```text
authored:
    xshell.assetsBasePath = "app:/_assets"

effective:
    xshell.assetsBasePath = "/_assets"
```

Bootstrap normalizes the authored value against the application base URL, producing effective application Paths such as
`/_assets/x/1.4.0.dev/components/x-button.js` (served under `/myapp/_assets/...` when the application base is `/myapp/`). It sends a mapping for each
canonical module definition and the XShell framework files. Each rule maps the virtual prefix to `assetsUrl` and carries `configUrl` in an
`exceptions` field. The current worker does not apply that field;
configuration discovery still fetches the original document URL before worker setup. The worker rewrites matching requests and fetches the physical
resource. Repeated
references to a definition share one mapping; they do not create additional live module instances.

```text
client: /_assets/<module-id>/<generation>/<resource>
    → Service Worker mapping
    → assetsUrl (currently an expanded directory)
    → resource response
```

`configUrl` identifies a module configuration document. `assetsUrl` identifies the physical directory or package holding its assets. Normal clients
must use `/_assets/<module-id>/<generation>/...` and remain independent of whether storage is expanded or packaged. Current mapping and fetch
behavior supports
expanded directories. Runtime ZIP loading is not implemented. The mapped fetch has no explicit `mode` option; the source's `same-origin` option
is commented out. Cross-origin destinations remain subject to fetch/CORS and the host CSP, whose default connect policy is same-origin.
Arbitrary CDN backing is not a supported deployment guarantee.

The worker owns one application-scope mapping registry and persists it in IndexedDB. Bootstrap sends `registerMappings`; the worker durably merges
the complete registration batch before replying with `registered`. After acknowledgement, bootstrap concurrently loads every normal module
inventory from
`/_assets/<module-id>/<generation>/module.files.json` and the framework inventory from `/_assets/xshell/<generation>/module.files.json`. The worker
only virtualizes those
requests; inventory discovery, path normalization, and attachment to the effective configuration remain bootstrap responsibilities.

Every physical inventory uses the filename `module.files.json` and package-relative paths such as `/pages/home.js`. Bootstrap exposes those entries
as virtual application resource paths such as `/_assets/x/1.4.0.dev/pages/home.js` in `config.modules.x.files` or
`/_assets/xshell/0.9.0.dev/xshell.js` in
`config.xshell.files` before validation and XShell initialization.

See [Asset URL Namespace ADR](../adr/0001-asset-url-namespace.md) and [Modules](30-modules.md).

## Immutable mapping registry

Mapping registration is additive. A canonical `src` is the mapping identity, and its `src → dst` relationship is immutable. Registering the same
`src` and `dst` is an idempotent no-op that retains the original diagnostic metadata. Registering an existing `src` with another `dst` rejects the
whole batch without changing persisted or in-memory state. Registration mutations are serialized so concurrent tabs cannot lose one another's
additions.

Mappings absent from a later registration remain stored. There is no per-tab mapping state, active generation, expiration, reference count, or
automatic garbage collection in V0. Old generations accumulate because the worker cannot determine whether a suspended or restored tab still
needs them. IndexedDB restores the complete registry after a worker restart.

For example, an old tab may continue loading `/_assets/orders/1.4.0.aaa/...` after a new tab registers
`/_assets/orders/1.5.0.bbb/...`; both mappings coexist and each URL continues resolving to its own physical destination.

## Development generations

Development generation identity is `<version>.dev`, for example `/_assets/orders/1.4.0.dev/...`. It does not require a package content hash and is
not a content-addressed or immutable generation. Published generations use `<version>.<hash>` and have immutable contents; development differs
only in the mutability of its contents, not in the Service Worker mapping rules.

A development mapping is immutable once registered, but the files behind its physical development destination are mutable. For example,
`/_assets/orders/1.4.0.dev` may stably map to `/_resources/.../orders/` while a developer changes files in that directory. The mapping remains the
same `src → dst` relationship: registering the same source and destination is idempotent, and registering the same source with another destination
is an immutable mapping conflict. Development mappings are never remapped to reflect source edits.

Tabs using the same module id and version intentionally share that one live development mapping. Two tabs loading
`/_assets/orders/1.4.0.dev/...` therefore use the same mutable source location; the first tab is not promised the bytes that existed when it loaded.
Changing the module version produces a different virtual mapping. Thus `/_assets/orders/1.4.0.dev/...` and
`/_assets/orders/1.5.0.dev/...` may coexist in the application-scope registry. If both map to the same physical development directory, both can
serve that directory's current files. V0 does not snapshot source trees, replace or remove old development mappings, or keep per-tab, client,
window, or session mapping state. Garbage collection remains outside V0.

The same contract applies to framework assets: `/_assets/xshell/0.9.0.dev/...` is a stable mapping to mutable development resources, and
`/_assets/xshell/0.10.0.dev/...` may coexist after an XShell version change.

Development does not provide Hot Module Replacement (HMR) or automatically replace already-imported ES modules, initialized application state,
effective configuration, or a loaded file inventory. Source edits can affect future network requests, but reload is the normal V0 refresh boundary:

```text
edit source
    ↓
reload page
    ↓
bootstrap again
    ↓
load current development configuration, inventories, and resources
```

A development tab uses the effective configuration and file inventory loaded during its bootstrap. Changes on disk do not mutate that already
loaded configuration or inventory; after reload, bootstrap can load the current development inventory again.

For requests matching multiple persisted rules, the worker selects the longest `src` path whose origin matches and whose path matches at a complete
namespace boundary. The worker preserves the incoming query, method, and headers, fetches the rewritten destination, and removes
Location/Content-Location from the returned response.

IndexedDB stores mappings, not a resource cache. Rule version/name/exceptions and inventory hashes are not used for cache versioning, exclusion,
or fetched-byte verification. This does not provide offline packaging.

See [Hosting](40-hosting.md) and [Packaging](50-packaging.md).
