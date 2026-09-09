# Factories

The factory subsystem selects implementations from URL-like configuration while keeping callers dependent on a requested contract rather than a
provider class. It consists of the `netstandard2.0` packages `DProjects.Factories.Abstractions` and `DProjects.Factories`.

## Boundaries and dependency direction

`DProjects.Factories.Abstractions` defines `IFactory<T>`, the two `IFactoryByUrl` contracts, the empty `IAssembly` marker, and protocol metadata
attributes. It depends only on dependency-injection abstractions and asynchronous-interface support. `DProjects.Factories` implements registration,
discovery, dispatch, aliases, handlers, configuration metadata, and secret substitution. The implementation depends on the abstraction package,
`DProjects.Secrets.Abstractions`, and `DProjects.Utils`.

Provider projects depend on these packages and implement one of:

```csharp
IFactoryByUrl<T>
IFactoryByUrl<T, TArgument>
```

The second form supplies creation-time context in addition to the URL. This allows a domain to pass caller-owned context without adding it to the URL.

## Registration and dispatch

`AddFactoryByUrl` builds a configuration and registers a transient dispatcher in `IServiceCollection`. A configuration can add:

- a factory type explicitly;
- all assignable factory types from an assembly;
- an alias that expands to another URL;
- a delegate handler for a scheme.

Assembly discovery is explicit: `AddFactoriesFromAssembly` scans the supplied assembly. The generic overload accepts an `IAssembly` marker so a
provider can expose its assembly without making callers name an arbitrary implementation type. A marker does not register anything by itself.

Discovered factory types are read through `ProtocolAttribute` and registered as keyed transient services. At creation time the dispatcher:

1. expands a configured alias;
2. normalizes a Windows drive path to a `file:///` URL in the one-argument dispatcher;
3. appends `:` when a non-empty value has no scheme delimiter;
4. substitutes `${secret:name}` tokens through `ISecretProvider`;
5. returns the unkeyed default `T` for an empty URL when one is registered;
6. invokes a matching handler, or resolves the keyed provider factory and passes it the URL.

Scheme matching is case-insensitive. An unknown scheme raises `ArgumentException`; a referenced missing secret raises `KeyNotFoundException`.
Objects created by a handler or provider and implementing `IDisposable` are disposed with the dispatcher.

Aliases are configuration names rather than schemes. The one-argument dispatcher also supports `alias?key=value`: its query values replace values
in the configured target URL. This behavior is not present in the two-argument dispatcher.

## Protocol metadata and compatibility

`ProtocolAttribute` supplies the dispatch name and description. `ProtocolUsageAttribute`, `ProtocolExampleAttribute`, help, parameter, and
configuration attributes describe how a protocol is configured; `FactoryByUrlProtocol` reads that metadata and can render descriptive output.
Only the protocol name participates in routing.

Protocol names and URL syntax are effective public contracts. Changing a scheme, query key, alias expansion rule, default, or parsing convention can
break persisted configuration even when the C# API remains unchanged. Provider factories should therefore own their parsing rules, and new providers
should be added by registration rather than by a central type switch.

The design trades compile-time construction for runtime configuration. It permits independently packaged providers and keeps their dependencies out
of the shared contract, but malformed URLs, incomplete metadata, and missing dependency-injection registrations surface only at registration or
creation time. Duplicate protocol names are not explicitly rejected, so registration must keep them unique. Assembly scanning also considers every
assignable type; provider assemblies consequently expose concrete, attributed factory classes rather than relying on implicit plugin loading.

## Verification and current limitations

`DProjects.Factories.Test` verifies assembly discovery and both `IFactoryByUrl` forms. Its tests cover aliases, colon normalization, secret
substitution, empty-URL defaults, and rejection of unknown schemes. Other subsystem contract tests exercise the mechanism with real provider
assemblies, including filesystem, database, logging serialization, and log-storage factories.

The repository contains more configuration surface than the focused factory tests cover. In particular, handler lifetimes, keyed alias resolution,
Windows-path normalization, alias query replacement, protocol metadata rendering, duplicate protocol handling, and disposal are not directly asserted
by `DProjects.Factories.Test`. The generic `DependencyInjectionFactory<T>` exists but automatic registration is commented out, so
`dependency-injection:` should not be treated as a generally enabled protocol.

## Related documentation

- [Repository architecture](../architecture.md)
- [Filesystem](../filesystem/index.md)
- [Database](../database/index.md)
- [Logging](../logging/index.md)
- [Documentation index](../index.md)
