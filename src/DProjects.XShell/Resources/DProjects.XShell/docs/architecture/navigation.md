# Navigation

One Navigation subsystem maps browser location to Pages. It has a working hash mode and an intended path mode; both are designed to share the same
page infrastructure.

An Area is a navigation context within a mode. Its `xshell.areas.definitions.<id>.prefix` identifies that context, while a module's
`/_assets/<module-id>/...` URL identifies a page resource. Navigation mode, Area, and resource ownership are separate. The hash-mode
`hashPrefix = "#!"` marks the browser fragment; it is not an Area prefix.

## Hash mode (current)

The checked-in default is `xshell.navigation.mode = "hash"` with `hashPrefix = "#!"`. Navigation listens for hash changes, decodes the page stack, and
updates `x-page` elements. Because the destination is in the fragment, the server need only serve the host page.

```text
host page#!/_assets/test/pages/test1.js → Navigation → x-page → page resource
```

## Path mode (planned)

Path mode would use browser paths and history with the same Pages. Direct deep links require the server to return the XShell host page.
`Navigation.init()` currently throws `Path mode is not implemented yet`; history-writing branches alone do not make it usable.

```jsonc
{ "xshell": { "navigation": { "mode": "hash", "hashPrefix": "#!" } } }
```

The nested names match `xshell.jsonc`; the runtime `Navigation` constructor reads `config.xshell.navigation.mode` and `hashPrefix`.

On a fresh hash-mode load with no hash, Navigation uses the default Area's `home` as its initial target. With an existing hash, it restores the
encoded page stack. When the root page finishes loading, Navigation emits `xshell:navigation:end` with its source; Areas then resolves the current
Area from that source's longest matching prefix. Effective menu and breadcrumb lookup is scoped to that Area. The current `x-page` breadcrumb
lookup occurs before that navigation-end event, so an Area transition can use the previous Area for that lookup.

The intended non-empty Area prefix would wrap the navigation path, for example `#!/customers/_assets/reports/pages/report.js`.
Current bootstrap path normalization and page resolution do not yet support that end-to-end; see
[Areas](../subsystems/areas.md#current-implementation-limits). The `/_assets/reports/...` segment still denotes the resource owner.

## Intents

A public navigation intent identifies a capability, for example `customer.detail` with `customerId`. The owning module maps it to a private
page/route. Other modules should request the intent through XShell rather than depend on a menu item or raw page URL. Intent registration and dispatch
are not yet implemented; current navigation accepts page `href` values.

See [Pages](pages.md) and [ADR-0003](../adr/0003-navigation.md).
