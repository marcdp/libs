# DProjects.Libs

`DProjects.Libs` is a collection of reusable .NET libraries used across DProjects applications.

The repository contains abstractions, shared implementations, and technology-specific providers for concerns such as filesystems, database access, logging, log storage, and component factories.

Because these are libraries rather than a single application, public APIs and behavioral contracts are treated as compatibility boundaries. The repository favors extending implementations behind stable contracts over changing abstractions unnecessarily.

> **Support boundary:** This is a long-lived library repository and individual package families have different maturity and compatibility requirements. The subsystems highlighted below represent actively maintained architectural patterns and should not be interpreted as a guarantee that every project in the solution has the same support level.

## Architecture

A recurring design pattern in the repository separates contracts, reusable behavior, and technology-specific integration:

```text
Abstraction / contract
        ↓
Reusable core implementation
        ↓
Technology-specific provider
```

For example:

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
```

Abstraction projects define contracts and the types required by those contracts. Shared implementations contain reusable behavior, while provider-specific dependencies and semantics remain in their corresponding adapters.

This keeps dependency direction explicit and avoids pushing technology-specific assumptions into generic libraries.

## Main library families

| Area | Purpose | Examples |
| --- | --- | --- |
| Filesystem | Common filesystem contract with reusable operations and backend-specific implementations | Local, memory, HTTP, S3, ZIP |
| Database | Provider-independent database contracts with database-specific adapters | PostgreSQL, SQLite, SQL Server, Oracle |
| Logging | Logging abstractions and integrations with the .NET logging ecosystem | Microsoft `ILogger`, OpenTelemetry |
| Log storage | Storage and reading of structured log data through explicit storage contracts | Filesystem-backed and null implementations |
| Factories | Protocol-based creation and discovery of implementations | `IFactoryByUrl<T>` |

## Verification

Testing is organized around behavior and contract boundaries.

The repository uses:

- unit tests for focused behavior;
- reusable contract tests for abstractions implemented by multiple providers;
- provider-specific tests for backend-specific behavior;
- integration tests when external infrastructure is required.

Examples of reusable contract suites include filesystem behavior, database readers, and database connections.

The CI pipeline treats repository-wide validation as an engineering contract:

```text
Restore
   ↓
Release build
   ↓
Non-integration test suite
   ↓
Selected real-provider contract validation
   ↓
Package
```

CI currently provisions PostgreSQL and executes selected database provider-contract validation against a real PostgreSQL instance. Other integrations that require external services or credentials remain separately categorized as integration tests.


## Engineering principles

The maintained subsystems follow a few recurring engineering principles.

### Public contracts are compatibility boundaries

Public interfaces, method signatures, constructors, package identities, protocols, and serialization behavior can affect consumers outside this repository.

Implementation changes are therefore preferred over unnecessary changes to established public contracts.

### Reusable behavior belongs in shared implementations

Providers should implement the lowest appropriate technology-specific primitives and reuse common behavior where semantics allow it.

This is particularly visible in the filesystem architecture, where shared implementations provide operations that would otherwise be duplicated across storage backends.

### Providers own technology-specific behavior

Provider SDKs, infrastructure assumptions, and backend-specific semantics belong in provider projects rather than generic abstractions.

A common contract does not imply that every backend has identical capabilities.

### Unsupported capabilities are explicit

Optional capabilities should not silently pretend to work.

When an operation is deliberately unsupported by a provider, the implementation should expose that limitation explicitly rather than return misleading defaults.

### Sync and async APIs describe the same logical behavior

Where a contract exposes synchronous and asynchronous APIs, they are treated as different access paths to the same behavior rather than independent implementations with unrelated semantics.

The implementation strategy can still follow the natural capabilities of the underlying technology.

### Shared contracts should have shared verification

When several providers implement the same abstraction, common behavior is preferably verified through reusable contract tests.

Provider-specific tests are then reserved for behavior that genuinely differs between implementations.

## Selected design decisions

### Filesystem providers

The filesystem subsystem separates the public filesystem contract from reusable implementation strategies and concrete providers.

Providers can derive from sync-first, async-first, or independent sync/async base implementations according to the natural behavior of the backend. Higher-level operations can then be implemented once in shared code instead of being independently reproduced by every provider.

This keeps common filesystem semantics centralized while still allowing providers to expose genuine backend-specific capabilities and limitations.

### .NET logging interoperability

The logging subsystem integrates with `Microsoft.Extensions.Logging` through the standard `ILoggerProvider` mechanism.

This allows DProjects logging implementations to participate in the existing .NET logging ecosystem instead of requiring applications to adopt a parallel logging infrastructure.

### OpenTelemetry

OpenTelemetry export uses the standard OpenTelemetry .NET implementation rather than maintaining a custom OTLP transport.

The OpenTelemetry adapter remains a technology-specific provider while participating in the same factory and logging architecture used elsewhere in the repository.

### Database providers

Database abstractions expose common connection, reader, command, schema, and metadata concepts while provider-specific behavior remains in the corresponding database adapters.

Shared contracts define behavior that should remain consistent across providers, while provider-specific capabilities and metadata differences are preserved rather than hidden behind misleading generic behavior.


## Support and limitations

`DProjects.Libs` has evolved over a long period and contains libraries with different maintenance histories, compatibility constraints, and maturity levels.

The architecture and verification practices described in this README apply most clearly to the actively maintained subsystems highlighted above. They should not be interpreted as a claim that every project or provider in the solution is equally complete or supported.

Some integrations require infrastructure, credentials, or environment-specific configuration and therefore are not exercised by the normal repository-wide CI run.

Provider abstractions also do not attempt to erase meaningful differences between underlying technologies. Capabilities that cannot be represented correctly by a provider should remain explicit rather than being simulated with misleading behavior.

## Build and validation

The repository uses the .NET SDK version defined in `global.json`.

Restore the solution:

```bash
dotnet restore DProjects.Libs.sln
```

Build the complete solution in Release configuration:

```bash
dotnet build DProjects.Libs.sln \
  --configuration Release \
  --no-restore
```

Run the repository-wide non-integration test suite:

```bash
dotnet test --solution DProjects.Libs.sln \
  --configuration Release \
  --no-build \
  --no-restore \
  -- \
  --filter-not-trait "Category=Integration" \
  --ignore-exit-code 8
```

Package the libraries:

```bash
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
docs/       subsystem documentation
.github/    CI and repository automation
```

Start with the [documentation index](docs/index.md) for architecture and subsystem navigation.

`AGENTS.md` documents the repository's detailed architecture, compatibility rules, implementation conventions, and validation requirements for AI coding agents and repository changes.
