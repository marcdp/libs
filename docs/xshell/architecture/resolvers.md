# Resolvers

This document describes the logical resource resolver used by the XShell loader.

## Status

Draft.

## Resource names

Resolver configuration uses keys such as `resolver.component:<pattern>` and values containing a destination plus semicolon-delimited metadata. Patterns may include named placeholders in braces.

## Resolution

The resolver removes query and fragment suffixes for matching, substitutes captured placeholder values into the destination, and returns the matching definition, resolved source, and unresolved path value.

## URL resolution

HTTP(S), protocol-relative, and leading-slash URLs follow separate paths from logical resources. Exact base-path behavior should be verified before it is treated as a stable public contract.

## TODO

TODO: Document precedence, escaping, duplicate patterns, and malformed definitions after tests define those cases.

## Related documentation

- [Architecture](index.md)
- [Loaders](loaders.md)
- [Modules](modules.md)

