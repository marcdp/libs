# ADR-0001: Keep QueueFsDir coordination bounded by filesystem semantics

## Status

Accepted

## Context

`QueueFsDir` stores messages in `tmp/`, `new/`, and `cur/`. It publishes a completed message by moving it from `tmp/` to `new/` and claims a
message by moving it from `new/` to `cur/`.

During Queue verification, a test attempted to guarantee that two independent `QueueFsDir` instances could never claim the same message. That
expectation raised an ownership question: Queue can control concurrency within one object, but independent instances and processes coordinate only
through the supplied `IFilesystem`. The filesystem contract does not define portable atomic competitive-move semantics.

## Options considered

### Option A: Add process-wide synchronization

Use a static `SemaphoreSlim`, global lock dictionary, or process-wide queue registry to coordinate independent instances in one process.

### Option B: Strengthen the generic filesystem contract

Require every `IFilesystem` provider to expose an atomic move guarantee strong enough for competing claim attempts.

### Option C: Bound Queue coordination by filesystem semantics

Serialize concurrent reads within a single `QueueFsDir` instance and make coordination among independent instances or processes dependent on the
supplied filesystem.

## Decision

Option C was chosen. Process-wide synchronization would coordinate only readers in one process while appearing to offer a broader guarantee. A
portable competitive atomic-move contract is not currently owned by `IFilesystem`, so Queue must not imply that it provides distributed or
cross-process coordination. The test and documented contract were narrowed to the guarantee Queue actually owns instead of adding hidden global
synchronization.

## Consequences

### Positive

- Queue remains simple and contains no global mutable synchronization state.
- The ownership boundary between Queue and the filesystem provider remains explicit.
- Tests verify same-instance coordination without implying a distributed guarantee.

### Trade-offs

- Independent Queue instances follow provider-specific filesystem concurrency semantics.
- Queue provides no exactly-once claim or multi-process coordination guarantee.
- Consumers that require broker semantics need a different abstraction or implementation.

## Related evidence

- [Queue architecture and verification boundary](../queues/index.md)
- [Support and status](../support.md)
- [Repository verification](../verification.md)
- [`DProjects.Queues.Test`](../../test/DProjects.Queues.Test/)
