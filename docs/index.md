# DProjects.Libs documentation

This documentation explains the architecture, compatibility boundaries, extension mechanisms, verification, and operational limitations evidenced
by the source and tests. For repository purpose and build commands, start with the [root README](../README.md).

## Architecture and cross-cutting concerns

- [Repository architecture](architecture.md) — layering, dependencies, compatibility, target frameworks, and verification boundaries.
- [Support and status](support.md) — maintained surfaces, verification evidence, and important limitations.
- [Verification](verification.md) — focused tests, shared contracts, provider checks, integrations, CI, and local validation.
- [Architecture decisions](decisions/index.md) — index for concrete decision records; none are currently recorded.

## Core architectural subsystems

- [Factories](factories/index.md) — URL protocols, explicit assembly scanning, aliases, secret substitution, and runtime dispatch.
- [Filesystem](filesystem/index.md) — sync/async base classes, path and entry contracts, composition, providers, and capabilities.
- [Database](database/index.md) — connections, readers/writers, portable schema, provider differences, and live-test coverage.
- [Logging](logging/index.md) — structured logging, Microsoft logging adapters, OpenTelemetry, serializers, and log storage.

## Additional subsystems

- **DProjects.Azure** — **Legacy / compatibility** integration retained for existing consumers and historical DProjects functionality; new development
  should depend on it only when there is a concrete compatibility requirement.
- [Cache](cache/index.md) — stream values, metadata framing, expiration, cleanup, filesystem storage, and bounded provider guarantees.
- [Crypto](crypto/index.md) — algorithms, password-derived formats, stream ownership, compatibility, and security limits.
- [Identity](identity/index.md) — evolving sign-in and membership abstractions, claims-based identity, provider composition, and extension boundaries.
- [Mail](mail/index.md) — the sender contract, verified database spooling and EML behavior, delivery boundaries, and lifecycle limits.
- [Queues](queues/index.md) — filesystem claim flow, polling, acknowledgement limits, and absent broker guarantees.
- [Repositories](repositories/index.md) — identified entities, file-per-entity serialization, ownership, and consistency limits.
- [Secrets](secrets/index.md) — identifiers versus values, providers, managers, factory substitution, and disclosure boundaries.
- [Streams](streams/index.md) — capabilities, ownership, bounded views, transforms, composition, and cancellation.
- [Utils](utils/index.md) — maintained cross-cutting helpers, transitive coupling, compatibility boundaries, and platform-specific limits.
- [XVault interoperability](xvault/index.md) — reading partially encrypted XVault files from .NET.

## Reading paths

For adding or configuring a provider, read [Factories](factories/index.md) first and then the relevant subsystem page. For a public or package-level
change, begin with [Repository architecture](architecture.md), then check the subsystem's behavioral and test contracts. Use
[Support and status](support.md) to interpret maintenance classifications and external-integration coverage, and [Verification](verification.md) to
understand what the repository's test and CI layers prove.

These pages describe architecture rather than list every public API. Source and executable tests remain authoritative when a detail is not covered.
`AGENTS.md` separately contains instructions for coding agents and is not part of the consumer-facing architecture documentation.
