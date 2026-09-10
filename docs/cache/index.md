# Cache

The cache family stores keyed binary payloads together with HTTP-style metadata. It is a small stream-oriented cache, not a general distributed cache
or an object cache. Its public behavior is defined by `DProjects.Cache.Abstractions`; `DProjects.Cache` supplies filesystem-backed and null
implementations plus URL factories.

```text
DProjects.Cache
        ↓
DProjects.Cache.Abstractions
```

The concrete package also depends on filesystem abstractions and stream wrappers. It does not add a provider SDK or a serialization layer for
application objects.

## Entry and lifetime contract

`IBlobCache` supports set, get, remove, get-or-create, asynchronous equivalents, and an asynchronous cleanup operation. A `BlobCacheEntry` contains a
string key, a `Stream`, and mutable headers for content type, content length, ETag, dates, and expiration.

The stream is the value. The subsystem does not serialize arbitrary objects or define a stable object schema. Disposing a returned `BlobCacheEntry`
disposes its stream, so callers own the entry returned by `Get` or `GetAsync`. Cache objects are themselves disposable, although the current concrete
implementations have no disposal work and do not dispose an injected filesystem.

## Filesystem storage and expiration

`BlobCacheFsDir` URL-encodes the key into a `.blob` filename. Each file contains serialized HTTP-style headers followed by the payload; reads expose
only the declared content length through `LimitedInputStream`. Writes stage content in temporary files before moving the completed representation to
its final path. That reduces exposure to a partially written final entry, but it is not a cross-process transaction or an atomic invalidation promise.

If a write has no `Expires` header, the implementation assigns an expiration one hour from the write. `Get` treats a missing or past expiration as a
miss and removes the entry. The get-or-create overload sets the produced entry's expiration to the requested interval before persisting it. No locking
or single-flight mechanism coordinates concurrent misses, so multiple callers may invoke the producer and replace the same key.

`Clean` sweeps `.blob` files based on their stored expiration. Tests verify removal of expired and missing-expiration entries, preservation of valid
entries and unrelated files, and fail-fast behavior for an invalid expiration value. Reads also remove expired or missing-expiration entries.

## Construction and provider behavior

Factories participate in `IFactoryByUrl<IBlobCache>` discovery through the package assembly marker:

- `fs-dir:` binds a cache directory within an injected `IFilesystem`;
- `file:` creates a filesystem through `IFactoryByUrl<IFilesystem>` and uses its root as the cache directory;
- `null:` discards writes and returns misses.

The null provider is useful for disabling storage, but it does not persist values: basic reads miss and writes, removals, and cleanup are no-ops.
Its sync and async get-or-create overloads invoke the caller's producer and return that live entry. Tests pin these explicit null-cache semantics;
they do not imply behavioral equivalence with filesystem persistence.

## Guarantees and verification boundary

The implementation establishes local key-to-file behavior, metadata framing, explicit expiration checks on reads, and sync/async entry points. It
does not establish durability, distributed consistency, atomic invalidation, eviction policy, capacity limits, or coherent multi-process updates.
Those properties depend on the supplied filesystem, and several are not represented by the cache contract at all.

`DProjects.Cache.Test` directly verifies sync and async set/get and replacement, metadata and default/explicit expiration, missing and expired
entries, safe key-to-file mapping, cleanup outcomes, get-or-create persistence and producer ownership, idempotent removal, cancellation at
filesystem and payload-copy boundaries, entry stream disposal, null-cache behavior, and `fs-dir:`, `file:`, and `null:` factories.

The suite uses a filesystem-backed implementation and does not establish concurrent single-flight producers, distributed consistency, durability,
capacity, eviction, or multi-process coordination. This maintained but bounded surface is reflected in [Support and status](../support.md).

Return to the [documentation index](../index.md) or review [Factories](../factories/index.md), [Filesystem](../filesystem/index.md), and
[Streams](../streams/index.md) for the collaborating boundaries.
