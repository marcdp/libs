# ADR-0005: Area and Menu Composition

## Status

Implemented for hash and path navigation.

## Context

Reusable modules should not need to know whether an application presents them under customers, inventory, or another navigation context.
The earlier Area-keyed menu shape coupled module definitions to one application's placement and obscured resource ownership.

## Decision

The root application owns `xshell.areas.default` and `xshell.areas.definitions.<id>`. Each Area defines a navigation prefix and lists
participating canonical module ids. A module owns reusable contributions to arbitrary named menu slots under
`modules.<module-id>.menus.<menu-name>`. A menu item may have an optional friendly `path` and a canonical XShell navigation target `href`. Areas
assembles effective menus for each Area in the listed module order and applies the Area prefix to both local values. The first navigation item
marked `default: true` in depth-first traversal determines its home through `path || href`. A module may participate in multiple Areas while
retaining one runtime module instance. An Area prefix denotes navigation context; `/_assets/<module-id>/...` denotes the resource namespace.

Root ownership is a composition convention rather than an enforced fragment boundary: the configuration merger accepts `xshell` settings from
imports. Area membership does not create module imports or routes.

## Consequences and open work

The runtime constructs canonical modules first, then Areas composes effective menus and homes before Navigation starts. Menu UI uses
`path || href`; Navigation resolves a friendly path to canonical Area-aware href before passing it to `x-page`. `x-page` strips a matched Area
prefix only for module resource lookup. Breadcrumb lookup uses the canonical href's Area before Navigation emits an Area change. Dynamic menu
updates retain `xshell:menus:change`; Area changes use `xshell:area:change`.

See [Areas](../subsystems/areas.md), [Navigation](../architecture/navigation.md), and [Configuration](../architecture/configuration.md).
