# Components

An XShell component is a standalone JavaScript file that exports a Web Component implementation.

A component can be provided in two forms:

```text
component.js
    ↓
default export
    ├── class  → Web Component class
    └── object → Web Component definition
```

## Component as a class

If the default export is a JavaScript class, XShell considers it to be the component class and returns it directly.

For example:

```js
class MyComponent extends HTMLElement {
    connectedCallback() {
        this.innerHTML = "Hello";
    }
}
customElements.define("my-component", MyComponent);
export default MyComponent;
```

In this case the component provides its own implementation and registration; the class-return branch does not apply definition contracts.

## Component as a definition

If the default export is an object, XShell considers it to be a Web Component definition.

A component file can then contain two separate parts:

```text
contract
    public interface

default export
    implementation
```

For example:

```js
// contract
export const contract = {
    description: "Provides a layout and action controls for a group of data fields.",

    events: {
        move: {
            description: "Raised when the field is moved.",
            detail: {
                direction: { type: "string" }
            }
        },

        edit: {
            description: "Raised when the field is edited."
        },

        remove: {
            description: "Raised when the field is removed."
        }
    },

    properties: {
        label:   { type: "string",  default: "",    attribute: true, state: true },
        message: { type: "string",  default: "",    attribute: true, state: true },
        columns: { type: "number",  default: 2,     attribute: true, state: true },
        remove:  { type: "boolean", default: false, attribute: true, state: true },
        move:    { type: "boolean", default: false, attribute: true, state: true },
        edit:    { type: "boolean", default: false, attribute: true, state: true }
    },

    methods: {},

    slots: {
        "": {
            description: "Default slot."
        },
        "header": {
            description: "Content displayed in the component header."
        }
    }
};

// implementation
export default {
    style: `
        :host {
            display: block;
        }

        div.body {
            display: grid;
            gap: 1em;
        }
    `,

    template: `
        <label x-if="state.label" x-text="state.label"></label>

        <div class="body">
            <slot></slot>
        </div>

        <x-button
            x-if="state.edit"
            icon="x-edit"
            x-on:click="edit">
        </x-button>
    `,

    state: {},

    controller({ host }) {
        return {
            edit(event) {
                host.dispatchEvent(
                    new CustomEvent("edit", {
                        bubbles: true,
                        composed: false
                    })
                );
            }
        };
    }
};
```

## Contract

The optional named `contract` export describes the public interface of the component.

It can describe:

```text
description
events
properties
methods
slots
examples
```

For example:

```js
export const contract = {
    description: "A sample component.",

    events: {
        change: {
            description: "Raised when the value changes."
        }
    },

    properties: {
        value: {
            type: "string",
            default: "",
            attribute: true,
            state: true,
            description: "Current value."
        }
    },

    methods: {},

    slots: {
        "": {
            description: "Default slot."
        },
        "header": {
            description: "Content displayed in the component header."
        }
    }
};
```

The contract describes how other code can interact with the component.

Its optional `slots` section documents the public Web Component composition API. The empty string `""` identifies the default unnamed slot, and
named keys identify named slots. Slot metadata supports `description` and an optional `required` boolean. This metadata does not create or render
`<slot>` elements; the implementation defines those elements. Every engine-reported slot must be declared in
`contract.slots`; `x` reports compiler-discovered slots while `html` reports none. `<slot></slot>` uses the empty-string key, while `<slot
name="actions"></slot>` uses the `actions` key. Duplicate occurrences
of a slot are allowed, and a declared slot does not have to appear in the template.

It is separate from the runtime implementation.

`query` is valid property metadata in the shared contract. The Page loader may use `query: true` to initialize state-backed public properties from
the Page `src` query string; generic Component loading accepts the metadata but does not read or rewrite query strings. Component `reflect: true`
writes a kebab-case attribute even without `attribute: true`, which controls incoming observation.
Page reflection requires `query: true` and does not reflect HTML attributes. See [Properties](../components/20-properties.md).

When the component definition is loaded, the component loader validates render-factory slot metadata against the contract before registering the
component.
If the factory reports a slot not declared in `contract.slots`, loading fails. This is component contract/loader validation, not render-engine
validation.

## Implementation

The default exported object contains the runtime implementation of the component.

Typical fields are:

```text
dependencies
style
template
state
controller
```

Conceptually:

```text
contract
    ↓
public contract

implementation
    ↓
style + template + state + behavior
```

The implementation object is not itself a browser Web Component class.

It must first be converted into one.

