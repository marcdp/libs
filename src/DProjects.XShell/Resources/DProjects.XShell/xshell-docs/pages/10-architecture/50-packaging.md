# Packaging

`pack` builds a complete application distribution from its root module. It follows every `modules.<id>.configUrl` reference recursively, packages
each canonical module once, and adds the XShell framework as its own immutable ZIP package. Recursion and ZIP output are mandatory.

## Command

```text
DProjects.XShell pack --source ./app --output ./dist
```

`--source` is the root application module directory. It contains `module.json` or `module.jsonc` with exactly one local module definition.
`--output` is the distribution directory. The command rejects an output directory containing the source or contained by the source.
Application packs automatically use the XShell resources shipped beside the running DProjects.XShell assembly. No `--xshell` argument exists, and
applications do not declare a framework filesystem path or a `modules.xshell` reference.
The former `--zip` flag and expanded output mode have been removed.

For compatibility with framework packaging, a source containing `xshell.json` or `xshell.jsonc` can still produce a standalone XShell framework
ZIP distribution. It has no module dependency graph. A source cannot contain both descriptor kinds.

## Discovery and build

The command parses the full graph before changing published output. Each reference key must equal the local module id in the referenced document.
References are resolved relative to the document that contains them. Repeated references to one canonical document are deduplicated. Missing
documents, id conflicts, cycles, invalid ids, and references without `configUrl` fail the build.

Only local filesystem source definitions are supported. A `url:../other/module.jsonc` reference and a `file:` URL resolve to local files.
Remote HTTP(S), `app:`, and deployment-root references fail clearly because the packer has no authenticated remote package fetch and integrity
verification mechanism. It does not recompile remote immutable packages or omit unsupported dependencies.

Modules are built in topological order, with dependencies before consumers. The framework is built separately from the bundled resources and is not
merged into the application module. Each module package retains its own local definition and its own dependency
references and reference contributions. The packer changes only each dependency's `configUrl` to the definitive packaged descriptor, such as
`url:../../customers/1.2.0.<hash>/module.json`. The relative URL works regardless of where the distribution is deployed. Original source
descriptors are never edited.

The command copies source files into private staging, normalizes JSONC descriptors to strict `module.json`, compiles JavaScript, HTML XShell SFC,
and standalone CSS through `ModuleFileCompiler`, then generates `module.files.json`. An HTML SFC becomes a JavaScript file; conflicting authored
HTML and JavaScript runtime paths fail. Framework resources remain uncompiled under the existing framework convention.

Each canonical archive contains the compiled tree, `module.files.json`, and a descriptor whose own generated `hash` is empty. The external
descriptor beside the ZIP has the calculated hash, `assetsUrl = "url:./module.zip"` (or `xshell.zip`), the physical file inventory, and `builtAt`.
Both descriptors contain the same rewritten dependency references. The archive is created before external publication metadata is added. Any
authored `builtAt` is removed from canonical module or framework definitions before hashing.

The package hash is the first 16 lowercase hexadecimal characters of SHA-256 over sorted canonical relative paths, lengths, and file bytes.
It includes the canonical descriptor, inventory, and final rewritten references. It excludes ZIP timestamps and external publication metadata.
ZIP entries are written in sorted order with a fixed timestamp, so the archive bytes are also reproducible for the same canonical tree.
Changing a dependency identity can change its consumers' hashes. Unchanged packages keep their identities and are reused after the existing
archive and external descriptor pass integrity checks. Existing immutable packages are never overwritten.

| Property | Location | Meaning |
| --- | --- | --- |
| `version` | Module or framework descriptor | Authored semantic version. |
| `hash` | Published descriptor | Immutable canonical content identity. |
| `builtAt` | Published module or framework descriptor | UTC time when that immutable package was originally built. |
| `createdAt` | `release.json` | UTC time when the application distribution was committed. |

`pack` generates `builtAt` only in the external published local definition, never in the canonical descriptor inside the ZIP. It is excluded from
package hashing and retained when an existing package is reused. Identical content therefore keeps the same package identity across builds. A
framework package uses `xshell.builtAt` in its published `xshell.json` under the same rules. Both timestamps use `yyyy-MM-ddTHH:mm:ssZ`.
Packages published before `builtAt` was introduced remain reusable without it; their immutable descriptors are not retroactively rewritten.

