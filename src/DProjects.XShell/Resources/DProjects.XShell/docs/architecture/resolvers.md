# Resolvers

This document describes the logical resource resolver used before XShell loads a resource.

## Status

Draft.

## Responsibility

The resolver determines what a logical reference represents. It matches the reference to configuration, substitutes pattern values, and returns the URL
and metadata needed by the loader. It does not fetch, import, parse, compile, or render the resource.

```text
logical resource reference
    -> resolver definition match
    -> resolved source URL, unresolved path, and loader metadata
    -> loader dispatch
```

## Definitions and resource names

Resolver configuration uses keys such as `resolver.component:<pattern>` and values containing a destination plus semicolon-delimited metadata. Patterns
may include named placeholders in braces. Bootstrap supplies framework definitions, accepts global module contributions, and generates conventional
definitions for each module's icons, layouts, components, pages, and JavaScript modules.

## Resolution

At runtime, `Resolver` reads the assembled `resolver.*` keys in configuration order. `resolve()` removes query and fragment suffixes for matching, uses the
first matching definition, substitutes captured placeholder values into the destination, and returns:

- `definition`, including the configured `loader`, `cache`, `module`, and `modulePath` metadata where present;
- `src`, the source URL used by the loader;
- `path`, the substituted destination before the resolver applies the current application pathname.

The original logical reference remains the loader's cache and diagnostic key. URL suffixes are removed only for resolver matching.

## URL resolution

`resolveUrl()` returns HTTP(S) and protocol-relative URLs directly. It prefixes a leading-slash URL with `document.location.pathname`; other values are
treated as logical resources and passed through `resolve()`. Module initialization uses this helper when loading configured stylesheets.

## Boundary with loading

The resolved definition tells `Loader` which resource-specific loader to use. The loader owns dynamic loader import, fetch/import execution, caching,
events, errors, and any later engine delegation. See [Loaders](loaders.md) for that part of the pipeline and a worked Markdown-page example.

## TODO

TODO: Document placeholder escaping, duplicate-pattern guarantees, and malformed definitions after tests define those cases. First-match behavior is
implemented, but configuration-order precedence is not yet declared as a stable public contract.

## Related documentation

- [Architecture](index.md)
- [Loaders](loaders.md)
- [Modules](modules.md)
- [Configuration](configuration.md)
