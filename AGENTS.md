# AGENTS.md

This file defines the engineering rules for AI coding agents working in the `DProjects.Libs` repository.

The objective is not only to make changes that compile. Changes must preserve the architectural boundaries, public contracts, compatibility goals, and extension patterns of the repository.

## Repository purpose

`DProjects.Libs` contains reusable .NET libraries used by DProjects applications.

This is a library repository rather than a single application. Public APIs, package boundaries, assembly names, target frameworks, and behavioral contracts therefore matter.

The solution is:

```text
DProjects.Libs.sln
```

The main repository structure is:

```text
src/        production libraries
test/       unit, contract, and integration tests
docs/       documentation
.github/    CI and repository automation
```

---

# 1. Architectural model

The repository is organized primarily around small domain-focused libraries.

Common project families follow patterns such as:

```text
Abstraction / contract
        ↓
Core implementation
        ↓
Technology-specific adapter
```

Examples include:

```text
DProjects.Fs.Abstractions
        ↓
DProjects.Fs
        ↓
DProjects.Fs.Aws
DProjects.Fs.Http
...

DProjects.Db.Abstractions
        ↓
DProjects.Db
        ↓
DProjects.Db.Postgresql
DProjects.Db.Sqlite
DProjects.Db.Sqlserver
DProjects.Db.Oracle
...

DProjects.Factories.Abstractions
        ↓
DProjects.Factories
```

Other domains follow similar principles even when they do not have all three layers.

## Dependency direction

Preserve the existing dependency direction.

In general:

* `*.Abstractions` contains public contracts and the types required by those contracts.
* Core implementation projects implement those abstractions and reusable behavior.
* Technology/provider-specific projects depend on the core library rather than pushing provider dependencies into the core.
* Tests depend on the projects whose behavior they verify.

Do not introduce references from abstractions to concrete implementations.

Do not introduce circular project references.

Do not move provider-specific SDK dependencies into abstraction projects or generic core libraries unless the architecture explicitly requires it.

Before adding a project reference, ask whether the dependency belongs at that architectural level.

---

# 2. Public API compatibility

Treat public types in `src/` as library APIs.

Do not casually change:

* public interfaces
* public method signatures
* public constructors
* public property semantics
* namespaces
* assembly names
* package IDs
* target frameworks
* URL/protocol syntax
* serialization formats

A change to a public contract may affect consumers outside this repository.

If the requested work can be completed without changing a public abstraction, prefer that solution.

Do not redesign an interface merely because another design appears cleaner.

When a public API change is explicitly required, update:

1. the abstraction,
2. all implementations,
3. shared contract tests,
4. implementation-specific tests,
5. relevant factories,
6. relevant documentation.

---

# 3. Abstractions versus implementations

Put contracts in the appropriate `*.Abstractions` project when that domain has one.

Examples of contract-level code include:

* interfaces
* public enums used by interfaces
* public settings/options used by contracts
* shared DTOs required to use an abstraction
* contract-level exceptions when appropriate

Implementation details belong in concrete projects.

Avoid adding implementation-specific behavior to an abstraction just to make one provider easier to implement.

Prefer extending implementations before expanding a stable public contract.

---

# 4. Filesystem architecture

The filesystem subsystem has an intentional implementation hierarchy.

The public contract is:

```text
IFilesystem
├── IFilesystemSync
├── IFilesystemAsync
└── IFilesystemInfo
```

Concrete filesystem implementations should normally derive from one of these base classes:

```text
Filesystem
FilesystemSync
FilesystemAsync
```

Choose the base class according to the backend's natural capabilities.

## `FilesystemSync`

Use `FilesystemSync` when the underlying implementation is fundamentally synchronous.

Implement the synchronous primitive operations and allow the base class to provide the corresponding asynchronous adapters where appropriate.

Typical examples are local or in-memory synchronous storage implementations.

## `FilesystemAsync`

Use `FilesystemAsync` when the underlying implementation is naturally asynchronous, such as a remote/network-backed service with native asynchronous APIs.

Implement the asynchronous primitive operations and allow the base class to provide synchronous bridges where appropriate.

## `Filesystem`

Use `Filesystem` when synchronous and asynchronous primitive implementations genuinely need to be implemented independently.

