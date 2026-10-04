# ADR-0003: Navigation

## Status

Hash/path modes and route translation are implemented; declarative intent dispatch is outside V0.

## Context

XShell maps browser location to Pages. One Navigation subsystem serves hash and path URL modes through the same Page and stack infrastructure.

## Decision

Hash mode uses the configured `#!` prefix and needs no server path fallback. Path mode uses browser paths, History API updates, and `popstate`; it
requires server fallback for direct deep links. `Extensions.UseXShell()` supplies that fallback within the application base path. The checked-in
framework configuration currently selects path mode. Both modes decode the same Page stack representation.

Public navigation intents should let modules request capabilities such as `customer.detail` without depending on another module's page URL or menu
configuration. Intent registration and dispatch are not implemented yet.

## Open work

The legacy `x-page` `replace`/`navigate` event listeners are commented out, not active hash-specific paths.
V0 uses the mode-neutral Navigation API, routes, Page stacks, query replacement, and dialog/embed presentation.
Intent mapping and dispatch remain outside V0. Layout presentation is separate from route resolution.

See [Navigation](../architecture/120-navigation.md) and [Pages](../architecture/70-pages.md).