For example, a published `customers/1.2.0.a83f92c1d4567890/module.json` may contain:

```json
{
  "modules": {
    "customers": {
      "version": "1.2.0",
      "hash": "a83f92c1d4567890",
      "builtAt": "2026-10-11T15:30:00Z",
      "assetsUrl": "url:./module.zip"
    }
  }
}
```

The actual published descriptor also retains its other configuration and file inventory fields.

## Distribution and transaction boundary

A committed output has this structure:

```text
dist/
  release.json
  xshell/0.9.0.<hash>/xshell.json
  xshell/0.9.0.<hash>/bootstrap.js
  xshell/0.9.0.<hash>/sw.js
  xshell/0.9.0.<hash>/xshell.zip
  app/1.0.0.<hash>/module.json
  app/1.0.0.<hash>/module.zip
  orders/2.0.0.<hash>/module.json
  orders/2.0.0.<hash>/module.zip
  customers/1.2.0.<hash>/module.json
  customers/1.2.0.<hash>/module.zip
```

`xshell.zip` remains the complete canonical framework package, including `xshell.json`, `bootstrap.js`, and `sw.js`. The latter two files are also
published beside the ZIP as exact byte copies of their canonical versions because bootstrap and Service Worker startup must address them directly.
The loose copies introduce no separate package identity or inventory entries: the canonical tree determines the one XShell hash. `release.xshell`
identifies this four-file package through its published `xshell.json` descriptor.

`release.json` is the authoritative commit record. Its `root` field identifies the application module; `xshell` identifies the exact framework
descriptor; and its package-id-sorted `packages` array lists the complete immutable package set. A standalone XShell pack sets `root` and `xshell`
to the same descriptor and lists it once. `createdAt` records the successful distribution commit, independently of package
build times. A new release may reuse packages built months earlier. For example:

```json
{
  "root": "app/1.0.0.1234567890abcdef/module.json",
  "xshell": "xshell/0.9.0.0123456789abcdef/xshell.json",
  "createdAt": "2026-10-11T15:35:00Z",
  "packages": [
    "app/1.0.0.1234567890abcdef/module.json",
    "customers/1.2.0.a83f92c1d4567890/module.json",
    "xshell/0.9.0.0123456789abcdef/xshell.json"
  ]
}
```

Consumers should read `release.json` to select a distribution, then resolve
its paths within the same output directory. Package directories outside the manifest are uncommitted and may be left by an interrupted publish.

The build has three phases: full graph discovery and validation, private staging of all ZIP packages, then publication. An exclusive
`.pack.lock` file serializes concurrent commits and is removed as the final publication operation while its exclusive handle is still held.
The command moves immutable packages to their final paths, verifies any existing identities, writes a complete temporary release manifest, then
atomically installs the manifest. A failed build or publication before that final operation leaves the previous `release.json` selecting the
previous complete release. Temporary staging and manifest files are removed after ordinary failures. Previously published and newly moved
immutable packages are left intact.

The atomic boundary is one file rename or replacement on the output filesystem. This requires a filesystem with atomic same-volume file rename
and replacement semantics and readers that use `release.json` as the release selector. It does not rely on atomic directory replacement across
filesystems. The staging directory is created beside `--output` to keep package moves on the same filesystem.

## Module file inventory

`FilesIndexer` records compiled runtime resources, excluding `module.files.json`, `module.json`, and `xshell.json`. Each entry contains a
package-relative path, byte size, and lowercase SHA-256 hash. Paths start with `/`, use forward slashes, and are sorted ordinally.
Bootstrap loads this physical inventory through the virtual resource namespace after Service Worker initialization.

Packaging creates distribution artifacts; deployment and browser runtime loading are separate concerns. The current Service Worker supports
expanded physical resources and does not load ZIP archives directly. The packer does not upload artifacts or implement deployment.

See [Modules](30-modules.md), [Service Worker](110-service-worker.md), and [Hosting](40-hosting.md).