Do not implement `IFilesystem` directly for new filesystem implementations when one of the filesystem base classes can express the behavior.

Some existing classes may predate this rule. Do not use those direct implementations as the preferred pattern for new code.

Do not perform broad migrations of existing filesystem implementations unless the task specifically requires them.

## Filesystem implementation levels

The filesystem API is intentionally layered.

Low-level primitive operations are used by higher-level reusable behavior.

Prefer implementing or overriding the lowest appropriate operation and reuse the base implementation for higher-level operations.

Do not duplicate recursive copy, move, synchronization, existence checking, or other shared behavior in every filesystem unless backend-specific semantics require it.

## Filesystem capabilities

Optional filesystem capabilities should fail explicitly when unsupported.

Use `NotSupportedException` for a capability that is intentionally unsupported.

Do not introduce `NotImplementedException` merely because implementing a required contract is inconvenient.

Respect `IsReadonly`. Mutating operations must not silently modify a read-only filesystem.

Paths must follow the repository's filesystem path semantics and existing `PathUtils` utilities.

## Filesystem contract tests

Filesystem implementations are expected to satisfy the shared filesystem behavior represented by `FilesystemTests`.

When adding or changing a filesystem implementation:

* run the shared filesystem contract tests,
* add the implementation to the shared test structure where appropriate,
* test both synchronous and asynchronous behavior exposed by `IFilesystem`,
* preserve path, entry, ordering, read/write, copy, move, and other existing contract semantics.

A provider-specific test should not redefine filesystem semantics independently from the shared contract.

---

# 5. Database architecture

Database contracts live primarily in:

```text
DProjects.Db.Abstractions
```

Generic database behavior belongs in:

```text
DProjects.Db
```

Provider-specific implementations belong in projects such as:

```text
DProjects.Db.Postgresql
DProjects.Db.Sqlite
DProjects.Db.Sqlserver
DProjects.Db.Oracle
```

Provider SDKs and provider-specific behavior should remain inside the corresponding adapter project wherever possible.

Do not introduce provider-specific assumptions into `DProjects.Db.Abstractions`.

---

# 6. IDBReader contract

`IDBReader` is a cursor-style abstraction.

All implementations must expose consistent behavior across:

```text
Read()
Read(object?[] values)
ReadAsync(...)
ReadAsync(object?[] values, ...)
```

These methods are different access paths over the same reader cursor.

They must not maintain independent cursor positions.

After the final row:

```text
Read()               -> null
Read(values)         -> false
ReadAsync()          -> null
ReadAsync(values)    -> false
```

`GetColumnsCount()`, `GetColumns()`, and `GetColumnsAsync()` must describe the same logical result set.

`NextResult()` and `NextResultAsync()` must follow equivalent result-set semantics.

When modifying an `IDBReader` implementation, run and extend the shared `DBReaderContractTests`.

Do not fix one reader implementation in a way that gives it semantics inconsistent with the other implementations.

---

# 7. Factory and URL protocol architecture

A major extension mechanism in this repository is:

```text
IFactoryByUrl<T>
```

Implementations can be selected from URL-like protocols.

Examples include filesystem, logging, queues, database connections, and other configurable components.

When adding a URL-created implementation, prefer the established factory mechanism instead of adding central `switch` statements or hard-coded type checks.

A typical factory follows this form:

```csharp
[Protocol("protocol", "description")]
[ProtocolUsage("protocol:...")]
[ProtocolExample("protocol:example", "description")]
public class SomeFactory : IFactoryByUrl<ISomething> {
    ...
}
```

Use the existing protocol attributes where applicable:

```text
Protocol
ProtocolUsage
ProtocolExample
```

Factories are discovered and registered through the existing factory configuration and assembly-scanning mechanisms.

Preserve this mechanism.

Do not introduce a parallel service-location or plugin mechanism unless explicitly requested.

## Assembly discovery

Projects participating in assembly-based factory discovery commonly expose an `Assembly` marker implementing `IAssembly`.

When creating a new adapter/project that must participate in automatic factory discovery, follow the existing assembly marker and registration conventions.

Ensure tests verify that the factory can actually be discovered and invoked through `IFactoryByUrl<T>`.

## URL compatibility

