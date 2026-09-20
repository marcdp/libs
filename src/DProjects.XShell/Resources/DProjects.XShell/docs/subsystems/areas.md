# Areas

An Area is a top-level navigation context. The root application composes Areas: it chooses their prefixes, homes, and participating modules.
A reusable module contributes menus without choosing an Area. One module can participate in several Areas, or none, while `Modules` creates one live
runtime instance for its canonical module id.

## Configuration and ownership

```jsonc
{
    "modules": {
        "reports": {
            "menus": {
                "navigation": [{ "label": "Reports", "href": "/pages/report.js" }],
                "tools": [{ "label": "Export", "href": "/pages/export.js" }]
            }
        }
    },
    "xshell": {
        "areas": {
            "default": "customers",
            "definitions": {
                "customers": { "prefix": "/customers", "label": "Customers", "home": "/pages/home.js", "modules": ["customers", "reports"] },
                "inventory": { "prefix": "/inventory", "label": "Inventory", "modules": ["inventory", "reports"] }
            }
        }
    }
}
```

This example expresses the **intended composition**; see the non-empty prefix limits below before using it as a working configuration. Each
`xshell.areas.definitions.<id>` entry may set `prefix`, `label`, `icon`, `home`, `modules`, and `order`. The `modules` array determines participation
and menu contribution order. Root ownership is an architectural rule, not a runtime restriction: bootstrap merges `xshell` fragments from imported
configurations too. Arrays, including `area.modules`, concatenate during that merge.

`Areas` creates runtime entries with `id`, `label` (defaulting to id), `icon` and `home` (defaulting to null), normalized `prefix`, a copied `modules`
array, `order` (defaulting to zero), and `default`. The configured `xshell.areas.default` is compared with area ids; an unknown non-empty id throws.
Duplicate normalized prefixes also throw. Areas are presented with the default first, then by `order`, then by `label`. `getDefaultArea()` falls back
to the first sorted Area if no default is configured; `getCurrentArea()` starts there.

## Navigation context and resources

An Area prefix identifies a navigation context; `/_assets/<module-id>/...` identifies a module resource. The latter does not change ownership when
the same module participates in another Area. `Areas` adds a leading slash to a configured prefix if needed and removes a trailing slash except
for `/`. An empty prefix becomes `/`. `resolveAreaId(href)` matches complete prefix segments, trying longer prefixes first, so `/admin/tools`
takes precedence over `/admin`.

In hash mode, Navigation starts at the default Area's `home` when the browser has no hash. When a root page finishes loading, Navigation emits
`xshell:navigation:end` with its `src`. Areas resolves that source to an Area and, if it differs from the current one, emits
`xshell:area:change`. If no prefix matches, it retains the current Area. `getAreas()`, `getArea(id)`, `getDefaultArea()`, `getCurrentArea()`, and
`resolveAreaId(href)` expose the Area model.

## Effective menus

Modules own area-independent contributions under `modules.<module-id>.menus.<menu-name>`, for example `navigation` and `tools`. Menus builds an
effective menu for each Area and each named slot by resolving `area.modules` in declared order and appending each module's items. It recursively
copies items with their module id, Area id, and effective href. Unknown module ids cause a warning and are skipped. A module in two Areas produces
two effective menu contributions, not two module instances.

`xshell.menus.getMenu("navigation")` and `getMenu("tools")` use the current Area; `getMenu("navigation", "inventory")` selects one explicitly.
`getMenuitemBreadcrumb(href, areaId = null)` searches the selected Area's effective menus, defaulting to the current Area. The intended lookup
sequence is navigation URL → Area → that Area's menus → breadcrumb. Menus emits `xshell:menus:changed` on `xshell:area:change` for menu UIs.
There is no framework-special main menu slot.

For dynamic children, `registerSource(name, source)` accepts a source with `resolve()` and optional Bus event dependencies. The registered source
can refresh its children; when an event-triggered `refresh()` reports a change, Menus emits `xshell:menus:changed`.

## Current implementation limits

- Bootstrap recursively treats every leading-slash string in a module configuration as a module-relative resource. A root-authored prefix such as
  `/customers` is therefore rewritten to `/_assets/<root-module-id>/customers`; the intended navigation prefix does not survive unchanged.
- Menus adds the Area prefix to an already-normalized menu href. A conceptual URL such as
  `/customers/_assets/reports/pages/report.js` has no corresponding generated page resolver rule: rules match `/_assets/<module-id>/...`, and neither
  Navigation nor `x-page` strips the Area prefix before calling Loader. Non-empty prefix page loading is not fully implemented.
- `x-page` looks up its breadcrumb while loading, before Navigation emits `xshell:navigation:end` for that page. On an Area change, the current Area
  may therefore still be the previous one during breadcrumb lookup.
- The checked-in Area selection page listens for `xshell:area:changed`, while Areas emits `xshell:area:change`. That listener is stale. The
  checked-in `x-page-menu` component requests the old `"main"` slot rather than `"navigation"` and does not listen for menu changes.

See [Navigation](../architecture/navigation.md), [Configuration](../architecture/configuration.md),
[Module Specification](../specifications/module.md),
and [ADR-0005](../adr/0005-area-menu-composition.md).
