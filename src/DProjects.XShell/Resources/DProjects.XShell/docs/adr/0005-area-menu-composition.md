# ADR-0005: Area and Menu Composition

## Status

Partially implemented: Area definitions and per-Area menu composition exist; non-empty prefix navigation has unresolved routing limits.

## Context

Reusable modules should not need to know whether an application presents them under customers, inventory, or another navigation context.
The earlier Area-keyed menu shape coupled module definitions to one application's placement and obscured resource ownership.

## Decision

The root application owns `xshell.areas.default` and `xshell.areas.definitions.<id>`. Each Area defines a navigation prefix and lists
participating canonical module ids. A module owns reusable contributions to arbitrary named menu slots under
`modules.<module-id>.menus.<menu-name>`. Menus assembles effective menus for each Area in the listed module order. A module may participate in
multiple Areas while retaining one runtime module instance. An Area prefix denotes navigation context; `/_assets/<module-id>/...` denotes
the resource namespace.

Root ownership is a composition convention rather than an enforced fragment boundary: the configuration merger accepts `xshell` settings from
imports. Area membership does not create module imports or routes.

## Consequences and open work

The runtime constructs Areas and effective menus, including current-Area menu lookup. Bootstrap currently rewrites leading-slash Area prefixes as
module resource paths, and the page resolver has no Area-prefix stripping rule. Navigation signals an Area change after page loading, too late
for the loading page's default breadcrumb lookup. Checked-in consumers still use an obsolete Area event name and the old `"main"` menu slot.
These limits must be resolved before non-empty prefixes are described as operational.

See [Areas](../subsystems/areas.md), [Navigation](../architecture/navigation.md), and [Configuration](../architecture/configuration.md).
