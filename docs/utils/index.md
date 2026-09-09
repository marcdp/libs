# Utils

`DProjects.Utils` is a broad `netstandard2.0` package of reusable, mostly static helpers. Unlike the filesystem, database, and logging families, it
has no abstraction/core/provider split and no factory extension mechanism. Its architectural significance comes from its wide use low in the
dependency graph: changes to utility behavior can affect many otherwise unrelated packages.

## Current scope

The source groups several kinds of functionality in one assembly:

- value conversion, enum/reflection helpers, byte/hex/base encodings, hashes, random values, and date/time formatting;
- strings, virtual paths, URLs, MIME types, HTML/XML/JSON, encodings, and console highlighting;
- streams, files, ZIP/GZip, HTTP headers, network and authentication helpers;
- sync/async collection bridges and synchronous execution of asynchronous delegates;
- configuration variable replacement, dependency-injection/configuration helpers, and command-line processing;
- environment, process, performance, browser, clipboard, Windows, and Linux helpers;
- schedule-string evaluation.

The package depends on `DProjects.DataTypes` and on Microsoft configuration and dependency-injection abstractions, JSON, code-page encoding, and
configuration-manager packages. This means it is not a dependency-free “primitives” library: a project reference to Utils imports that package set
and the DataTypes project transitively.

At least nineteen production projects directly reference Utils, including several `*.Abstractions` projects. Observable cross-subsystem contracts
therefore include `PathUtils` virtual-path behavior in filesystems, `UrlUtils` parsing and redaction in factories/providers, `AsyncUtils` sync/async
bridges, `ConvertUtils` data coercion, and string/date/hash encodings used in persistence and protocols.

## Architectural boundary

A helper belongs here only when its semantics are genuinely shared and do not require a domain abstraction, provider dependency, or lifecycle of its
own. Existing use illustrates the practical boundary:

- generic slash-path manipulation is shared in `PathUtils`, while root containment and filesystem mutation remain in filesystem implementations;
- generic URL/query operations are shared in `UrlUtils`, while each protocol factory owns its URL schema and defaults;
- byte, stream, encoding, and hash mechanics are shared, while log serialization and database type mapping remain in their domains;
- `AsyncUtils` provides mechanical adapters, while the filesystem and database layers define the behavioral relationship between their own sync and
  async APIs.

This boundary matters because a utility reference points inward from many packages. Moving domain policy into Utils would invert ownership: the
shared package would become a hidden central layer that every domain must coordinate through. A small domain-local helper can be preferable when its
meaning is specific, even if its implementation resembles code elsewhere.

## Compatibility and trade-offs

All public utility methods are library API. Their edge cases—including case sensitivity, culture, time kind, path normalization, wildcard grammar,
URL encoding, query defaults, byte order, serialization text, and exception type—may be consumed outside this repository. “Cleanup” changes can
therefore be breaking without changing a signature.

Static helpers are convenient and allocation-light for small transformations, but they make dependencies implicit and offer no provider capability
negotiation. Environment and I/O helpers also combine cross-platform APIs with runtime-specific branches. Targeting `netstandard2.0` establishes an
API availability floor; it does not guarantee identical behavior on every operating system or runtime.

Several current behaviors are important limitations rather than supported extension points:

- `AsyncUtils.RunSync` and async-to-sync enumeration block a thread. They are compatibility bridges, not substitutes for native async I/O.
- `FileUtils.ReadTextFileAsync` wraps synchronous reading in a completed task; it is not asynchronous file I/O.
- `FileUtils` HTTP/HTTPS reads reach active `NotImplementedException` paths. HTTP reading is not a supported `FileUtils` capability.
- `ConvertUtils.To` has active `NotImplementedException` branches for some dictionary conversions. Its broad conversion surface must not be assumed
  total for every source/target pair.
- hash helpers expose MD5 and SHA-1 alongside SHA-2 and HMAC. Their availability does not make the weaker hashes suitable for authentication or new
  security-sensitive designs.
- schedule parsing is a repository-specific string grammar and directly constructs local or UTC `DateTime` values; it is not a timezone/calendar
  scheduling abstraction.

These legacy breadth and consistency issues are reasons to inspect a helper and its tests before reusing it, not reasons to enlarge the package by
default.

## Verification

`DProjects.Utils.Test` contains focused tests for arrays, async adapters, authentication headers, encodings, base64, bytes, conversion, dates, enums,
files, gzip/ZIP, hashes, hex, XML, paths, processes, reflection, schedules, streams, strings, and URLs. The strongest contract coverage is
concentrated in conversion, strings, paths, URLs, streams, encodings, archives, and hashing. Atomic file-write tests verify replacement, create-only
behavior, encoding, cancellation, and temporary-file cleanup.

Coverage is uneven. Several named test classes contain no active facts or theories, including clipboard, environment, exception, HTML, HTTP, JSON,
Linux, MIME, networking, and Windows tests. Commented tests also do not establish behavior. Platform integrations and the active unsupported branches
above should consequently be treated as limitations even though the repository-wide non-integration suite builds and exercises their assembly.

## Related documentation

- [Repository architecture](../architecture.md)
- [Factories](../factories/index.md)
- [Filesystem](../filesystem/index.md)
- [Documentation index](../index.md)
