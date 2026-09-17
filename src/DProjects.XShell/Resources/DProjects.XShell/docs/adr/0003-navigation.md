# ADR-0003: Navigation

This record captures design considerations for hash-based and path-based XShell navigation.

## Status

Draft; hash mode is implemented and configured by default, while path mode is incomplete.

## Context

XShell maps browser location to one or more page elements and encodes optional navigation metadata and stacked pages in the URL.

## Hash-based navigation

The current implementation listens for hash changes, defaults to the `#!` prefix, and synchronizes a parsed page stack with the DOM. Hash routing can operate without server fallback configuration but places application location after the fragment marker.

## Path-based navigation

URL construction contains path-mode branches and history API calls, but navigation initialization currently throws `Path mode is not implemented yet`. Server fallback, refresh, base-path, and popstate behavior remain unresolved.

## Decision considerations

The eventual choice must cover hosting requirements, deep links, browser history, application base paths, query and fragment semantics, and compatibility with existing URLs.

## TODO

TODO: Do not select or recommend a mode until path mode is implemented and both modes have equivalent navigation contract tests.

## Related documentation

- [Navigation Architecture](../architecture/navigation.md)
- [Application Specification](../specifications/application.md)
