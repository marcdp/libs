# Filesystem

The filesystem subsystem presents local, in-memory, archive, composed, HTTP, and S3-backed storage through one path-oriented contract. Its central
architectural choice is to share filesystem semantics while allowing providers to retain real capability and transport differences.

## Project boundaries

`DProjects.Fs.Abstractions` targets `netstandard2.0` and contains the contracts, entries, settings, capability values, comparers, and reusable
extension operations. It references `DProjects.Utils` but no concrete filesystem implementation or provider SDK.

`DProjects.Fs`, also targeting `netstandard2.0`, implements the base classes and general-purpose providers. It depends inward on the abstractions and
on shared factory, stream, crypto, and utility packages. `DProjects.Fs.Aws` contains S3 and S3-bucket repository integration and depends on the core.
`DProjects.Fs.Http` targets `net10.0` because it hosts the ASP.NET Core middleware; the HTTP client filesystem and its factory remain in the core
project and use `HttpClient` directly.

The dependency direction is therefore:

```text
DProjects.Fs.Abstractions
            ↑
      DProjects.Fs
       ↑          ↑
DProjects.Fs.Aws  DProjects.Fs.Http
```

## Contract and implementation levels

`IFilesystem` combines `IFilesystemSync` and `IFilesystemAsync`; both also expose `IFilesystemInfo` and disposal. `IFilesystemInfo` carries mutable
`IsReadonly` state and a diagnostic `Url`. The API groups operations into levels:

- level 0: entry lookup/enumeration, existence, and read/write streams;
- level 1: typed existence checks;
- level 2: save, create, delete, and touch primitives;
- level 3: typed delete, copy, move, and synchronization;
- level 4: watchers, metadata, and capability queries.

The levels are an implementation structure rather than separate public interfaces. Base classes derive higher-level behavior from lower-level
operations where possible, which centralizes recursive copy, move, synchronization, typed deletion, and stream behavior.

Three base classes express the backend's natural execution model:

- `FilesystemSync` requires synchronous entry, enumeration, and stream primitives and adapts them to the asynchronous contract.
- `FilesystemAsync` requires native asynchronous primitives and bridges synchronous calls through `AsyncUtils.RunSync`.
- `Filesystem` requires both sync and async entry/enumeration/stream primitives independently while still sharing higher-level behavior.

Local, memory, resource, XML, and ZIP implementations are sync-first. HTTP, S3, and repository-backed filesystems are async-first. Mounter and union
derive from `Filesystem` because routing across child filesystems must preserve both paths. Some decorators (`FilesystemFilter` and
`FilesystemMetadata`) implement `IFilesystem` directly and forward both surfaces; these are existing exceptions to the base-class pattern.

Cancellation tokens are meaningful on native and composed async paths. The sync-first adapters check cancellation before invoking synchronous work;
they cannot make an already-running synchronous backend operation cancellable.

## Providers and composition

Core factories expose protocols for local and operating-system paths (`file`, `os`, `os-folder`, `smb`, `temp`), memory and resources (`mem`, `res`),
archives and serialized trees (`zip`, `xml`), an injected filesystem view (`fs`), and composed views (`filter`, `metadata`, `mounter`, `union`). The
core also exposes the `http` client protocol. `DProjects.Fs.Aws` adds `s3` and `s3-buckets`.

The composition providers have distinct responsibilities:

- filter hides or rejects paths according to ordered include/exclude rules;
- metadata overlays metadata support on another filesystem;
- mounter maps path prefixes to child filesystems;
- union searches or writes through an ordered set of filesystems;
- repository exposes a collection of independently owned child filesystems as top-level entries;
- ZIP and XML persist a filesystem representation in a file owned by another filesystem.

Factories are registered through the [URL factory mechanism](../factories/index.md). `DProjects.Fs.Assembly` and provider markers identify assemblies
for explicit scanning. Nested protocols such as `zip:FSURL` and `filter:FSURL!...` reuse `IFactoryByUrl<IFilesystem>` instead of hard-coding concrete
children.

## Behavioral and compatibility contracts

Paths are virtual, slash-separated paths interpreted with `PathUtils`; a provider maps them to its backend. Local path resolution rejects traversal
outside the configured root. Entry enumeration distinguishes immediate files/directories from descendants and applies name patterns. Copy, move, and
sync settings govern overwrite and conflict behavior, including file/directory type replacement.

Read-only state is enforced before mutation. Unsupported optional behavior raises `NotSupportedException`; the base fallbacks do not silently succeed.
`Supports`/`SupportsAsync` allows callers to query `Touch`, `CreateWatcher`, `Metadata`, and `Select`, but support can be path-dependent in mounted
and repository views. Callers must still handle operation failures because capability and state can change.

Representative capabilities visible in implementations are:

| Implementation | Execution model | Explicit capabilities and limitations |
| --- | --- | --- |
| Local/temp/SMB | Sync-first | Local reports touch and watchers. Paths are root-contained; SMB reuses local behavior over a UNC root. |
| Memory | Sync-first | Reports touch and metadata; returned entries and metadata are snapshots. |
| Resource | Sync-first, read-only | Reads embedded assembly resources; mutation falls back to explicit read-only failure. |
| ZIP | Sync-first | Reports touch; can be read-only and is backed by another filesystem stream. |
| XML | Sync-first | Reports metadata; touch is explicitly unsupported. |
| HTTP | Async-first | Transports filesystem operations; watchers are explicitly unavailable and server mode may be read-only. |
| S3 | Async-first | Reports metadata; uses object-store semantics and supports optional gzip/cache behavior. |
| Repository | Async-first | Delegates within child containers; root/container mutation and metadata operations are deliberately restricted. |

The `Url` property is a diagnostic identity, not necessarily a round-trippable credential container. HTTP, SMB, and S3 implementations deliberately
omit passwords, API keys, or access secrets while retaining safe identity and option data. Code must not assume `Url` can recreate authenticated
storage.

## Verification

`FilesystemTests` is the reusable provider contract. Local/temp, memory, ZIP, and XML run it in `DProjects.Fs.Test`; HTTP and S3 inherit it in their
provider test projects. The suite covers entries and ordering, existence, read/write and append behavior, directory operations, delete, touch, copy,
move, sync, read-only state, streams, URL identity, and metadata when `Supports` reports it.

Focused tests supplement that broad contract with path-containment security, sync/async equivalence, cancellation, copy/sync type conflicts,
read-only enforcement, memory snapshots and etags, mounter boundaries, filter behavior, metadata persistence, repository child lifetimes, and URL
credential redaction. S3 hardening tests verify cancellation before network access and ownership of injected versus internally created HTTP clients.

HTTP and S3 contract suites are marked `Integration`. HTTP starts a local ASP.NET Core host; S3 requires a user-secret URL and external service.
Consequently, the normal non-integration suite gives strong coverage to reusable behavior and core providers but does not prove availability or
semantics of every external backend in each run.

## Related documentation

- [Factories](../factories/index.md)
- [Repository architecture](../architecture.md)
- [Documentation index](../index.md)