Protocol strings form part of the effective public API.

Do not silently change:

* protocol names,
* URL parsing,
* query parameter names,
* aliases,
* default values.

When adding query options, preserve existing URLs whenever possible.

---

# 8. Sync and async behavior

Many contracts intentionally expose both synchronous and asynchronous APIs.

Preserve the relationship established by the relevant base class.

Do not create two independent implementations when one is intended to delegate to the other.

When implementing asynchronous code:

* propagate `CancellationToken` where the contract provides one,
* pass the token to downstream asynchronous operations when supported,
* do not silently replace asynchronous I/O with blocking I/O when a native async API exists.

When implementing a synchronous backend through `FilesystemSync`, use its established sync-first model.

When implementing an asynchronous backend through `FilesystemAsync`, use its established async-first model.

Do not replace these models globally without an explicit architectural task.

---

# 9. Target framework compatibility

The repository is built with the .NET SDK configured by:

```text
global.json
```

Currently the repository uses the .NET 10 SDK.

However, many reusable source projects target:

```text
netstandard2.0
```

Some provider or platform-specific projects target newer frameworks such as:

```text
net10.0
```

The SDK used to build a project is not the same thing as the API surface available to that project's target framework.

When editing a `netstandard2.0` project:

* do not use APIs unavailable in `netstandard2.0`,
* verify package compatibility,
* preserve the project's target framework unless explicitly asked to change it.

Do not normalize all projects to the same target framework as part of unrelated work.

---

# 10. NuGet/package boundaries

Most production projects are reusable NuGet-style libraries.

Do not modify package metadata as a side effect of an implementation task.

In particular, do not change unless explicitly required:

```text
PackageId
AssemblyName
Version
Authors
TargetFramework
```

Do not bump package versions automatically.

Adding a dependency to a package is an architectural decision. Prefer existing repository abstractions and utilities before adding another external package.

---

# 11. Testing strategy

Tests are located under:

```text
test/
```

The repository uses xUnit-style tests.

Prefer tests that verify public behavior rather than internal implementation details.

## Contract tests

When several implementations share an abstraction, prefer reusable contract tests.

Examples already present include:

```text
FilesystemTests
DBReaderContractTests
DBConnectionTests<T>
```

When adding another implementation of an existing abstraction, first check whether an existing contract test can be reused.

If a bug reveals a missing shared invariant, prefer adding that invariant to the shared contract tests when appropriate.

Then add implementation-specific tests only for behavior unique to that implementation.

## Sync and async tests

If a contract exposes synchronous and asynchronous variants, verify both when behavior could diverge.

Async tests should use the test cancellation token where practical:

```csharp
TestContext.Current.CancellationToken
```

## Integration tests

Tests requiring external infrastructure, credentials, cloud services, network services, or real database servers must be marked:

```csharp
[Trait("Category", "Integration")]
```

Do not make normal CI depend on locally configured credentials or external infrastructure.

Do not remove the `Integration` categorization simply to make a test execute in CI.

---

# 12. Build and validation

The repository-wide engineering contract is the CI pipeline.

Before considering a repository-wide change complete, use the same basic sequence:

```bash
dotnet restore DProjects.Libs.sln

dotnet build DProjects.Libs.sln \
  --configuration Release \
  --no-restore

dotnet test DProjects.Libs.sln \
  --configuration Release \
  --no-build \
  --filter "Category!=Integration" \
  --ignore-exit-code 8

dotnet pack DProjects.Libs.sln \
  --configuration Release \
  --no-build \
  --output artifacts/packages
```

For a focused change, run the smallest relevant test project first.

Example:

```bash
dotnet test test/DProjects.Fs.Test/DProjects.Fs.Test.csproj
```

or:

```bash
dotnet test test/DProjects.Db.Test/DProjects.Db.Test.csproj
```

After focused tests pass, run the repository-wide non-integration test suite when practical.

Integration tests should be run only when the required infrastructure or credentials are available and the task requires them.

If build, test, or pack cannot be executed, explicitly report which validation was not performed.

Keep README build instructions and CI behavior aligned when changing build/test infrastructure.

---

# 13. Coding style

`.editorconfig` is the authoritative formatting and naming configuration.

Follow it rather than introducing a new style.

