# Repositories

The repositories family is a compact asynchronous abstraction for storing identified entities. Its current concrete behavior is file-per-entity
persistence over `IFilesystem`; it does not implement a broader domain model, unit of work, query language, or change tracker.

```text
DProjects.Repositories
        ↓
DProjects.Repositories.Abstractions
```

`DProjects.Repositories` also depends on filesystem abstractions and the repository's YAML serializer. There are no technology-specific repository
provider packages.

## Identity and operations

`IGenericRepositoryElement<TKey>` requires a mutable `Id`. `IGenericRepository<TEntity,TKey>` offers asynchronous get, list, add, save, and remove
operations. Although entity identity is generic, the get and remove methods accept a `string`; the filesystem implementation also converts `Id` to
text when forming a filename. Callers should therefore treat the textual ID representation and its filesystem validity as compatibility concerns.

`GenericRepositoryFsDir` writes `<id>.<format>` below a configured directory. `GetAsync` returns `null` when that file is absent. `ListAsync` applies
the caller's pattern with the configured extension and yields deserialized entities. `RemoveAsync` delegates to filesystem deletion.

`AddAsync` and `SaveAsync` perform the same save operation. The abstraction does not specify an add-only conflict, optimistic version,
compare-and-swap, or update precondition. Concurrent writers, atomic replacement, durability, and listing consistency inherit whatever the supplied
filesystem actually provides; the repository layer adds no locking or transaction.

## Serialization and persistence boundary

Three formats are selectable at construction:

- JSON uses `System.Text.Json`, camel-case property names, and indented output;
- YAML uses the repository YAML serializer without front matter;
- YFM uses YAML front matter and treats a property named `content` as body content.

The selected extension and serialized representation are persisted compatibility surfaces. Changing entity shape, serializer behavior, property
naming, front-matter handling, or an ID's string representation can affect existing files. Deserialization errors propagate; the implementation does
not provide schema migration, validation, or corrupt-entry quarantine.

The constructor synchronously ensures the configured directory exists. Subsequent operations are asynchronous and pass their cancellation token to
filesystem access, including list enumeration and loading each listed file.

## Factories and ownership

The concrete assembly exposes an `IAssembly` marker and references factory abstractions, but it currently defines no
`IFactoryByUrl<IGenericRepository<...>>` implementation or protocol metadata. Repository construction is therefore direct and generic at compile time,
not URL-selected through the established factory mechanism.

Neither repository interface is disposable. `GenericRepositoryFsDir` retains the supplied filesystem and does not close it, so filesystem ownership
must be managed outside the repository.

## Verification and limitations

`DProjects.Repositories.Test` verifies add/get/save/remove/list behavior across JSON, YAML, and YAML front matter, including filtered and empty lists,
missing entries, YFM body content, malformed JSON/YAML, invalid format selection, unsafe ID/path rejection, and cancellation across all filesystem
operations. There is no reusable multi-provider repository contract because the family currently has one filesystem-backed implementation. The
nearby `FilesystemRepository` type belongs to filesystem implementation internals and is not an implementation of this generic entity contract.

The tests do not establish transactional writes, repository-level concurrency control, atomic replacement, durability, or stronger consistency than
the supplied filesystem. See
[Support and status](../support.md), [Filesystem](../filesystem/index.md), and [Factories](../factories/index.md).

Return to the [documentation index](../index.md).
