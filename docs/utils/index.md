# Utils

`DProjects.Utils` is a broad `netstandard2.0` compatibility package of mostly static helpers. It has no abstraction/core/provider split and sits low
in the dependency graph, so a small behavioral change can affect otherwise unrelated packages.

## Scope and dependency cost

The assembly combines three broad concerns:

- value conversion, strings, dates, encodings, hashes, paths, URLs, and serialization helpers;
- stream, archive, file, network, platform, and process operations;
- sync/async bridges plus configuration, dependency-injection, command-line, and schedule helpers.

It depends on `DProjects.DataTypes` and several Microsoft configuration, dependency-injection, JSON, encoding, and configuration-manager packages.
Utils is therefore not a dependency-free primitives layer: consumers inherit a meaningful transitive package surface.

Some helpers participate in cross-subsystem contracts. `PathUtils` shapes virtual filesystem paths, `UrlUtils` supports factory and provider URL
handling, `AsyncUtils` bridges execution models, and conversion/encoding helpers affect persisted representations. Those semantics should be changed
with the same compatibility care as a domain contract.

## Architectural boundary

A helper belongs in Utils only when its semantics are genuinely shared and require no domain abstraction, provider dependency, or owned lifecycle.
Generic path and URL mechanics fit that boundary; filesystem containment, protocol schemas, log serialization, and database type mapping remain with
their owning subsystems.

This distinction prevents a convenient shared package from becoming the repository's hidden policy layer. A domain-local helper is preferable when
its meaning belongs to one subsystem, even if similar mechanics exist elsewhere.

## Compatibility and trade-offs

Public utility methods are library API. Case sensitivity, culture, time kind, path normalization, wildcard grammar, URL encoding, byte order,
serialization text, and exception categories can all be observable. Signature-preserving cleanup can still be a breaking change.

The main limitations are architectural rather than a catalogue of individual incomplete branches:

- `AsyncUtils.RunSync` and async-to-sync enumeration block a thread; they are compatibility bridges, not substitutes for native async I/O.
- Platform, network, and I/O helpers have uneven runtime and cross-platform guarantees. Inspect the implementation and focused tests before treating
  one as a supported portable capability.
- Hash helpers retain MD5 and SHA-1 for compatibility alongside stronger algorithms. The weaker choices should not be selected for authentication or
  new security-sensitive designs.
- Coverage and completeness vary in legacy and platform-oriented helpers. Project existence and solution-wide compilation do not establish every
  helper as a supported cross-platform abstraction.

These constraints argue for keeping Utils stable and narrow rather than adding domain policy to an already broad compatibility surface.

## Verification

`DProjects.Utils.Test` provides focused coverage for core conversion, string, path, URL, stream, encoding, archive, hashing, scheduling, and file
behaviors. Atomic file-write tests also cover replacement, create-only behavior, encoding, cancellation, and temporary-file cleanup.

Coverage is less consistent around environment-dependent and platform-specific helpers. For those APIs, the relevant implementation and tests are
the appropriate evidence; the repository-wide non-integration suite is not a portability guarantee.

## Related documentation

- [Support and status](../support.md)
- [Repository architecture](../architecture.md)
- [Factories](../factories/index.md)
- [Filesystem](../filesystem/index.md)
- [Documentation index](../index.md)
