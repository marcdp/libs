# Navigation Architecture

This document summarizes the navigation service that maps browser URLs to XShell page stacks.

## Status

Draft.

## Current mode

The default configuration selects hash navigation with the `#!` prefix. Hash changes are parsed into a root page and optional stack entries, then synchronized with top-level `x-page` elements.

## Navigation targets

The service currently recognizes top, stack, dialog, and embedded page targets, with `auto` selecting a target from the current page context.

## Path mode

The configuration and URL-building code mention path mode, but initialization currently throws an error because path mode is not implemented.

## TODO

TODO: Specify history behavior, query-value typing, malformed navigation metadata, and page-stack restoration after those behaviors have tests.

## Related documentation

- [Architecture](index.md)
- [Navigation ADR](../adr/navigation.md)

