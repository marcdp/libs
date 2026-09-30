# Packaging

XShell modules are authored as expanded directories. The `pack` command converts one authored module directory into a runtime-ready expanded or ZIP
package. It does not recursively package modules referenced by `configUrl`.

## Pack command

```text
DProjects.XShell pack --source <module-directory> --output <directory> [--zip]
```

The source must exist and contain exactly one of `module.json` or `module.jsonc`. JSONC comments and trailing commas are supported, and the authored
descriptor is copied without being rewritten. The descriptor's `modules` object must contain exactly one entry without `configUrl`; that entry's key
is the package id, independently of property order, and its non-empty `version` is the package version. All other module entries are external
references and are not fetched or packaged.

The command uses one staging and compilation flow for both output modes:

1. copy the complete source module to a temporary staging directory outside the source and output trees;
2. remove any copied `module.files.json`;
3. compile staged JavaScript, HTML XShell SFC, and standalone CSS resources through `ModuleFileCompiler`;
4. generate `module.files.json` from the final compiled files; and
5. emit the compiled staging tree as an expanded directory or, with `--zip`, an immutable ZIP.

JavaScript is replaced at its existing path with its compiled content. An authored HTML SFC such as `pages/orders.html` or
`components/x-example.html` becomes the corresponding JavaScript resource (`pages/orders.js` or `components/x-example.js`), and the source HTML is
not included. Packing fails if both the HTML and JavaScript source exist because they resolve to the same runtime path. Standalone CSS is compiled
and replaced at the same path; resources are not bundled or concatenated.

Without `--zip`, the output is `<output>/<id>-<version>/`. Its root directly contains the module descriptor, `module.files.json`, and resource
directories. A completed temporary copy replaces an older directory with the same package name so stale files are not retained.

With `--zip`, the output is `<output>/<id>-<version>-<sha256>.zip`, where the lowercase SHA-256 is computed over the ZIP bytes. The ZIP root directly
contains the module contents without an enclosing package directory. If the identical immutable target already exists, it is reused.

The command rejects an output directory that equals or is inside the source directory. Temporary staging is always cleaned after success or failure,
and resource compilation failures are not swallowed.

## Module file manifest

`ModuleFilesIndexer` records every final package file except `module.files.json` itself. Each entry contains:

```json
{
  "path": "/pages/home.js",
  "size": 1234,
  "hash": "<lowercase-sha256>"
}
```

`path` is relative to the package root, uses forward slashes, and carries a leading `/`. `size` is the file byte length. `hash` is the lowercase
SHA-256 digest of the file bytes. Entries are sorted deterministically by path using ordinal comparison. Because the manifest is generated after
compilation, an HTML SFC contributes its generated `.js` path and not its authored `.html` path.

The manifest is a physical package inventory. It is not module semantic configuration and is not automatically loaded by bootstrap or the Service
Worker.

## Development resources

`ResourcesMiddleware` uses the same `ModuleFileCompiler` on demand in development, including HTML-to-JavaScript resolution and conflict detection.
It also generates `module.files.json` on demand for module directories and adds no-cache headers to development resources. A debugger attached to the
ASP.NET host forces this development behavior even when the configured ASP.NET environment is not Development.

Packaging does not provide ZIP-backed browser loading. Current Service Worker mapping and fetch behavior supports expanded directories only.

## Future work

TODO: Add automatic publish-time packing only if explicit MSBuild/publish targets are implemented. The current project merely copies `Resources/**`
to the output directory and has no automatic `dotnet publish` packaging integration.

TODO: Add ZIP-backed Service Worker resource loading before documenting packaged ZIPs as a directly consumable runtime asset source.

See [Modules](modules.md), [Service Worker](service-worker.md), and [Configuration](configuration.md).
