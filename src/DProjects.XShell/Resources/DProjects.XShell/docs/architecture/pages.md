# Pages and layouts

```text
Page = Component + Navigation
```

A Page uses the same state/render-engine infrastructure as Components. Its JavaScript default export can be a `Page` subclass or a definition object;
`page-js` converts a definition into a `Page` subclass and orchestrates its contract, properties, public API, controller, lifecycle, and engines.
The state engine owns reactive state only, and the render engine owns rendered output only. Navigation loads and mounts a Page as a destination. State
properties marked `query: true` are initialized from query values by `page-js`.

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

## Controller services

A definition-based Page controller receives a lazily resolved execution context through its destructured argument:

```js
controller({ state, events, query, navigation }) {
    // ...
}
```

This controller service provider is not a second general-purpose dependency-injection framework. It combines Page-specific context with named
services from XShell. It is also distinct from declarative resource `dependencies`, which the Loader resolves before controller creation and makes
available under the single `dependencies` value described in [Declarative dependencies](#declarative-dependencies).

The Page-specific values are:

* `definition` — the frozen and sealed Page definition object used to create the Page class. This is shared definition metadata/context rather than
  Page-instance state and normally is not needed by Page code. Modifying it is not supported.
* `state` — the Page-instance state object created by the configured state engine and consumed by the render engine/template. State is private
  implementation data unless a value is explicitly exposed through the Page contract; public properties and private state are not interchangeable.
* `context` — the context supplied when that Page instance is created. It is Page-instance framework context; XShell does not establish arbitrary
  context members as part of the public Page contract.
* `timer` — a newly created Page-scoped `Timer` on every access. String callbacks dispatch commands to that Page's controller. Each Timer is
  registered as a Page-owned disposable and is disposed during final unload, not during an ordinary unmount.
* `events` — a newly created Page-scoped `Events` helper on every access. String callbacks dispatch commands to that Page's controller. Each helper
  is registered as a Page-owned disposable and removes its listeners during final unload.
* `page` — the current Page instance. Use it only for Page-specific operations that genuinely require the Page object; prefer `state`, `query`, or
  an injected service for ordinary controller work.
* `dependencies` — the object containing resources declared by the definition's `dependencies` member. These resources are resolved before the
  Page class and its controllers are created; the loaded object belongs to that generated class rather than to one Page instance. See
  [Declarative dependencies](#declarative-dependencies).
* `query` — a newly created browser `URLSearchParams` instance on every access, populated from the query text in the Page instance's own `src`. It
  is raw Page-instance query access and performs no type conversion.

For example:

```js
export default {
    dependencies: {
        formatter: "module:/utils/formatter.js"
    },

    state: {
        name: ""
    },

    controller({ state, query, dependencies, navigation, events }) {
        return {
            load() {
                state.name = query.get("name") ?? "";
            }
        };
    }
};
```

Here `dependencies` is the resolved declarative-resource object, `navigation` falls through to the XShell service container, and `state`, `query`,
and `events` are Page-specific values. Destructuring requests the values; it does not make them public Page properties.

Names other than the eight Page-specific values fall through to `xshell.services.resolve(name)`. The services registered by the current XShell
initialization are `areas`, `auth`, `bus`, `config`, `container`, `debug`, `dialog`, `i18n`, `loader`, `modules`, `navigation`, `resolver`, `runtime`,
`services`, `tabs`, `temp`, `urlRewriter`, and, after login, `identity`. Code can therefore request a registered service naturally, for example
`controller({ navigation, bus, loader })`. A missing name fails service resolution; Page injection does not manufacture unknown services.
`navigation` is the shared service for navigation operations; it is not the current Page's `query` object.

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

Injected `query` and contract-property query binding can coexist, but they serve different purposes:

```text
controller({ query })
    raw URLSearchParams access; values remain strings

contract property query: true
    declarative, typed, validated initialization of a state-backed public Page property
```

For a Page `src` ending in `?name=lucas&count=123&tag=a&tag=b`, the injected object supports ordinary browser APIs:

```js
query.get("name");  // "lucas"
query.get("count"); // "123"
query.has("debug"); // false
query.getAll("tag"); // ["a", "b"]
```

Repeated parameters are preserved and all values remain strings. In contrast, a contract property such as the following is initialized in state
before controller creation and is converted and validated as an integer:

```js
export const contract = {
    properties: {
        count: {
            type: "integer",
            default: 0,
            state: true,
            query: true
        }
    }
};
```

The current raw `query` accessor constructs `URLSearchParams` from the segment selected by `self.src.split("?")[1]`; it does not remove a trailing
`#fragment`. Contract-property binding does remove that fragment before parsing. Page destinations normally carry the Page source and query without
a fragment, but this is a current edge-case difference between the two mechanisms.

## Page lifecycle

A Page instance is loaded once and can be mounted and unmounted repeatedly. `x-page` preserves the Page controller and state while its host is
temporarily disconnected, then remounts that same instance when the host reconnects. Replacing the resource, and `removePage()`, are final
destruction paths: they unmount the current Page and then unload it before the instance is discarded. Page render engines and page styles belong
to each mount; Page state, controller, timers, events, and disposables belong to the Page instance until its final unload. See the component
[Lifecycle](../components/lifecycle.md) contract for the complete sequence.

Mounting does not recreate the controller. The Page instance owns its state, controller, Timers, Events helpers, and other disposables across
mount/unmount cycles, and final unload disposes the helpers and releases the controller.

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