Its optional `style` property is one CSS source string. Put all CSS rules for the Component in that string; `style` does not accept an array of
strings.

## Declarative dependencies

A definition-based Component can declare resources that its controller needs through `dependencies`. It is a declarative resource request, not a
separate dependency-injection subsystem. Each key is the name made available on the `dependencies` object passed to the controller, and each value
is an ordinary XShell resource reference.

```js
export default {
    dependencies: {
        marked: "module:/_assets/x/utils/markdown.js",
        fileIcon: "icon:x-file",
        editor: "component:x-code-editor"
    },

    template: `<div></div>`,

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

`component-js` passes the dependencies object to the Loader. For each value, the Resolver selects the matching resource definition, URL, and
resource-specific loader; the Loader then obtains the resolved resource. The resulting values retain their declared keys in `dependencies`.

All declared dependencies are resolved before the generated Web Component class is returned and before a component instance creates its controller.
An unresolvable reference rejects the Loader request. If resolution succeeds but one or more loads fail, the Loader waits for its requested loads to
settle and then rejects. In either case, the component class and its controller are not created.

This is distinct from runtime loading. `dependencies` is for resources known in the implementation definition and resolved automatically during
component loading. The `loader` service remains available to controller code for dynamic or runtime resource loading; dynamic
`loader.load(...)` calls are not replaced by `dependencies`.

## Component loader

Components are loaded through the `component-js` loader.

Conceptually:

```text
component.js
    ↓
component-js loader
    ↓
default export
    ↓
class?
    ├── yes → return class
    └── no
         ↓
       component definition
         ↓
       build Web Component class
         ↓
       customElements.define(...)
```

For definition-based components, `component-js`:

1. imports the module and reads its contract/default implementation;
2. normalizes defaults and validates both schemas and property/state consistency;
3. selects state/render engines and creates the render factory;
4. checks factory-reported slots against `contract.slots`;
5. loads render-factory dependencies and then implementation `dependencies`, and initializes the render factory;
6. builds and registers an `HTMLElement` subclass with public properties/method proxies;
7. on instance construction, creates state, helpers, and the private controller and validates declared controller methods;
8. coordinates connection lifecycle and scheduled rendering.

The result is a standard browser Web Component.

## Containing Page lookup

Definition-based Component controllers can request `getPage` service:

```js
controller({ getPage }) {
    // ...
}
```

Both services locate the closest containing `<x-page>` host. The lookup walks ordinary DOM parents and, when it reaches a Shadow DOM root, continues
through `getRootNode().host`. Consequently, it can find a containing Page across Shadow DOM boundaries. A Component and a Page remain separate
runtime systems; this lookup only gives a Component access to its containing Page.

### `getPage()`

`getPage()` is synchronous and returns the current value of the closest `<x-page>` host's `page` property. Its effective return type is
`Page | null`: it returns `null` when there is no containing `<x-page>`, and it can also return `null` while that host has no current Page instance.
It is therefore not guaranteed to return a Page during Component `mount()`.

```js
controller({ getPage }) {
    return {
        mount() {
            const page = getPage();
            if (!page) return;

            // use containing Page
        }
    };
}
```

## Controller and Web Component ownership

The generated Web Component owns DOM/custom-element integration, public properties, public contract methods, its `ShadowRoot`, and framework
lifecycle plumbing. The controller owns implementation behavior, private methods, lifecycle handlers, and X Template handlers.

Controller methods always execute with the controller object as `this`. A controller that needs the generated Web Component must explicitly
request the injected `host` dependency; XShell does not inject `shadowRoot` separately.

```js
export const contract = {
    methods: {
        validate: {
            description: "Validates the component."
        }
    }
};

