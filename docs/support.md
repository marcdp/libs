# Support and status

Project existence does not imply equal maintenance, compatibility expectations, or verification depth. This page classifies the subsystem families
that are documented as public architectural surfaces.

The status terms are deliberately restrained:

- **Maintained** means the family is treated as a deliberate compatibility and behavioral surface, with active implementation and meaningful
  verification evidence.
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
| Streams | Maintained | Focused behavior and lifecycle tests | Older wrappers and compositions have uneven coverage. |
| XVault reader | Maintained | Canonical cross-format fixtures, hostile inputs, supported configuration registration | Read/decrypt only; no authoring or management; known versions only. |
| Cache | Maintained | Filesystem persistence, metadata, expiration, cleanup, get-or-create, cancellation, null-provider, and factory tests | Filesystem-backed semantics; no distributed coordination, capacity, or eviction guarantees. |
| Crypto | Legacy / compatibility | Focused vectors and round trips | V1 preserves APIs and persisted formats; new guarantees belong in v2. |
| Mail | Maintained | Recipient expansion, DB enqueue, EML/BCC behavior, cancellation, failure, cleanup, null-sender, and factory tests | Database enqueue is not delivery confirmation; downstream delivery and retained connection ownership are outside the contract. |
| Queues | Maintained | Filesystem write/read, claims, duplicate-claim prevention, delete, purge, cancellation, concurrency, null-provider, and factory tests | No FIFO, redelivery, lease, durable acknowledgement, or distributed/multi-process guarantee. |
| Repositories | Maintained | JSON/YAML/YFM CRUD, listing/filtering, malformed data, format and ID validation, path safety, and cancellation tests | One filesystem-backed implementation; no transaction or repository-level concurrency guarantee. |
| Secrets | Maintained | Sealing, password rotation, CRUD, encrypted JSON persistence, failure, cancellation, null-manager, and factory tests | External/platform providers are not exercised broadly; provider security properties are not uniform. |
| Utils | Maintained | Broad focused tests across commonly used conversion, text, path, URL, stream, archive, hashing, scheduling, and file helpers | Intentional cross-cutting surface; platform and environment-dependent guarantees vary by helper. |

No documented family is currently classified as Experimental. That label remains available for a future surface whose contracts are intentionally
still evolving rather than forcing it into Maintained or Legacy / compatibility.

## Interpreting provider support

Provider boundaries are part of the design. An explicit `NotSupportedException` can describe an intentional capability difference rather than an
unfinished maintained subsystem. Callers should use provider capability APIs where available and still handle operation failures because support can
depend on path, backend state, credentials, or server features.

Integration coverage also varies. The normal suite exercises reusable behavior without external resources; credential- or service-dependent tests
are marked as integration tests. CI adds selected live-provider validation, but that evidence should not be generalized to every backend or runtime.

For the evidence behind these classifications, see [Repository architecture](architecture.md), [Filesystem](filesystem/index.md),
[Database](database/index.md), [Logging](logging/index.md), [Factories](factories/index.md), [Streams](streams/index.md),
[XVault interoperability](xvault/index.md),
[Cache](cache/index.md), [Crypto](crypto/index.md), [Mail](mail/index.md), [Queues](queues/index.md), [Repositories](repositories/index.md),
[Secrets](secrets/index.md), and [Utils](utils/index.md). The cross-cutting [Verification](verification.md) page explains how to interpret test and CI
evidence.

Return to the [documentation index](index.md) or [root README](../README.md).
