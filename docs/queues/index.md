# Queues

The queue family provides a small asynchronous message-store contract and filesystem/null implementations. It is intentionally documented in terms of
observable repository behavior; common broker terminology such as exactly-once, at-least-once, visibility timeout, or durable ordering does not apply
unless the code establishes it.

```text
DProjects.Queues
        ↓
DProjects.Queues.Abstractions
```

The concrete package depends on filesystem abstractions, stream framing utilities, factories, and logging used by filesystem moves.

## Contract and message representation

`IQueue` exposes `WriteAsync`, `ReadAsync`, `DeleteAsync`, and `PurgeAsync`. `Message` holds a byte-array body plus HTTP-style headers; its text
constructor records UTF-8 content type, and text decoding honors a declared charset. A queue assigns an `x-id` header during a filesystem write.

The contract is claim-oriented rather than callback-oriented: reading returns a message, and the caller separately requests deletion. It contains no
nack, lease renewal, delivery count, batch, priority, or transactional operation.

## Filesystem claim behavior

`QueueFsDir` maintains `tmp`, `new`, and `cur` directories. A write serializes headers and body into `tmp`, then moves the completed file into `new`.
A read takes the first file returned by filesystem enumeration, moves it from `new` to `cur`, then deserializes it. That move is the implemented claim
boundary, subject to the atomicity and concurrency semantics of the supplied filesystem.

Filenames contain a timestamp and random identifier. The contract and tests do not establish ordering, and enumeration order may vary by filesystem;
the timestamp-shaped name must not be treated as a FIFO guarantee.

`ReadAsync` first attempts an immediate claim, then treats `waitTimeout` as a polling duration in milliseconds with intervals of at most one second.
A zero timeout therefore performs one immediate attempt. The polling delay observes cancellation. This is bounded polling, not broker push delivery
or a visibility timeout.

`DeleteAsync` finds the claimed file by its `x-id` suffix and removes it from `cur`; direct tests verify deletion of the message returned by a read.
Claimed files still have no timeout or redelivery path, so an abandoned `cur` item is not automatically made visible again. Deletion is a filesystem
cleanup operation, not a durable broker acknowledgement guarantee.

`PurgeAsync` deletes matching files from all three directories. There is no selective purge or recovery distinction between staged, available, and
claimed messages.

## Providers, lifecycle, and failures

Factories register `fs-dir:`, `file:`, and `null:` protocols. `fs-dir:` uses an injected filesystem and path; `file:` creates a filesystem and roots
the queue at `/`; `null:` discards writes, returns no messages, and treats delete/purge as successful no-ops.

Filesystem exceptions and parsing failures propagate. A move race between readers is not translated into a queue-specific empty result. `IQueue` is
disposable, but current implementations do no disposal work and do not dispose the filesystem, including one created by the `file:` factory. Ownership
of that dependency is therefore not closed by the queue contract.

## Verification boundary

`DProjects.Queues.Test` verifies body/header round trips, generated IDs, immediate and bounded empty reads, claims that prevent a second read,
deletion, purge and continued reuse, pre-canceled operations, null-queue behavior, and factories. Concurrent readers on one queue and independent
queue objects sharing a `FilesystemMem` instance are also tested to ensure one message is claimed once in those configurations.

That evidence is specific to the filesystem workflow under test. It does not establish FIFO, leases, redelivery, exactly-once delivery, durable
acknowledgements, crash recovery, or multi-process/distributed coordination. Ordering remains unspecified.

See [Support and status](../support.md), [Filesystem](../filesystem/index.md), and [Factories](../factories/index.md). Return to the
[documentation index](../index.md).
