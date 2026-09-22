# Areas

An Area is a navigation context composed from participating modules, including the effective menus contributed by those modules. Modules define
reusable menu contributions; Areas define application composition. A module may participate in several Areas or none, while Modules keeps one live
runtime instance per canonical module id. Navigation remains responsible for browser URLs, history, page stacks, and page activation.

## Configuration and ownership

```jsonc
{
    "modules": {
        "reports": {
            "menus": {
                "navigation": [{ "label": "Reports", "href": "/pages/report.js", "default": true }],
                "tools": [{ "label": "Export", "href": "/pages/export.js" }]
            }
        }
    },
    "xshell": {
        "areas": {
            "default": "customers",
            "definitions": {
                "customers": { "prefix": "/customers", "label": "Customers", "modules": ["reports"] },
                "inventory": { "prefix": "/inventory", "label": "Inventory", "modules": ["reports"] }
            }
        }
    }
}
```

Each `xshell.areas.definitions.<id>` entry may set `prefix`, `label`, `icon`, `modules`, and `order`. The `modules` array sets participation
and menu contribution order. A configured Area `home` is no longer used. Root ownership is an architectural convention: bootstrap can merge
`xshell` fragments from imported configurations. Arrays, including `area.modules`, concatenate during that merge.

Areas are presented with the configured default first, then by `order`, then `label`. An unknown configured default or duplicate normalized
prefix throws. With no configured default, the first sorted Area is used. The initial current Area is the default Area.

## Menus and home

Areas composes each named menu slot by visiting participating modules in `area.modules` order and copying items in declaration order. Each
effective item has its module id, Area id, label, icon, children, and effective href. Effective menu structures are separate for each Area;
participation creates no additional module instances. Unknown module ids cause a warning and are skipped.

Bootstrap first maps module-relative menu hrefs into `/_assets/<module-id>/...`. Areas then adds the Area prefix to local hrefs, leaving external
scheme URLs unchanged. The Area home is the effective href of the first `navigation` item marked `"default": true` in depth-first traversal.
Children are searched. If none is marked, `home` is null. On a fresh load at the selected mode's empty/root URL, Navigation requires the default
Area to have a home.

`xshell.areas.getMenu("navigation")` selects the current Area; `getMenu("navigation", "inventory")` selects one explicitly.
`getMenuitemBreadcrumb(href, areaId = null)` searches only that Area's effective menus, defaulting to the current Area.
`registerSource(name, source)` supports dynamic children. When a dependency event refreshes effective content, Areas emits
`xshell:menus:change`.

## Navigation context

An Area prefix marks a navigation context; `/_assets/<module-id>/...` marks resource ownership. A configured `"inventory"` or
`"/inventory/"` normalizes to `"/inventory"`. Empty or root prefixes normalize to `""`. Prefix matching uses complete segments and tries the
longest prefix first. Bootstrap preserves Area prefixes as navigation metadata. At page load, `x-page` removes the matched Area prefix before
module resource resolution while retaining the prefixed URL for navigation and breadcrumbs.

When Navigation emits `xshell:navigation:end`, Areas resolves the current Area from the URL. A change emits `xshell:area:change`.
Menu UIs should refresh on that event; `xshell:menus:change` is reserved for dynamic menu-content updates.

## Current implementation limits

Both hash and path navigation apply Area prefixes. Bootstrap's generic configuration merge can accept Area definitions from imported fragments even
though Area composition belongs to the root application. Dynamic menu children are mutable so registered sources can refresh them.

See [Navigation](../architecture/navigation.md), [Configuration](../architecture/configuration.md),
[Module Specification](../specifications/module.md), and [ADR-0005](../adr/0005-area-menu-composition.md).
