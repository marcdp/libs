# DProjects.Libs documentation

This documentation explains the architecture, compatibility boundaries, extension mechanisms, verification, and operational limitations evidenced
by the source and tests. For repository purpose and build commands, start with the [root README](../README.md).

## Start here

- [Repository architecture](architecture.md) — layering, dependencies, compatibility, target frameworks, and verification boundaries.
- [Factories](factories/index.md) — URL protocols, explicit assembly scanning, aliases, secret substitution, and runtime dispatch.
- [Filesystem](filesystem/index.md) — sync/async base classes, path and entry contracts, composition, providers, and capabilities.
- [Database](database/index.md) — connections, readers/writers, portable schema, provider differences, and live-test coverage.
- [Logging](logging/index.md) — structured logging, Microsoft logging adapters, OpenTelemetry, serializers, and log storage.
- [Utils](utils/index.md) — shared helper scope, transitive coupling, compatibility risks, and legacy limitations.
- [Support and status](support.md) — maintained surfaces, verification evidence, and important limitations.
- [Architecture decisions](decisions/index.md) — index for concrete decision records; none are currently recorded.

## Reading paths

For adding or configuring a provider, read [Factories](factories/index.md) first and then the relevant subsystem page. For a public or package-level
change, begin with [Repository architecture](architecture.md), then check the subsystem's behavioral and test contracts. Use
[Support and status](support.md) to interpret maintenance classifications and external-integration coverage.

These pages describe architecture rather than list every public API. Source and executable tests remain authoritative when a detail is not covered.
`AGENTS.md` separately contains instructions for coding agents and is not part of the consumer-facing architecture documentation.
