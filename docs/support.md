# Support and status

Project existence does not imply equal maintenance, compatibility expectations, or verification depth. This page classifies the subsystem families
that are documented as public architectural surfaces.

The status terms are deliberately restrained:

- **Maintained** means the family is treated as a deliberate compatibility and behavioral surface, with active implementation and test evidence.
- **Experimental** means the design may still change materially and should not be assumed stable without inspecting its current contracts.
- **Legacy / compatibility** means existing consumers and behavior matter, but breadth, historical coupling, or uneven coverage makes conservative use
  and change preferable.

Maintained does not mean that every provider supports every operation or is exercised against live infrastructure in each CI run.

## Current classifications

| Family | Status | Verification | Important limitations |
| --- | --- | --- | --- |
| Filesystem | Maintained | Shared contracts and provider tests | External providers need services or credentials; capabilities vary. |
| Database | Maintained | Shared/provider tests and selected live CI | Capabilities vary; full live coverage needs infrastructure. |
| Logging | Maintained | Sink, serializer, adapter, lifecycle, and OpenTelemetry tests | Background logging is buffering, not durable delivery. |
| Log Storage | Maintained | Focused query, tail, retention, and parsing tests | Tail and retention vary; no shared provider contract suite. |
| Factories | Maintained | Core dispatch and provider-use tests | Configuration, assembly, secret, and dependency errors are runtime concerns. |
| Utils | Legacy / compatibility | Broad but uneven focused tests | Large compatibility surface; portability and completeness vary by helper. |

No documented family is currently classified as Experimental. That label remains available for a future surface whose contracts are intentionally
still evolving rather than forcing it into Maintained or Legacy / compatibility.

## Interpreting provider support

Provider boundaries are part of the design. An explicit `NotSupportedException` can describe an intentional capability difference rather than an
unfinished maintained subsystem. Callers should use provider capability APIs where available and still handle operation failures because support can
depend on path, backend state, credentials, or server features.

Integration coverage also varies. The normal suite exercises reusable behavior without external resources; credential- or service-dependent tests
are marked as integration tests. CI adds selected live-provider validation, but that evidence should not be generalized to every backend or runtime.

For the evidence behind these classifications, see [Repository architecture](architecture.md), [Filesystem](filesystem/index.md),
[Database](database/index.md), [Logging](logging/index.md), [Factories](factories/index.md), and [Utils](utils/index.md).

Return to the [documentation index](index.md) or [root README](../README.md).
