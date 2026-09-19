# ADR-0003: Navigation

## Status

Partially implemented: hash mode is current; path mode and public intents are planned.

## Context

XShell maps browser location to Pages. One Navigation subsystem should serve hash and path URL modes through the same page infrastructure. Earlier
architecture prose described path mode as available, but `Navigation.init()` still throws for it.

## Decision

Keep hash navigation as the current mode. Its default prefix is `#!`; it needs no server route fallback. Path mode should use browser paths and
history and requires server fallback for deep links. It must not be reported as operational until initialization and history restoration work.

Public navigation intents should let modules request capabilities such as `customer.detail` without depending on another module's page URL or menu
configuration. Intent registration and dispatch are not implemented yet.

## Open work

Specify path-mode base paths, direct loads, refresh, and `popstate` behavior. Define intent mapping and missing-target failures. Keep layout
presentation choices separate from route resolution.

See [Navigation](../architecture/navigation.md) and [Pages](../architecture/pages.md).
