# Packaging

XShell modules and the XShell framework are authored as expanded directories. The `pack` command converts one source directory into a runtime-ready
expanded or ZIP package. It does not recursively package modules referenced by `configUrl`.

## Pack command

```text
DProjects.XShell pack --source <package-directory> --output <directory> [--zip]
```

The source must resolve to exactly one package kind. A normal module contains exactly one of `module.json` or `module.jsonc`; its `modules` object
must contain exactly one entry without `configUrl`, whose key and non-empty `version` supply the package identity. The XShell framework contains
exactly one of `xshell.json` or `xshell.jsonc`; its id is `xshell` and its version comes from the required non-empty `xshell.version`. A source with
both JSON and JSONC variants, both module and XShell descriptors, or no supported descriptor is rejected. JSONC comments and trailing commas are
supported, and descriptors are copied without rewriting.

The command uses one staging and compilation flow for both output modes:

1. copy the complete source package to a temporary staging directory outside the source and output trees;
2. remove any copied `module.files.json`;
3. for a normal module, compile staged JavaScript, HTML XShell SFC, and standalone CSS resources through `ModuleFileCompiler`; XShell framework files
   remain unchanged because the module compiler depends on module-definition semantics;
4. generate `module.files.json` from the final compiled files; and
5. emit the compiled staging tree as an expanded directory or, with `--zip`, an immutable ZIP.

JavaScript is replaced at its existing path with its compiled content. An authored HTML SFC such as `pages/orders.html` or
`components/x-example.html` becomes the corresponding JavaScript resource (`pages/orders.js` or `components/x-example.js`), and the source HTML is
not included. Packing fails if both the HTML and JavaScript source exist because they resolve to the same runtime path. Standalone CSS is compiled
and replaced at the same path; resources are not bundled or concatenated.

Without `--zip`, the output is `<output>/<id>-<version>/`, including `xshell-<version>/` for the framework. Its root directly contains the package
descriptor, `module.files.json`, and resource directories. A completed temporary copy replaces an older directory with the same package name so stale
files are not retained.

With `--zip`, the output is `<output>/<id>-<version>-<sha256>.zip`, including `xshell-<version>-<sha256>.zip` for the framework, where the lowercase
SHA-256 is computed over the ZIP bytes. The ZIP root directly contains the package contents without an enclosing directory. If the identical
immutable target already exists, it is reused.

The command rejects an output directory that equals or is inside the source directory. Temporary staging is always cleaned after success or failure,
and resource compilation failures are not swallowed.

## Module file inventory

`FilesIndexer` records every final package file except `module.files.json` itself. Both normal modules and XShell use this one inventory filename.
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
`/components/x-button.js` in module `x` becomes `/_assets/x/components/x-button.js`; `/xshell.js` becomes `/_assets/xshell/xshell.js`. The Service
Worker virtualizes delivery but does not discover or interpret inventories.

## Development resources

`ResourcesMiddleware` uses the same `ModuleFileCompiler` on demand in development, including HTML-to-JavaScript resolution and conflict detection.
It also generates `module.files.json` on demand and adds no-cache headers when ASP.NET is Development. Debugger presence does not enable it.
Development inventories enumerate physical sources, so HTML SFC entries retain authored `.html` paths rather than packed `.js` paths.
Server descriptor parsing accepts trailing commas, but browser bootstrap does not; copied runtime descriptors must remain browser-parseable.

Packaging does not provide ZIP-backed browser loading. Current Service Worker mapping and fetch behavior supports expanded directories only.

## Outside V0

The project copies `Resources/**` to output without automatic MSBuild/publish-time packing.
ZIPs are distribution artifacts; extract them before using an expanded-directory worker mapping. Runtime ZIP loading is not implemented.

See [Modules](modules.md), [Service Worker](service-worker.md), and [Configuration](configuration.md).

See [Hosting](hosting.md) for the development environment and resource middleware.
