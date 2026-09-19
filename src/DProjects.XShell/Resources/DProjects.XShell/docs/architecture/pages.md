# Pages and layouts

```text
Page = Component + Navigation
```

A Page uses the normal XShell Component model. Its JavaScript default export can be a component class or definition object; `page-js` loads it through
the same state/render infrastructure. Navigation loads and mounts a Page as a destination. Query values are mapped onto the Page's public properties
by `x-page`.

```text
URL + query → Navigation → page resource → Loader → Page properties → Component
```

See [Components](components.md) for the declaration and implementation formats. A component contract's DOM events are distinct from public module Bus
events.

## Layouts

Layouts are presentation containers for Pages. They do not resolve routes. Checked-in layout names are `default`, `main`, `stack`, `dialog`, and
`embed`, configured under `xshell.page.layout`. `x-page` currently uses `embed` when no layout is supplied; Navigation assigns `main` to the root page
and `stack` to additional pages. Dialog and embed are also navigation opening modes.

The intended selection precedence is application/XShell default, module override, then page override. Current `x-page` reads a legacy dotted
`page.layout.<name>` key, so this nested precedence is not implemented end to end. Stack rendering exists, but its full history contract remains to be
specified.

See [Navigation](navigation.md), [Configuration](configuration.md), and [Loaders](loaders.md).
