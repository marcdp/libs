# Pages and layouts

```text
Page = Component + Navigation
```

A Page uses the same state/render-engine infrastructure as Components. Its JavaScript default export can be a `Page` subclass or a definition object;
`page-js` converts a definition into a `Page` subclass. Navigation loads and mounts a Page as a destination. State entries marked `qs` are initialized
from query values by `page-js`.

```text
URL + query → Navigation → page resource → Loader → Page state → render engine
```

See [Components](components.md) for the contract and implementation formats. A component contract's DOM events are distinct from public module Bus
events.

## Layouts

Layouts are presentation containers for Pages. They do not resolve routes. Checked-in layout names are `default`, `dialog`, `main`, `stack`, and
`embed`, configured under `xshell.defaults.page.layout`. `x-page` currently uses `embed` when no layout is supplied; Navigation assigns `main` to the
root page and `stack` to additional pages. Dialog and embed are also navigation opening modes.

`x-page` selects layouts only from `xshell.defaults.page.layout`. In the checked-in `page-js`, render/state engine precedence is XShell Page default,
legacy `modules.<id>.page` override, then Page `meta` override. Although module configuration and the schema now support
`modules.<id>.defaults.page`, the checked-in Page loader does not consume that location yet. Stack rendering exists, but its full history contract
remains to be specified.

`xshell.defaults.dialog` is separate: its `confirm`, `message`, `prompt`, and `picker` values select the Page resources used by dialog operations.
Those resource defaults do not select the `dialog` layout.

See [Navigation](navigation.md), [Configuration](configuration.md), and [Loaders](loaders.md).
