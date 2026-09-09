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

`Clean` is intended to sweep files based on their stored expiration. There are no effective cache tests establishing its observable sweep behavior,
so consumers should treat cleanup as unverified and rely on read-time expiration only where that is sufficient.

## Construction and provider behavior

Factories participate in `IFactoryByUrl<IBlobCache>` discovery through the package assembly marker:

- `fs-dir:` binds a cache directory within an injected `IFilesystem`;
- `file:` creates a filesystem through `IFactoryByUrl<IFilesystem>` and uses its root as the cache directory;
- `null:` discards writes and returns misses.

The null provider is useful for disabling storage, but it does not model the filesystem provider in every overload. Its synchronous get-or-create
returns the producer result directly, while its asynchronous get-or-create follows a set-then-read path even though null writes are discarded. Code
that selects `null:` should use the basic miss behavior deliberately rather than infer full provider equivalence.

## Guarantees and verification boundary

The implementation establishes local key-to-file behavior, metadata framing, explicit expiration checks on reads, and sync/async entry points. It
does not establish durability, distributed consistency, atomic invalidation, eviction policy, capacity limits, or coherent multi-process updates.
Those properties depend on the supplied filesystem, and several are not represented by the cache contract at all.

`test/DProjects.Cache.Test` currently contains a test project shell but does not reference the cache packages or contain executable cache tests.
Factory registration, filesystem failure behavior, cancellation, concurrent producers, cleanup, and null-provider equivalence therefore lack direct
subsystem verification. This limited evidence is reflected in [Support and status](../support.md).

Return to the [documentation index](../index.md) or review [Factories](../factories/index.md), [Filesystem](../filesystem/index.md), and
[Streams](../streams/index.md) for the collaborating boundaries.
