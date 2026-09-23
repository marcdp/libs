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
                "navigation": [{ "label": "Reports", "path": "/reports", "href": "/pages/report.js", "default": true }],
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

Areas composes each named menu slot by visiting participating modules in `area.modules` order. A module contribution is either a static array,
which Areas clones in declaration order, or a string naming a source already registered through `Areas.registerSource(name, source)`. For a named
source, Areas calls `source.resolve()` during composition and clones the returned array as the complete contribution. Each effective item has its
module id, Area id, label, icon, children, path, and href. `path` is optional and is the friendly/public navigation alias; `href` is the canonical
XShell navigation target. Effective menu structures are separate for each Area; participation creates no additional module instances. Unknown
module ids and unknown sources produce warnings and are skipped.

`childrenSource` is a separate pattern. It belongs on a static menu item and adds dynamically resolved children to that item; it does not replace
the named menu contribution:

```jsonc
{
    "navigation": [
        { "label": "Customers", "href": "/pages/customers.js", "childrenSource": "customer-pages" }
    ],
    "tools": "report-tools"
}
```

Here `customer-pages` supplies only `Customers` children, while `report-tools` supplies the entire `tools` menu. Source names are runtime lookup
identifiers, not URLs or resolver entries. They provide runtime data without mutating the readonly effective configuration.

The bundled `x-demo` module illustrates a complete-menu source. Its `navigation` contribution names
`x-demo-dynamic-navigation-menu-source`; its controller reads the physical `module.files.json` inventory, derives a page hierarchy from `/pages`,
and registers the resulting menu. Numeric filename and directory prefixes order entries and are removed from labels. This is an `x-demo` convention,
not an XShell requirement: `module.files.json` remains a physical file inventory, not menu metadata. Its `/pages/index.js` becomes the top-level
`Demo` item, and other page sections become that item's children.

Bootstrap first maps module-relative menu hrefs into `/_assets/<module-id>/...`. Areas then applies the Area prefix to both local `path` and local
`href`, leaving external scheme URLs unchanged. For example, `/components` and `/_assets/x-demo/pages/01-components/index.js` become
`/main/components` and `/main/_assets/x-demo/pages/01-components/index.js` in the `main` Area. The Area home is `path || href` of the first
`navigation` item marked `"default": true` in depth-first traversal. Children are searched. If none is marked, `home` is null. On a fresh load at
the selected mode's empty/root URL, Navigation requires the default Area to have a home.

`xshell.areas.getMenu("navigation")` selects the current Area; `getMenu("navigation", "inventory")` selects one explicitly.
`getMenuitemBreadcrumb(href, areaId = null)` searches only that Area's effective menus by canonical href, defaulting to the current Area. Its
entries retain both the canonical `href` and optional friendly `path`, so breadcrumb UI can use a ready-to-navigate friendly link without
performing translation itself.
`Areas.registerSource(name, source)` registers a source before Areas composition. A source exposes `resolve()`, which returns menu items, and may
declare `dependsOn`, an array of Bus event names. For `childrenSource` targets, an event refreshes their effective children and emits
`xshell:menus:change` when content changed. Complete-menu sources named by a string are resolved only during `Areas.init()`; the current runtime
does not attach them as refresh targets, so they are not live-reactive after composition.

## Navigation context

An Area prefix marks a navigation context; `/_assets/<module-id>/...` marks resource ownership. Both `path` and `href` are Area-aware navigation
values after composition; neither should be described as an unprefixed physical resource URL. A configured `"inventory"` or
`"/inventory/"` normalizes to `"/inventory"`. Empty or root prefixes normalize to `""`. Prefix matching uses complete segments and tries the
longest prefix first. Navigation resolves a friendly path through `Areas.resolvePath(path)` to its canonical Area-aware href. At page load,
`x-page` removes the matched Area prefix before module resource resolution while retaining the prefixed canonical URL for navigation and
breadcrumbs.

When Navigation emits `xshell:navigation:end`, Areas resolves the current Area from the URL. A change emits `xshell:area:change`.
Menu UIs should refresh on that event; `xshell:menus:change` is reserved for dynamic menu-content updates.

## Current implementation limits

Both hash and path navigation apply Area prefixes. Bootstrap's generic configuration merge can accept Area definitions from imported fragments even
though Area composition belongs to the root application. Dynamic `childrenSource` content can refresh; complete named menu sources are composed once.

See [Navigation](../architecture/navigation.md), [Configuration](../architecture/configuration.md),
[Module Specification](../specifications/module.md), and [ADR-0005](../adr/0005-area-menu-composition.md).
