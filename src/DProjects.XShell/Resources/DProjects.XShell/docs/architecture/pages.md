# Pages and layouts

```text
Page = Component + Navigation
```

A Page uses the same state/render-engine infrastructure as Components. Its JavaScript default export can be a `Page` subclass or a definition object;
`page-js` converts a definition into a `Page` subclass and orchestrates its contract, properties, public API, controller, lifecycle, and engines.
The state engine owns reactive state only, and the render engine owns rendered output only. Navigation loads and mounts a Page as a destination. State
entries marked `qs` are initialized from query values by `page-js`.

```text
browser URL + query → Navigation → canonical Area-aware href → x-page → module resource → Loader → Page state → render engine
```

For a menu item, the browser URL may be its optional friendly `path`. Navigation resolves that path to the menu item's canonical Area-aware `href`
before it sets `x-page.src`. `x-page` then removes the Area prefix only for module resource resolution. It does not interpret menu paths, and the
Loader does not translate menu paths to hrefs. Direct navigation to the canonical href continues to work when no friendly path is used.

See [Components](components.md) for the contract and implementation formats. A component contract's DOM events are distinct from public module Bus
events.

## Declarative dependencies

Definition-based Pages use the same `dependencies` mechanism as Components. A Page implementation declares an object whose keys are exposed on
the `dependencies` object passed to `controller({ dependencies })`, and whose values are ordinary XShell resource references.

```js
export default {
    dependencies: {
        marked: "module:/_assets/x/utils/markdown.js",
        fileIcon: "icon:x-file",
        editor: "component:x-code-editor"
    },

    template: `<main></main>`,

    controller({ dependencies }) {
        return {
            load() {
                console.log(dependencies.marked);
                console.log(dependencies.fileIcon);
                console.log(dependencies.editor);
            }
        };
    }
};
```

`page-js` resolves these declarations through the same Resolver and Loader path used by Components. All declared dependencies are resolved before
the Page class is returned and before a Page instance creates its controller. A resolution or load failure rejects the Page load before its class
or controller is created. For the common resource-reference rules and the distinction from runtime `loader.load(...)`, see
[Components](components.md#declarative-dependencies).

Definition-based Pages use the same public-property and state-default rule as components and layouts: `contract.properties[*].default` is
canonical for public properties, while `definition.state` supplies private/internal defaults. A state-backed public property may be repeated in
`definition.state` only with a structurally equal value; a non-state-backed public property may not be repeated there.

## Query-string initialization

Page properties opt into query-string initialization with `query: true`. The Page loader reads the query portion of that Page's own `src`, converts
the property name from camelCase to kebab-case, and applies the value to state before creating the controller. Only `string`, `number`, `integer`,
and `boolean` properties are supported, and `query: true` requires `state: true`. Missing parameters leave the contract default unchanged; malformed
numeric, integer, or boolean values reject Page creation. Boolean values are `true`, `1`, `false`, and `0`, with the textual values matched
case-insensitively.

Query binding is input-only and does not imply HTML attributes, property reflection, or URL updates. Properties without `query: true` never consume
Page query parameters, and normal Components do not perform this Page-specific initialization.

## Page lifecycle

A Page instance is loaded once and can be mounted and unmounted repeatedly. `x-page` preserves the Page controller and state while its host is
temporarily disconnected, then remounts that same instance when the host reconnects. Replacing the resource, and `removePage()`, are final
destruction paths: they unmount the current Page and then unload it before the instance is discarded. Page render engines and page styles belong
to each mount; Page state, controller, timers, events, and disposables belong to the Page instance until its final unload. See the component
[Lifecycle](../components/lifecycle.md) contract for the complete sequence.

## Layouts

Layouts are presentation containers for Pages. They do not resolve routes. Checked-in layout names are `default`, `dialog`, `main`, `stack`, and
`embed`, configured under `xshell.ui.layout`. `x-page` currently uses `embed` when no layout is supplied; Navigation assigns `main` to the
root page and `stack` to additional pages. Dialog and embed are also navigation opening modes.

`x-page` selects a layout through `xshell.ui.layout.<context>`. In `page-js`, a definition-based Page resolves each engine from Page `meta`
first, then `modules.<id>.defaults.page`; there is no XShell render-engine or state-engine fallback. Stack rendering exists in both navigation
modes, but its full behavioral contract remains to be specified.

`xshell.ui.layout.dialog` is the layout used for a Page opened in dialog context. It is separate from the standard dialog pages:
`xshell.ui.dialog.confirm`, `message`, `prompt`, and `picker` identify the Page resources used by dialog operations. Those Page-resource
defaults do not select the `dialog` layout.

See [Navigation](navigation.md), [Configuration](configuration.md), and [Loaders](loaders.md).
