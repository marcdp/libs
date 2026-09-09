# Repository architecture

`DProjects.Libs` is a solution of dozens of reusable .NET libraries and their tests rather than one deployable application. Package boundaries,
public contracts, protocols, and persisted representations consequently matter as much as internal implementation structure.

## Organizing model

Maintained subsystem families commonly separate three roles:

```text
Technology-specific provider
        ↓
Reusable core implementation
        ↓
Abstraction / contract
```

Arrows indicate dependency direction: provider projects depend on reusable core projects, which depend on abstractions. For example:

```text
DProjects.Fs.Aws  DProjects.Fs.Http  PostgreSQL  SQLite  SQL Server  Oracle
       ↓          ↓                  ↓       ↓       ↓       ↓
      DProjects.Fs                      DProjects.Db
            ↓                               ↓
DProjects.Fs.Abstractions        DProjects.Db.Abstractions
```

This is a recurring pattern, not a claim that every project has three layers. Some foundational packages stand alone, while other families use an
abstraction/core pair without a separately packaged provider.

## Contracts and implementations

An abstraction package owns the public interfaces and the data/settings types needed to use them. A core package owns reusable behavior and default
implementations. Provider packages own client libraries, transport assumptions, and backend-specific mappings. Tests depend on the layer whose
behavior they verify.

The separation serves observable repository needs:

- consumers can reference a contract without selecting a provider in code;
- common behavior such as filesystem copy/sync or database command ownership is implemented once;
- provider packages can target a newer framework or carry a driver without imposing it on every contract consumer;
- unsupported backend behavior remains explicit instead of being approximated by a misleading common denominator.

Not every current dependency is ideal. In particular, `DProjects.Log.Abstractions` references concrete `DProjects.Fs` even though its public contracts
do not use filesystem types, and many abstraction projects depend on the broad Utils package. These are existing compatibility and package-graph
constraints, not examples to extend. Architecture should be inferred from the active contracts, implementations, and tests rather than from one
legacy project reference.

## Provider extension through factories

`IFactoryByUrl<T>` is the cross-cutting provider selection mechanism. Factories declare a protocol such as `file`, `s3`, `postgresql`, or `otlp` and
parse the rest of the URL into an implementation. Callers explicitly register factories or scan known assemblies; provider assemblies commonly expose
an `IAssembly` marker for this purpose.

This mechanism is used across filesystem, database, logging, log storage, cache, crypto, identity, mail, queues, repositories, and secrets. It avoids
a central switch in the contract or core package and lets provider dependencies remain independently packaged. The trade-off is runtime validation:
protocol duplication, parsing errors, missing dependency-injection services, and unsupported options are not all visible to the compiler.

Factory discovery is not automatic plugin loading. An application chooses the assemblies to scan. Protocol attributes provide routing and help
metadata; they do not activate an assembly. See [Factories](factories/index.md) for dispatch and compatibility details.

## Compatibility boundaries

The repository exposes more than C# signatures. Compatibility-sensitive surfaces include:

- public types, constructors, members, nullability, exception categories, and ownership/disposal rules;
- assembly names, package IDs, target frameworks, and project dependency direction;
- URL protocols, aliases, query keys, defaults, and secret-substitution syntax;
- path semantics, logging formats, text reader/writer formats, database schema mappings, and other serialized data;
- ordering, cursor position, filtering, read-only behavior, and sync/async equivalence established by tests.

Stable abstractions do not require every provider to have the same capabilities. The common convention is to report a deliberate unsupported
operation with `NotSupportedException`, while invalid input and invalid object state use their corresponding exception categories. Filesystem
capabilities can also be queried, but operation failure remains authoritative.

Security-sensitive identity values have narrower contracts than creation URLs. Filesystem `Url` values redact credentials and may not recreate an
authenticated provider. Database execution uses native parameters; rendered SQL is diagnostic only. Persisted URLs and logs must not be treated as a
safe place for secrets merely because a factory can substitute them at creation time.

## Sync, async, and ownership

Where a contract exposes sync and async APIs, they describe the same logical state. The implementation strategy follows the backend:

- filesystem base classes provide sync-first, async-first, or independent implementations;
- database async paths call provider async operations and propagate cancellation;
- cursor read variants share one position;
- streaming filesystem, database-reader, and log-storage results retain ownership until disposed or enumeration completes.

Adapters necessarily have limits. A sync-first filesystem can check cancellation before synchronous work but cannot cancel that work once running;
`AsyncUtils` bridges block a thread; background logging has no durable-delivery acknowledgement. These differences are documented at the subsystem
boundary rather than hidden behind identical method names.

## Target frameworks and package graph

`global.json` selects the repository SDK and test runner. Most reusable libraries target `netstandard2.0`, while selected database providers and the
ASP.NET Core filesystem middleware target a newer framework. The build SDK does not expand the API surface available to a `netstandard2.0` library;
subsystem tables retain exact targets where those differences affect consumers.

Projects carry their own package metadata and dependencies. The solution is therefore not a monolith: adding a package reference to an abstraction or
shared utility changes the transitive dependency set for multiple consumers. Provider drivers belong in provider projects, and target frameworks or
package identities should not be normalized as incidental cleanup.

## Verification architecture

Tests define behavioral boundaries at three scopes:

1. focused unit and hardening tests cover parsing, lifecycle, security, cancellation, error, and edge-case behavior;
2. reusable contract suites run the same behavior against multiple implementations, notably `FilesystemTests`, `DBConnectionTests<T>`, and the mixed
   `IDBReader` cursor assertions;
3. provider tests cover dialect, metadata, transport, resource ownership, and explicit limitations that cannot be generalized.

External-resource tests carry the `Integration` trait. CI restores and Release-builds the whole solution, runs all non-integration tests, provisions
PostgreSQL for one selected real-provider schema-discovery contract, and packs the solution. The build proves package compatibility and the normal
test suite proves reusable behavior; neither proves every credential-dependent provider on every run.

```text
restore → Release build → non-integration tests → PostgreSQL provider contract → pack
```

Contract suites are evidence for their included implementations only. A provider-specific test that inherits a shared suite but is integration-only
does not run in the ordinary non-integration stage. Conversely, a passing unit test for generated SQL does not prove a live server accepts every
schema operation.

## Documented subsystem boundaries

- [Factories](factories/index.md): URL-based selection, registration, assembly scanning, and protocol compatibility.
- [Filesystem](filesystem/index.md): common paths and entries, reusable base classes, composition, providers, and capability differences.
- [Database](database/index.md): connection/reader/writer contracts, portable schema, provider mappings, and live verification limits.
- [Logging](logging/index.md): structured entries, .NET logging adapters, OpenTelemetry, serializers, and separate log storage.
- [Utils](utils/index.md): the broad shared helper layer, transitive coupling, compatibility risks, and legacy limitations.

Architecture decision records are reserved for concrete decisions with alternatives and consequences. The current
[decisions index](decisions/index.md) contains no ADRs; this page documents architecture evidenced by the current source rather than inventing
historical rationale.

Return to the [documentation index](index.md), review [Support and status](support.md), or use the [root README](../README.md) for repository
orientation and build commands.
