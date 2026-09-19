# Navigation

One Navigation subsystem maps browser location to Pages. It has a working hash mode and an intended path mode; both are designed to share the same
page infrastructure.

## Hash mode (current)

The checked-in default is `xshell.navigation.mode = "hash"` with `hashPrefix = "#!"`. Navigation listens for hash changes, decodes the page stack, and
updates `x-page` elements. Because the destination is in the fragment, the server need only serve the host page.

```text
host page#!/orders/123 → Navigation → Page
```

## Path mode (planned)

Path mode would use browser paths and history with the same Pages. Direct deep links require the server to return the XShell host page.
`Navigation.init()` currently throws `Path mode is not implemented yet`; history-writing branches alone do not make it usable.

```jsonc
{ "xshell": { "navigation": { "mode": "hash", "hashPrefix": "#!" } } }
```

The nested names match `xshell.jsonc`. The runtime `Navigation` service still reads older dotted keys, so this nested setting is not yet consumed
correctly.

## Intents

A public navigation intent identifies a capability, for example `customer.detail` with `customerId`. The owning module maps it to a private
page/route. Other modules should request the intent through XShell rather than depend on a menu item or raw page URL. Intent registration and dispatch
are not yet implemented; current navigation accepts page `href` values.

See [Pages](pages.md) and [ADR-0003](../adr/0003-navigation.md).
