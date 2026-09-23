# Navigation

One Navigation subsystem maps browser location to Pages. Hash and path modes are both implemented on the same Page and stack model.

An Area is a navigation context within a mode. Its `xshell.areas.definitions.<id>.prefix` identifies that context, while a module's
`/_assets/<module-id>/...` URL identifies a page resource. Navigation mode, Area, and resource ownership are separate. The hash-mode
`hashPrefix = "#!"` marks the browser fragment; it is not an Area prefix.

## Menu paths and canonical targets

A menu item may provide both `path` and `href`. `path` is an optional friendly/public navigation path; `href` is the canonical XShell navigation
target. `path` is an alias, not a replacement for `href`. A menu item without `path` remains valid and menu-facing UI naturally falls back to
`href` through `menuitem.path || menuitem.href`.

For example, a module may contribute:

```jsonc
{ "label": "Components", "path": "/components", "href": "/_assets/x-demo/pages/01-components/index.js" }
```

For an Area with prefix `/main`, the effective item uses Area-aware values for both fields:

```jsonc
{ "label": "Components", "path": "/main/components", "href": "/main/_assets/x-demo/pages/01-components/index.js" }
```

`x-menu`, `x-page-menu`, and search results pass `path || href` to their generic navigation elements. `x-menuitem` and `x-anchor` receive only an
`href`; neither knows about menu paths or performs path-to-href translation. Breadcrumb lookup starts with the canonical Area-aware `href`, and
the returned breadcrumb entries carry their `path` for UIs that choose to expose the friendly link.

Navigation accepts either form. When a browser/navigation URL matches `Areas.resolvePath(value)`, Navigation replaces it with that effective menu
item's canonical `href` before setting `x-page.src`; when no path matches, it preserves the original value. Thus `/main/components` and direct
`/main/_assets/x-demo/pages/01-components/index.js` navigation both remain valid. Navigation translates to the Area-aware canonical href, not to
the final module resource URL.

`x-page` receives that canonical Area-aware href, identifies its Area, and removes only the Area prefix before asking the loader/resolver for the
module resource. The loader/resolver therefore sees `/_assets/x-demo/pages/01-components/index.js`; it does not know about menu `path` values or
perform path-to-href translation.

## Hash mode

Navigation listens for hash changes, decodes the page stack, and updates `x-page` elements. The configured `hashPrefix` is `#!`. Because the
destination is in the fragment, the server needs no path fallback for deep links.

```text
host page#!/_assets/test/pages/test1.js → Navigation → x-page → page resource
```

## Path mode

Path mode uses `history.pushState()`, `history.replaceState()`, and `popstate` with the same Pages and encoded stack data as hash mode. It removes the
configured application base path before interpreting the browser URL. Direct loads and refreshes require host collaboration so a deep application
path returns the XShell host page. `Extensions.UseXShell()` provides that SPA fallback within `AppBasePath`, while allowing configured reserved
prefixes and already-selected ASP.NET endpoints to continue through the pipeline.

```jsonc
{ "xshell": { "navigation": { "mode": "path", "hashPrefix": "#!" } } }
```

The nested names match `xshell.jsonc`; its checked-in default is currently `path`. The runtime `Navigation` constructor reads
`config.xshell.navigation.mode` and `hashPrefix`.

On a fresh load at the mode's empty/root URL, Navigation uses the default Area's `home`, derived from the first depth-first navigation item marked
`default: true`. Startup fails clearly if that home is absent. With an existing URL, Navigation restores the encoded page stack. When the root page
finishes loading, it emits `xshell:navigation:end`; Areas resolves the current Area from the longest matching prefix. `x-page` selects its
breadcrumb Area from the URL itself so lookup does not depend on the later navigation-end event.

A non-empty Area prefix wraps both friendly paths and canonical hrefs. For example, the browser can show `#!/customers/reports` in hash mode or
`/customers/reports` in path mode, while `x-page` receives `/customers/_assets/reports/pages/report.js`. `x-page` removes the Area prefix before
resolving the module resource; the `/_assets/reports/...` segment still denotes the resource owner.

TODO: The `x-page` `replace` and `navigate` event handlers in `Navigation._stackToDom()` remain debugger-marked and manipulate hash state directly.
Normal Navigation API and browser-history paths work, but those legacy event paths still need mode-neutral completion.

## Intents

A public navigation intent identifies a capability, for example `customer.detail` with `customerId`. The owning module maps it to a private
page/route. Other modules should request the intent through XShell rather than depend on a menu item or raw page URL. Intent registration and dispatch
are not yet implemented; current navigation accepts page `href` values.

See [Pages](pages.md) and [ADR-0003](../adr/0003-navigation.md).