Important existing conventions include:

* 4-space indentation,
* C# interface names beginning with `I`,
* PascalCase types and public members,
* block-scoped namespaces,
* braces for control-flow blocks,
* nullable reference types where enabled by the project.

Do not mass-format unrelated files.

Do not convert an entire file to a different style merely because the edited section uses another style.

Keep diffs focused on the requested behavior.

---

# 14. Nullability and argument semantics

Respect nullable annotations.

Do not suppress nullable warnings with `!` unless the invariant is clear and justified.

Prefer explicit validation when a public method cannot operate correctly with a null or invalid argument.

Preserve existing distinctions between:

* null,
* empty collections,
* empty strings,
* missing entries,
* unsupported operations.

These differences can be part of a public library contract.

---

# 15. Errors and unsupported behavior

Use exceptions according to intent.

Prefer:

```text
ArgumentException / ArgumentOutOfRangeException
```

for invalid caller input.

Prefer:

```text
InvalidOperationException
```

when the object's current state makes the operation invalid.

Prefer:

```text
NotSupportedException
```

when an operation is intentionally unsupported by an implementation.

Do not add new `NotImplementedException` to production public paths as a substitute for implementing required behavior.

Existing `NotImplementedException` instances may represent unfinished code, intentional restrictions, or technical debt. Inspect the surrounding contract and tests before changing them.

Do not automatically replace every existing occurrence.

---

# 16. Security-sensitive behavior

Treat code involving the following areas as security-sensitive:

* cryptography,
* secrets,
* authentication,
* serialization of types,
* filesystem paths,
* remote URLs,
* database credentials,
* configuration interpolation.

Do not weaken an existing security restriction merely to make an API symmetrical or to make a test easier to write.

Do not log secrets, connection credentials, encryption keys, tokens, or sensitive URL query values.

When modifying security-sensitive behavior, add tests for invalid or hostile inputs where appropriate.

---

# 17. Scope discipline

Prefer the smallest change that correctly solves the requested problem.

Do not combine a bug fix with:

* unrelated refactoring,
* project renaming,
* namespace cleanup,
* broad formatting,
* package upgrades,
* framework upgrades,
* architecture redesign.

Repository cleanup should be performed as explicit work, not hidden inside functional changes.

When code looks unfinished outside the requested scope, do not automatically complete it.

If that unfinished behavior directly affects the requested contract, address it and add tests.

---

# 18. Before modifying code

Before implementing a change:

1. Identify the project containing the behavior.
2. Read its `.csproj`.
3. Identify the corresponding abstraction, if one exists.
4. Inspect the relevant base class.
5. Inspect at least one sibling implementation.
6. Locate the corresponding tests.
7. Check whether a shared contract test already defines the required behavior.
8. Identify whether the change affects a public API, package boundary, URL protocol, or serialized format.
9. Implement the smallest change consistent with those constraints.

Do not infer architecture from a single class when surrounding projects establish a broader pattern.

---

# 19. Adding a new implementation

When adding a new implementation of an existing abstraction:

1. Reuse the existing abstraction.
2. Place the implementation in the correct core or provider-specific project.
3. Use the established base class when one exists.
4. Keep external/provider dependencies in the adapter project.
5. Add an `IFactoryByUrl<T>` factory if the domain uses protocol-based construction.
6. Add protocol metadata when applicable.
7. Add assembly discovery support when applicable.
8. Reuse the existing shared contract tests.
9. Add implementation-specific tests for unique behavior.
10. Mark external-resource tests as `Integration`.
11. Verify build, tests, and package generation.

Do not create a new abstraction solely because a concrete implementation differs internally.

---

# 20. Definition of done

A change is complete when:

* the requested behavior is implemented,
* architectural boundaries remain intact,
* public compatibility is preserved unless a breaking change was explicitly requested,
* no unnecessary dependencies were introduced,
* relevant tests were added or updated,
* shared contract tests pass,
* relevant sync and async paths behave consistently,
* integration tests are categorized correctly,
* the relevant projects build,
* repository-wide CI validation passes when practical,
* unrelated code was not modified.

The goal is not merely:

> Make the code compile.

The goal is:

> Make the smallest correct change while preserving the contracts and architectural intent of DProjects.Libs.
