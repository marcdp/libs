# ADR-0003: Navigation

## Status

Hash and path modes are implemented; declarative intent dispatch remains planned.

## Context

XShell maps browser location to Pages. One Navigation subsystem serves hash and path URL modes through the same Page and stack infrastructure.

## Decision

Hash mode uses the configured `#!` prefix and needs no server path fallback. Path mode uses browser paths, History API updates, and `popstate`; it
requires server fallback for direct deep links. `Extensions.UseXShell()` supplies that fallback within the application base path. The checked-in
framework configuration currently selects path mode. Both modes decode the same Page stack representation.

Public navigation intents should let modules request capabilities such as `customer.detail` without depending on another module's page URL or menu
configuration. Intent registration and dispatch are not implemented yet.

## Open work

The `x-page` `replace` and `navigate` event handlers remain debugger-marked and hash-specific; complete them without changing the core mode-neutral
Navigation API. Define intent mapping and missing-target failures. Keep layout presentation choices separate from route resolution.

See [Navigation](../architecture/navigation.md) and [Pages](../architecture/pages.md).
