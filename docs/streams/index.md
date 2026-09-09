# Streams

`DProjects.Streams` is a standalone package of composable `System.IO.Stream` wrappers. It has no separate abstraction project or URL factory layer:
the public stream types are both the contract and implementation surface.

The package concentrates reusable capability, transformation, boundary, and ownership behavior used by other families such as cache and crypto.
Wrapper composition is useful precisely because each layer can add one concern, but consumers must preserve the capability and disposal contract of
the full chain.

## Capabilities and adapters

`InputStream` and `OutputStream` are non-seekable bases. Input streams advertise read-only behavior and reject writing; output streams advertise
write-only behavior and reject reading. Seeking, position, and length are unsupported unless a concrete wrapper explicitly supplies them. Unsupported
directions fail with `NotSupportedException`, allowing callers to distinguish deliberate capability limits from end-of-stream.

The package groups naturally into several roles:

- ownership/delegation wrappers: `DisposableStream`, `LeaveOpenInputStream`, and `LeaveOpenOutputStream`;
- bounded and composed reads: `PartialInputStream`, `LimitedInputStream`, and `CatInputStream`;
- encoding transforms: Base64 and hexadecimal input encoders/decoders, Base64 output encoding, and folded output;
- compression transforms: GZip input decompression and output compression;
- observation/buffering and sinks: byte counting, sponge buffering, and null input/output streams.

These categories describe composition roles, not interchangeable behavior. For example, Base64 encoding changes byte representation, folded output
adds line breaks, and GZip final bytes may be emitted only when the compressor is disposed.

## Ownership and `leaveOpen`

Most wrappers that retain an inner stream accept `leaveOpen`, defaulting to `false`. With the default, disposing the wrapper generally disposes its
source or destination; with `leaveOpen: true`, the caller remains responsible for the inner stream. The explicit `LeaveOpenInputStream` and
`LeaveOpenOutputStream` adapters exist to prevent a downstream wrapper from closing the original stream.

`DisposableStream` invokes a callback once and optionally closes its inner stream. Compression and buffered output wrappers may implement
`IAsyncDisposable`; callers should use the disposal form appropriate to their pipeline so buffered/final transform data is completed. Ownership is a
behavioral contract, not merely cleanup style.

## Bounded and partial reads

`PartialInputStream` exposes a read-only, non-seekable view beginning at an offset and bounded by a length, or unbounded when length is `-1`. It seeks
the source when possible and otherwise reads and discards bytes to reach the offset. Its own `Position` and `BytesRead` are relative to the view;
`Length` is available only when the source can report it, and `BytesLeft` is unavailable for an unbounded view.

Synchronous and asynchronous partial reads stop at the same boundary. Async offset skipping and reads propagate cancellation, including a
pre-canceled token without consuming the source. `LimitedInputStream` is an older maximum-byte wrapper with a different surface; callers should not
infer all of the hardened partial-view argument, position, and cancellation guarantees from it.

`CatInputStream` consumes several streams in sequence. Its disposal behavior applies to streams still retained by the concatenation and is controlled
by `leaveOpen`; it does not make the combined view seekable.

## Sync, async, and composition limits

Transform wrappers generally delegate synchronous and asynchronous operations to their inner stream and pass cancellation tokens where their
overrides accept one. A wrapper cannot add cancellation to synchronous work in an underlying stream, and not every older override forwards tokens in
the same way. Capability flags and actual supported methods remain authoritative for each composed chain.

Base64, hexadecimal, folding, and GZip wrappers are streaming transforms rather than random-access codecs. They deliberately reject incompatible
read/write/seek operations. Some older edge methods remain uneven—for example, the GZip decompression wrapper's `Flush` throws
`NotImplementedException` even though it is a read path—so consumers should avoid treating every `Stream` member as meaningful for every wrapper.

## Verification

`DProjects.Streams.Test` contains focused sync/async tests for Base64 transforms, byte counting, concatenation, disposal, folding, GZip, null streams,
partial ranges, and sponge buffering. Shared base-class assertions verify read/write/seek rejection. Partial-stream tests additionally cover seekable
and non-seekable sources, boundary reads, invalid arguments, cancellation during offset skipping, pre-cancellation, length semantics, and `leaveOpen`.

Coverage is substantial but is not a proof for every composition order or all malformed transform inputs. Hex wrappers, `LimitedInputStream`, and some
ownership combinations have less direct evidence than the hardened partial/Base64 paths. See [Verification](../verification.md) and
[Support and status](../support.md).

Return to the [documentation index](../index.md).
