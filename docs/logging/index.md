# Logging

The logging family has two related but separate flows: producing structured `LogEntry` values, and reading persisted entries through log storage.
Logging integrates bidirectionally with `Microsoft.Extensions.Logging`; storage deliberately exposes query and retention contracts rather than
acting as a logging sink itself.

## Package boundaries

`DProjects.Log.Abstractions` defines `ILog`, `ILogClient`, `ILogEntrySerializer`, `LogEntry`, and the repository log levels.
`DProjects.Log` implements filtering, clients, sinks, serializers, URL factories, and adapters for Microsoft logging. Both target `netstandard2.0`.

`DProjects.Log.OpenTelemetry` is a provider package. It depends on the core, the factory abstractions, and the official OpenTelemetry logging and OTLP
exporter packages. It contains the only active `IFactoryByUrl<ILog>` implementation for the `otlp` scheme.

Storage follows its own abstraction/core split. `DProjects.Log.Storage.Abstractions` defines `ILogStorage`, query/statistics types, and entry reader
and deserializer contracts. `DProjects.Log.Storage` implements filesystem-backed and null storage plus line deserializers. It depends on logging
contracts, filesystem abstractions, factories, and JSON support, but logging producers do not depend on storage.

There is a legacy dependency inconsistency in the current project graph: `DProjects.Log.Abstractions.csproj` references concrete `DProjects.Fs` and
`DProjects.Utils`, although its public logging contracts do not expose filesystem types and source inspection finds no filesystem usage in that
project. Consumers of the abstraction package therefore inherit a broader project/package dependency than the source API requires. This
documentation records the current boundary; it does not imply that new logging contracts should depend on concrete filesystem behavior.

## Entries, clients, and sinks

`LogEntry` is the shared record: timestamp, level, message, fields, tags, source, user, resource, span ID, and trace ID. `ILog` accepts complete
entries or level-specific calls. `ILogClient` adds configured context, message-template arguments, and a `Writed` event.

`LogClient` parses `{name}` message-template placeholders into a rendered message and structured fields. Template fields override configured fields
of the same name; metadata configured on the client is copied to each entry. The `Writed` event describes the entry submitted by the client even if a
downstream sink filters it.

`Log` is the reusable sink base. It applies minimum-severity filtering before `ProcessEntry` and can process synchronously or through a bounded
background queue. `ILog` has no flush or delivery-acknowledgement contract, and the queue can decline entries when full, so threaded logging is a
throughput trade-off rather than durable delivery.

Core sink protocols include `null`, `stdout`, `temp`, `file`, `fs-file`, and `fs-dir`. The filesystem forms either create a filesystem from the URL or
use an injected `IFilesystem`; serializer selection is itself protocol-based. Active serializers are JSON, raw, RAT, and an OTLP JSON representation.
Protocols, format names, level values, and serialized line shapes are compatibility boundaries for configured applications and stored logs.

## Microsoft logging interoperability

The repository supports both directions:

- `LogLogger` adapts an `ILog` sink to a Microsoft `ILogger`, preserving structured entry fields as named state.
- `LogClientLogger` implements `ILogClient` over `ILogger` and preserves native message templates as well as the DProjects `LogEntry` view.
- `DProjects.Log.Provider.Logger` and `Logger<T>` implement Microsoft `ILogger` over `ILog`.
- `LoggerProvider` implements `ILoggerProvider`; `AddLogProvider` registers it and creates the configured `ILog` from a URL.

The provider maps severity, category to source, `EventId` to fields, exceptions to detailed fields, structured state, and nested scopes. Field
precedence is adapter metadata, then outer-to-inner scopes, then explicit log state; `{OriginalFormat}` is not duplicated as a DProjects field.
Unsupported Microsoft `None` events remain disabled. Disposing the provider disposes its created `ILog` and prevents creation of further loggers.

This integration uses the standard .NET provider surface, so applications can place DProjects sinks in an existing logging pipeline rather than run a
parallel global logger. Conversely, DProjects clients can target any configured Microsoft provider.

## OpenTelemetry

`LogOtlp` is a `LogLogger` backed by an OpenTelemetry `LoggerFactory`. The URL factory accepts
`otlp://host:port?service=...&scope=...`, defaults the port to 4318, builds an HTTP/protobuf `/v1/logs` endpoint, includes formatted messages, parses
state values, and optionally adds a service resource.

A `LogOtlp` created from host/port owns and disposes its internal logger factory. A `LogOtlp` wrapping a caller-supplied `ILoggerFactory` does not own
it. This ownership distinction permits composition and is explicitly tested. The separate `otlp` entry serializer does not export telemetry; it only
serializes a line representation.

## Log storage

`ILogStorage` exposes asynchronous statistics, retention, query, and tail operations. Query bounds are inclusive, level is a minimum severity, message
matching is ordinal substring matching, and tag/source/user filters are exact. Enumeration is streaming and cancellation-aware.

Storage protocols are:

- `fs-file`: one selected log file, physical line order, final-record tail support, and optional following of appends; retention is unsupported;
- `fs-dir`: files selected by pattern and recursion, deterministic path then line order, whole-file age retention, and no tail/follow because there is
  no defined active file;
- `null`: empty statistics and query/tail streams with validated cancellation and no-op retention.

Deserializer protocols include `auto`, `classic`, `json`, `rat`, and `raw`. Automatic detection recognizes the supported structured forms; W3C and
CSV are explicitly unavailable. Malformed structured records fail enumeration without exposing the complete raw record in exception messages.

Directory retention uses provider-supplied modification timestamps, captures its cutoff at operation start, and deletes selected files best-effort.
Cancellation or I/O failure can leave partial completion, so retry safety is part of the contract. Single-file follow assumes append-only writes.

## Verification

`DProjects.Log.Test` covers severity filtering and mappings, exception/trace metadata, message-template edge cases, field precedence, scopes, event
IDs, provider disposal, serializer outputs, and both Microsoft logging adapter directions. OpenTelemetry tests verify unique factory discovery,
URL/default parsing, delivery into an in-process OpenTelemetry pipeline, and ownership of external logger factories without requiring a collector.

`DProjects.Log.Storage.Test` uses memory and controlled filesystems to verify deterministic querying, stats, inclusive filters, cancellation and early
disposal, malformed-record behavior, tailing large UTF-8 files, live append following, retention boundaries and partial completion, factories, and
each explicit unsupported operation. These are focused implementation tests rather than a reusable abstract storage contract suite.

## Related documentation

- [Filesystem](../filesystem/index.md)
- [Factories](../factories/index.md)
- [Repository architecture](../architecture.md)
- [Documentation index](../index.md)
