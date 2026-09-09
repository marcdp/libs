# DProjects.Libs

`DProjects.Libs` is a collection of .NET libraries for DProjects applications. It provides contracts, shared implementations, and providers
for filesystems, database access, logging, log storage, and component factories.

Because this is a library repository rather than a single application, public APIs and observable behavior are compatibility boundaries. The
preferred evolution path is to extend implementations behind stable contracts and keep provider concerns out of shared abstractions.

Package families do not all have the same maturity or verification depth. See [Support and status](docs/support.md) for the maintained surfaces,
evidence behind each classification, and important limitations.

## Architecture at a glance

A recurring pattern separates technology integration from reusable behavior and public contracts:

```text
Technology-specific provider
        ↓
Reusable core implementation
        ↓
Abstraction / contract
```

Arrows indicate dependency direction: provider projects depend on reusable core projects, which depend on abstractions.

For example:

```text
DProjects.Fs.Aws                DProjects.Db.Postgresql
DProjects.Fs.Http               DProjects.Db.Sqlite
        ↓                           ↓
   DProjects.Fs                    DProjects.Db
        ↓                           ↓
DProjects.Fs.Abstractions       DProjects.Db.Abstractions
```

This is a recurring model rather than a requirement that every package have three layers. The key boundary is ownership: abstractions define
contracts, shared projects implement reusable behavior, and adapters retain their SDKs, transport assumptions, and provider-specific semantics.

## Main library families

| Area | Purpose | Examples |
| --- | --- | --- |
| Filesystem | Common filesystem contract, shared operations, and backend-specific storage | Local, memory, HTTP, S3, ZIP |
| Database | Provider-independent database contracts and database-specific adapters | PostgreSQL, SQLite, SQL Server, Oracle |
| Logging | Structured logging and integration with the .NET logging ecosystem | Microsoft `ILogger`, OpenTelemetry |
| Log storage | Querying and retention of structured log data through explicit storage contracts | Filesystem-backed and null storage |
| Factories | Protocol-based construction and discovery of implementations | `IFactoryByUrl<T>` |

The [documentation index](docs/index.md) links to the architecture and subsystem guides.

## How the architecture works

`IFactoryByUrl<T>` is the main cross-cutting extension mechanism. Providers declare URL-like protocols and are registered explicitly or discovered
by scanning selected assemblies. This keeps provider dependencies independently packaged and lets applications choose implementations through
configuration. The trade-off is deliberate: protocol syntax, aliases, secrets, and dependency-injection requirements are validated at runtime.

Filesystem implementations build higher-level operations such as copy, move, and synchronization from the lowest appropriate provider primitives.
Sync-first and async-first base classes let each backend use its natural execution model while preserving one logical contract. Capability differences
remain visible; an object store, archive, local disk, and HTTP service are not forced into misleadingly identical behavior.

Database adapters apply the same separation to connections, cursors, schema models, and provider dialects. Shared contracts define cursor position,
resource ownership, cancellation, and lifecycle semantics. Provider packages retain their drivers and can reject schema operations that cannot be
represented safely rather than returning partial metadata that could make reconciliation destructive.

Logging participates in the standard .NET logging ecosystem and keeps OpenTelemetry as a provider integration. Persisted log querying and retention
live behind a separate storage contract, which avoids coupling log producers to one storage model. Background logging remains a throughput mechanism,
not a durable-delivery promise.

Across these families, ownership and disposal are part of the contract. Readers, streams, factories, providers, and composed filesystems may retain
resources beyond a single call, so tests cover lifecycle behavior alongside returned values.

## Engineering principles

- **Public contracts are compatibility boundaries.** Signatures, package identities, protocols, serialization, ownership, and error behavior can all
  affect consumers outside this repository.
- **Shared behavior belongs in shared implementations.** Providers implement the lowest appropriate primitives and reuse common operations when the
  backend semantics allow it.
- **Providers retain provider concerns.** SDKs, infrastructure assumptions, and technology-specific mappings stay in adapters rather than leaking
  into generic abstractions.
- **Unsupported capabilities are explicit.** A common contract does not imply identical provider capabilities; deliberate gaps fail clearly instead
  of returning misleading results.
- **Contracts deserve reusable verification.** Shared test suites establish common behavior, while focused provider tests cover genuine differences.

The deeper guides explain how these principles apply to [filesystem](docs/filesystem/index.md), [database](docs/database/index.md),
[logging](docs/logging/index.md), and [factory](docs/factories/index.md) design.

## Verification

Testing follows the boundaries in the architecture:

- focused unit and hardening tests cover behavior, lifecycle, security, and edge cases;
- reusable contract tests exercise abstractions implemented by multiple providers;
- provider tests cover dialects, transports, metadata, and explicit capability differences;
- integration tests identify behavior that requires credentials or external infrastructure.

CI restores and builds the solution, runs the non-integration suite, validates a selected database contract against PostgreSQL, and packages the
libraries. This provides broad contract evidence without implying that every external backend is exercised on every run.

## Support boundary

Maintained identifies a family that is actively treated as a compatibility and behavioral surface. It does not promise that every backend implements
every optional operation, works without its required infrastructure, or receives live integration coverage in each run. Unsupported provider
operations, credential-dependent tests, and environment-specific behavior remain explicit. The [support page](docs/support.md) records the evidence
and limitations for each documented family and classifies the broad Utils package separately as a legacy compatibility surface.

## Build and validation

The repository uses the .NET SDK selected by `global.json`.

```bash
dotnet restore DProjects.Libs.sln

dotnet build DProjects.Libs.sln \
  --configuration Release \
  --no-restore

dotnet test --solution DProjects.Libs.sln \
  --configuration Release \
  --no-build \
  --no-restore \
  -- \
  --filter-not-trait "Category=Integration" \
  --ignore-exit-code 8

dotnet pack DProjects.Libs.sln \
  --configuration Release \
  --no-build \
  --output artifacts/packages
```

Provider-specific integration tests may require additional services, credentials, or environment configuration.

## Repository structure

```text
src/        production libraries
test/       unit, contract, and integration tests
docs/       architecture and subsystem documentation
.github/    CI and repository automation
```

Start with the [documentation index](docs/index.md) for deeper navigation or [Support and status](docs/support.md) for the repository's explicit
support boundary. `AGENTS.md` contains the detailed engineering rules for repository changes.
