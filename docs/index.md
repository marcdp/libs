# DProjects.Libs documentation

This documentation explains the architecture, compatibility boundaries, extension mechanisms, verification, and known limitations evidenced by the
current source and tests. For package purpose, support scope, and build commands, start with the [root README](../README.md).

## Start here

- [Repository architecture](architecture.md) — layering, dependencies, compatibility, target frameworks, and verification boundaries.
- [Factories](factories/index.md) — URL protocols, explicit assembly scanning, aliases, secret substitution, and runtime dispatch.
- [Filesystem](filesystem/index.md) — sync/async base classes, path and entry contracts, composition, providers, and capabilities.
- [Database](database/index.md) — connections, readers/writers, portable schema, provider differences, and live-test coverage.
- [Logging](logging/index.md) — structured logging, Microsoft logging adapters, OpenTelemetry, serializers, and log storage.
- [Utils](utils/index.md) — shared helper scope, transitive coupling, compatibility risks, and legacy limitations.
- [Architecture decisions](decisions/README.md) — index for concrete decision records; none are currently recorded.

## Reading paths

For adding or configuring a provider, read [Factories](factories/index.md) first and then the relevant subsystem page. For evaluating a public or
package-level change, begin with [Repository architecture](architecture.md), then check the subsystem's behavioral and test contracts. For external
integration support, use the provider capability and verification sections; a project existing in the solution does not by itself establish complete
runtime support.

These pages describe architecture rather than list every public API. Source and executable tests remain authoritative when a detail is not covered.
`AGENTS.md` separately contains instructions for coding agents and is not part of the consumer-facing architecture documentation.
