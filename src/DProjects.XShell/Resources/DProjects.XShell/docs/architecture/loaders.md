# Loaders

This document introduces XShell's dispatcher for resolved resources and its type-specific loader modules.

## Status

Draft.

## Loading flow

The loader resolves each logical resource, locates or imports the configured loader, invokes it with resource and application context, and waits for all requested resources to settle.

## Current loader types

The repository contains loaders for component JavaScript, icons, module JavaScript, and JavaScript, HTML, and Markdown pages. Resolver configuration selects the loader by name.

## Cache and registry

Definitions marked with `cache=true` can reuse an in-flight or completed result. The loader also maintains a read-only projection of resource, source, and status entries for diagnostic use.

## Errors and events

Load failures are collected into an aggregate loader error, while the event bus receives fetch, loaded, and error notifications.

## TODO

TODO: Specify clone behavior, retry behavior, and the exact ordering of results for mixed cached and newly loaded resources.

## Related documentation

- [Architecture](index.md)
- [Resolvers](resolvers.md)
- [Component Lifecycle](../subsystems/components/lifecycle.md)
