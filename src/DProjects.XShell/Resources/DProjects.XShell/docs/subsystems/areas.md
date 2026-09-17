# Areas

Areas group top-level navigation destinations and the main-menu entries associated with them.

## Status

Draft.

## Runtime model

The `Areas` service discovers names from keys beneath `xshell.areas`. For each name it projects the remaining keys into an area object, adds `id`, and
applies defaults for `label`, `order`, and `default`. It also creates the path prefix `/<area-name>/` used by the current area-resolution implementation.

Areas are sorted with the default area first, then by numeric `order`, then by `label`. `getDefaultArea()` returns the area marked as default or, if none
is marked, the first sorted area.

## Confirmed configuration

| Key | Current use |
| --- | --- |
| `xshell.areaDefault` | Names the default area when the area's own `default` value is not truthy. The framework default is `main`. |
| `xshell.areas.<name>.default` | Marks an area as the default. |
| `xshell.areas.<name>.label` | Supplies its display label and final sort key. Defaults to the area name. |
| `xshell.areas.<name>.order` | Supplies its numeric sort order. Defaults to `0`. |
| `xshell.areas.<name>.home` | Supplies the initial navigation target when this is the default area and the browser has no navigation hash. |
| `xshell.areas.<name>.icon` | Is exposed on the area object and used by the checked-in area-selection page. |
| `xshell.areas.<name>.description` | Is exposed on the area object and used by the checked-in area-selection page. |

Application configuration can define these keys directly. A module can contribute them globally, for example
`global.xshell.areas.help.home`; bootstrap removes `global.`, substitutes the module name where requested, and normalizes a leading-slash value beneath
the module's asset path.

## Initial and current area navigation

In hash mode, navigation restores the current hash when one exists. Otherwise it gets the default area and navigates to that area's `home` value. The
current implementation therefore expects a usable home value for the selected default area, although no formal validation or required-field schema exists.

After a top-level page loads, navigation emits `xshell:navigation:end`. The Areas service listens for that event, attempts to match the page source against
area prefixes, and emits an area-change event when the resolved name changes.

## Menus

Module menu keys use the shape `menus.<menu-name>.<area-name>`. During menu construction, the area segment is copied to each menu item. For the `main`
menu, `Menus.getMenuMain()` obtains the current page's menu breadcrumb and returns entries whose area matches the breadcrumb's area. A `menus.main` entry
without an explicit area segment defaults to area `main`.

This relationship is configuration-driven: Areas does not build menus, and Menus does not choose the default area's home page.

## TODO

TODO: Reconcile `id` and `name` in current-area resolution. The constructor assigns `area.id`, but `resolveAreaName()` and `getCurrentArea()` currently
read `area.name`, so reliable current-area tracking cannot yet be documented as a stable contract.

TODO: Reconcile the emitted `xshell:area:change` event with the checked-in area page, which listens for `xshell:area:changed`.

TODO: Define validation for missing default-area `home` values and clarify whether area prefixes are configurable.

## Related documentation

- [Subsystems](index.md)
- [Navigation Architecture](../architecture/navigation.md)
- [Configuration Architecture](../architecture/configuration.md)
- [Application Specification](../specifications/application.md)