export default {
    template: '<div class="content"></div>',
    controller({ state, host }) {
        return {
            mount() {
                host.addEventListener("click", () => this.refresh());
                this.refresh();
            },

            refresh() {
                const element = host.shadowRoot.querySelector(".content");
                // update private implementation behavior
            },

            validate(options) {
                // validate using the caller's original arguments
            }
        };
    }
};
```

In this example, `this.refresh()` calls another method on the same controller, `host.shadowRoot` accesses the Web Component DOM, and
`element.validate(options)` is a public Web Component proxy to `controller.validate(options)`.

Controller methods are private by default. X Template handlers resolve directly against the controller, so a template can invoke `refresh`,
`validate`, or any other controller handler without exposing it on the Web Component. Only names declared by `contract.methods` are installed as
public Web Component methods. Each declared public method must have a controller function of the same name and cannot replace a framework or
native Web Component method. Contract method entries are metadata; the loader never executes them as implementation functions.

## State and rendering

A definition-based component resolves its engines from its own `meta` first and then from its owning module's required defaults:

```jsonc
{
    "modules": {
        "orders": {
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            }
        }
    }
}
```

`component-js` precedence is component `meta`, then `modules.<id>.defaults.component`. Each resolved module requires both this component-default
group and the corresponding Page-default group; render engine and state engine values are required non-empty strings. XShell defaults do not
provide component render or state engines. `xshell.ui.component` instead selects the global `lazy`, `error`, and Markdown Page `markdown` components.

The loader uses the selected engines while converting the definition into the final Web Component class. It remains responsible for the contract,
public API, properties and attributes, controller, lifecycle, services, and orchestration. The state engine owns reactive state only; the render
engine owns rendered output only.

For example:

```text
component definition
    ↓
component-js loader
    ├─ state engine → reactive state
    └─ render engine → rendered output
    ↓
HTMLElement subclass
```

X Templates can be used as a render engine, but they are not required by the component model.

## Component resources

Modules normally expose components from their `components/` directory.

For example:

```text
module1/
└── components/
    ├── module1-button.js
    ├── module1-form.js
    └── module1-field.js
```

Bootstrap creates conventional component resolvers for each module.

A logical component reference such as:

```text
component:module1-button
```

can therefore resolve to the corresponding JavaScript component file.

See [Resolvers](80-resolvers.md) and [Loaders](90-loaders.md).

## Summary

The main XShell component model is:

```text
one component
    =
one JavaScript file
```

That file can export:

```text
optional contract
    +
default implementation
```

and the default implementation is either:

```text
Web Component class
```

or:

```text
Web Component definition object
    ↓
component-js
    ↓
Web Component class
```

The optional `contract` export describes the public interface.

The default export provides the runtime implementation.

## Markdown component inputs

`x-markdown` is a trusted-content renderer and does not sanitize generated HTML. `x-markdown.value` accepts trusted raw Markdown text, and
`x-markdown.src` accepts a normal browser URL to trusted Markdown content and uses `fetch`, without routing the document through the Loader or
requiring a `string:` prefix. Both inputs use the same renderer, backed by `x/utils/markdown.js` and its vendored parser. Applications must
sanitize untrusted or user-controlled Markdown according to their own policy before rendering it, or use a different Markdown Component with an
appropriate sanitization policy. CSP is defense in depth and does not replace sanitizing untrusted content.

```html
<x-markdown src="/_assets/xshell-docs/pages/10-architecture/100-services.md"></x-markdown>
<x-markdown value="This is **Markdown**."></x-markdown>
```

The Service Worker maps requests for `/_assets/...` application Paths to physical resources. The component keeps that virtual source URL as the base for
relative `a[href]`, `img[src]`, `source[src]`, `video[src]`, and `audio[src]` attributes, including those inside templates. It preserves fragment-only,
root-relative, fully qualified, and protocol URLs. Relative query strings and fragments survive resolution; same-origin results use pathname,
query, and fragment. Without `src`, direct `value` content retains its authored URLs.

After conversion, the component discovers custom elements, including nested template content, and loads their `component:<name>` dependencies
before committing the DOM. Raw HTML and custom XShell elements are intentionally supported and may become live DOM. HTTP failures render the
response status and source URL as text through the same Markdown path. A newer input supersedes an older fetch or pending render. Changing `src`
to a document with identical text still updates its relative URLs.

Ordinary anchors remain anchors. Inside a Page, `x-page` performs navigation interception; `x-markdown` does not call Navigation. Markdown links
retain their `.md` URLs and resolve through the generic [Markdown Page adapter](70-pages.md#markdown-pages).

## Related documentation

* [Components](../components/)
* [Component Contract](../components/10-manifest.md)
* [Properties](../components/20-properties.md)
* [Slots](../components/50-slots.md)
* [State](../components/30-state.md)
* [Lifecycle](../components/60-lifecycle.md)
* [Resolvers](80-resolvers.md)
* [Loaders](90-loaders.md)
* [X Templates](../extensions/x-templates/)

Implementation fields also include `meta` and compiler-generated `templateRenderer`.
Loader-derived `meta.id` is required by the implementation schema, alongside optional `title` and engine overrides.
Contract enforcement and metadata limits are detailed in [Component Contract](../components/10-manifest.md).
