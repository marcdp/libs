# Pages and layouts

```text
Page = Component + Navigation
```

A Page uses the same state/render-engine infrastructure as Components. Its JavaScript default export can be a `Page` subclass or a definition object;
`page-js` converts a definition into a `Page` subclass. Navigation loads and mounts a Page as a destination. State entries marked `qs` are initialized
from query values by `page-js`.

```text
browser URL + query → Navigation → canonical Area-aware href → x-page → module resource → Loader → Page state → render engine
```

For a menu item, the browser URL may be its optional friendly `path`. Navigation resolves that path to the menu item's canonical Area-aware `href`
before it sets `x-page.src`. `x-page` then removes the Area prefix only for module resource resolution. It does not interpret menu paths, and the
Loader does not translate menu paths to hrefs. Direct navigation to the canonical href continues to work when no friendly path is used.

See [Components](components.md) for the contract and implementation formats. A component contract's DOM events are distinct from public module Bus
events.

## Layouts

Layouts are presentation containers for Pages. They do not resolve routes. Checked-in layout names are `default`, `dialog`, `main`, `stack`, and
`embed`, configured under `xshell.defaults.layout`. `x-page` currently uses `embed` when no layout is supplied; Navigation assigns `main` to the
root page and `stack` to additional pages. Dialog and embed are also navigation opening modes.

`x-page` selects a layout through `xshell.defaults.layout.<context>`. In `page-js`, a definition-based Page resolves each engine from Page `meta`
first, then `modules.<id>.defaults.page`; there is no XShell render-engine or state-engine fallback. Stack rendering exists in both navigation
modes, but its full behavioral contract remains to be specified.

`xshell.defaults.layout.dialog` is the layout used for a Page opened in dialog context. It is separate from the standard dialog pages:
`xshell.defaults.dialog.confirm`, `message`, `prompt`, and `picker` identify the Page resources used by dialog operations. Those Page-resource
defaults do not select the `dialog` layout.

See [Navigation](navigation.md), [Configuration](configuration.md), and [Loaders](loaders.md).
