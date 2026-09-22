# Packaging

XShell modules are authored as expanded directories. The implemented `pack` command stages such a directory, adds a physical file inventory, and
creates an immutable ZIP package.

## Pack command

```text
DProjects.XShell pack --source <module-directory> --output <directory>
```

The source must exist and contain exactly one of `module.json` or `module.jsonc`. The descriptor must provide non-empty string `id` and `version`
properties. The command then:

1. copies the complete source directory to a temporary staging directory;
2. generates `modules.files.json` in staging;
3. ZIPs the staging contents without an enclosing base directory;
4. computes the lowercase SHA-256 hash of the ZIP bytes;
5. writes or reuses `<id>-<version>-<sha256>.zip` in the output directory; and
6. removes the temporary staging directory.

The expanded staging directory is temporary and is not a command output. If the immutable target ZIP already exists, the temporary ZIP is removed
and the existing path is reported.

## Module file manifest

`ModuleFilesIndexer` records every file below the module directory except a file named `modules.files.json`, case-insensitively. Each entry contains:

```json
{
  "path": "/pages/home.js",
  "size": 1234,
  "hash": "<lowercase-sha256>"
}
```

`path` is relative to the inventory root, uses forward slashes, and carries a leading `/`. `size` is the file byte length. `hash` is the lowercase
SHA-256 digest of the file bytes. Entries are sorted deterministically by path using ordinal comparison. The manifest inventories the physical
package contents; it is not module semantic configuration and is not automatically loaded by bootstrap or the Service Worker.

## Development resources

`ResourcesMiddleware` serves default files and static files from the expanded XShell resource tree. In development it also generates an inventory
on demand only when the containing directory has `module.json` or `module.jsonc`, and it adds no-cache headers to development resources. A debugger
attached to the ASP.NET host forces this development behavior even when the configured ASP.NET environment is not Development.

There is a current filename inconsistency: development exposes the virtual name `module.files.json`, while `pack` writes `modules.files.json`, and the
indexer excludes only `modules.files.json`. Treat both as implementations of the same module file manifest concept, not as a finalized universal
filename.

## Future work

TODO: Unify the manifest filename before consumers depend on it.

TODO: Align package identity with the configuration model. `pack` currently requires top-level `id` and `version`, while checked-in module
descriptors key identity under `modules` and place `version` inside the effective module entry.

TODO: Add automatic publish-time packing only if explicit MSBuild/publish targets are implemented. The current project merely copies `Resources/**`
to the output directory and has no automatic `dotnet publish` packaging integration.

TODO: Add ZIP-backed Service Worker resource loading before documenting packaged ZIPs as a directly consumable runtime asset source.

See [Modules](modules.md), [Service Worker](service-worker.md), and [Configuration](configuration.md).
