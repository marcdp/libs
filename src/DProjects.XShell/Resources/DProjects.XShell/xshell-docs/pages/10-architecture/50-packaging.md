# Packaging

Normal modules and the XShell framework are authored as expanded directories. The `pack` command publishes expanded or ZIP distribution packages
for either kind. It does not recursively package modules referenced by `configUrl`.

## Pack command

```text
DProjects.XShell pack --source <package-directory> --output <directory> [--zip]
```

The source must resolve to exactly one package kind. A normal module contains exactly one of `module.json` or `module.jsonc`; its `modules` object
must contain exactly one entry without `configUrl`, whose key and non-empty `version` supply the package identity. The XShell framework contains
exactly one of `xshell.json` or `xshell.jsonc`; its id is `xshell` and its version comes from the required non-empty `xshell.version`. 

Module and XShell descriptors may be authored as JSON or JSONC during development. Packaging parses either form and publishes normalized strict JSON
using the canonical filenames `module.json` and `xshell.json`. Production packages therefore do not contain `module.jsonc` or `xshell.jsonc`.
Comments, trailing commas, and original formatting are not preserved.

The command uses one staging and compilation flow for both output modes:

1. copy the complete source package to a temporary staging directory outside the source and output trees;
2. normalize the canonical descriptor's own generated `hash` to `""`, overwriting any authored value;
3. remove any copied `module.files.json`;
4. for a normal module, compile staged JavaScript, HTML XShell SFC, and standalone CSS resources through `ModuleFileCompiler`; XShell framework files
   remain unchanged because the module compiler depends on module-definition semantics;
5. generate `module.files.json` from the final compiled files;
6. calculate the package hash over the complete canonical staging tree; and
7. publish the staged tree as an expanded directory or, with `--zip`, a ZIP distribution package.

JavaScript is replaced at its existing path with its compiled content. An authored HTML SFC such as `pages/orders.html` or
`components/x-example.html` becomes the corresponding JavaScript resource (`pages/orders.js` or `components/x-example.js`), and the source HTML is
not included. Packing fails if both the HTML and JavaScript source exist because they resolve to the same runtime path. Standalone CSS is compiled
and replaced at the same path; resources are not bundled or concatenated.

Both representations use `<output>/<id>/<version>.<hash>/`. The generated 16-character lowercase hash comes from SHA-256 over ordered relative file
paths and bytes in the compiled staging tree. This includes the canonical `module.json` or `xshell.json` with its own `hash` set to `""`, the generated
`module.files.json`, and every other staged file. Expanded and ZIP packages use the same algorithm; the hash is not calculated from ZIP bytes.
Development descriptors need not contain `hash`. Packaging owns it, and an authored value cannot control package identity.

Without `--zip`, the package directory contains the compiled resources, generated `module.files.json`, and canonical descriptor. Normal modules contain
`module.json`; XShell framework packages contain `xshell.json`. After calculating package identity, the published descriptor receives the actual
hash, equal to the package directory suffix. The package identity is not recalculated after this injection.

For a normal module, `--zip` publishes `module.json` and `module.zip`. The emitted `module.json` is normalized JSON with
`modules.<local-id>.assetsUrl = "url:./module.zip"` and `modules.<local-id>.files = [...]` from the generated inventory.

For the XShell framework, `--zip` publishes `xshell.json` and `xshell.zip`. The emitted `xshell.json` is normalized strict JSON, including
`xshell.assetsUrl = "url:./xshell.zip"` and `xshell.files = [...]` from the same generated inventory. Each archive contains the staged tree at its
root, including `module.files.json`. The external descriptor contains the actual package hash. The descriptor inside the ZIP retains `hash: ""`,
because the archive is created from canonical staging before the external descriptor is updated. The external descriptor also carries the existing
`assetsUrl` and `files` publication metadata.

The package path is immutable after first publication. Repeating a pack with the same content identity and representation reuses that path without
rewriting it. Requesting expanded output where a ZIP package exists, or ZIP output where an expanded package exists, fails with a representation
collision. New packages are prepared in temporary directories and published by a directory move.

The command rejects an output directory that equals or is inside the source directory. Temporary staging is always cleaned after success or failure,
and resource compilation failures are not swallowed.

## Module file inventory

`FilesIndexer` records runtime resources, excluding `module.files.json`, `module.json`, and `xshell.json`. Other JSON files remain ordinary resources.
The canonical descriptor still participates in the whole-package hash; it is excluded only from the runtime file inventory. Both normal modules and
XShell use this one inventory filename.
Each entry contains:

```json
{
  "path": "/pages/home.js",
  "size": 1234,
  "hash": "<lowercase-sha256>"
}
```

`path` is relative to the package root, uses forward slashes, and carries a leading `/`. `size` is the file byte length. `hash` is the lowercase
SHA-256 digest of the file bytes. Entries are sorted deterministically by path using ordinal comparison. Because the inventory is generated after
compilation, an HTML SFC contributes its generated `.js` path and not its authored `.html` path.

The inventory is a generated physical package inventory rather than authored semantic configuration. After Service Worker initialization, bootstrap
loads it through the virtual resource namespace and converts its package-relative paths into effective-config paths. For example,
`/components/x-button.js` in Development module `x` version `1.4.0` becomes `/_assets/x/1.4.0.dev/components/x-button.js`;
`/xshell.js` in XShell version `0.9.0` becomes `/_assets/xshell/0.9.0.dev/xshell.js`. The Service
Worker virtualizes delivery but does not discover or interpret inventories.

## Development resources

`ResourcesMiddleware` uses the same `ModuleFileCompiler` on demand in development, including HTML-to-JavaScript resolution and conflict detection.
It also generates `module.files.json` on demand and adds no-cache headers when ASP.NET is Development. Debugger presence does not enable it.
Development inventories enumerate physical sources, so HTML SFC entries retain authored `.html` paths rather than packed `.js` paths.
Server descriptor parsing and browser bootstrap both accept JSONC comments and trailing commas. Bootstrap requests the canonical `xshell.json` URL;
when the checked-in development source is `xshell.jsonc`, `ResourcesMiddleware` serves that physical source through the canonical request path.

Development resources use `<version>.dev` virtual generations. Their Service Worker mappings are immutable, but the mapped physical files remain
mutable; the mapping is not a content snapshot. Reload is the V0 boundary for a new effective configuration or inventory. See
[Service Worker](110-service-worker.md#development-generations) for the complete development-generation contract.

ZIP packaging is implemented. Runtime ZIP-backed browser loading is not implemented; current Service Worker mapping and fetch behavior supports
expanded directories only.

## Outside V0

The project copies `Resources/**` to output without automatic MSBuild/publish-time packing.
ZIPs are distribution artifacts. Runtime ZIP loading is not implemented; deploy an expanded directory for the current worker mapping.

See [Modules](30-modules.md), [Service Worker](110-service-worker.md), and [Configuration](20-configuration.md).

See [Hosting](40-hosting.md) for the development environment and resource middleware.
