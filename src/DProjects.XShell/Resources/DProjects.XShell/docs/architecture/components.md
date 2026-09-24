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
export default class MyComponent extends HTMLElement {
    connectedCallback() {
        this.innerHTML = "Hello";
    }
}
```

In this case the component provides its own Web Component implementation.

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

    controller({ }) {
        return {
            edit({ event }) {
                this.dispatchEvent(
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
`<slot>` elements; the implementation defines those elements. Every native `<slot>` used by the template must nevertheless be declared in
`contract.slots`. `<slot></slot>` uses the empty-string key, while `<slot name="actions"></slot>` uses the `actions` key. Duplicate occurrences
of a slot are allowed, and a declared slot does not have to appear in the template.

It is separate from the runtime implementation.

When the component definition is loaded, the component loader validates template slot usage against the contract before registering the component.
If the template uses a slot that is not declared in `contract.slots`, loading fails. This is component contract/loader validation, not render-engine
validation.

## Implementation

The default exported object contains the runtime implementation of the component.

Typical fields are:

```text
style
template
state
script
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

1. imports the component JavaScript file;
2. reads its default export;
3. validates template slot usage against `contract.slots`;
4. selects the configured state engine and creates the component state through it;
5. selects the configured render engine;
6. builds an `HTMLElement` subclass;
7. connects contract-defined attributes and properties to state;
8. creates the controller and connects lifecycle and rendering behavior;
9. registers the resulting class with `customElements`.

The result is a standard browser Web Component.

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
provide component render or state engines. `xshell.ui.component` instead identifies the global `lazy` and `error` components.

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

See [Resolvers](resolvers.md) and [Loaders](loaders.md).

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

## Related documentation

* [Components](../components/)
* [Component Contract](../components/manifest.md)
* [Properties](../components/properties.md)
* [Slots](../components/slots.md)
* [State](../components/state.md)
* [Lifecycle](../components/lifecycle.md)
* [Resolvers](resolvers.md)
* [Loaders](loaders.md)
* [X Templates](../extensions/x-templates/)
