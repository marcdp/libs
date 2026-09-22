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
        label:   { type: "string",  default: "",    attr: true, state: true },
        message: { type: "string",  default: "",    attr: true, state: true },
        columns: { type: "number",  default: 2,     attr: true, state: true },
        remove:  { type: "boolean", default: false, attr: true, state: true },
        move:    { type: "boolean", default: false, attr: true, state: true },
        edit:    { type: "boolean", default: false, attr: true, state: true }
    },

    methods: {}
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

    script({ }) {
        return {
            onCommand(command, params) {
                if (command == "edit") {
                    this.dispatchEvent(
                        new CustomEvent("edit", {
                            bubbles: true,
                            composed: false
                        })
                    );
                }
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
            attr: true,
            state: true,
            description: "Current value."
        }
    },

    methods: {}
};
```

The contract describes how other code can interact with the component.

It is separate from the runtime implementation.

The current loader reads `module.contract`, but it does not substantially use the object after passing it into component-class construction. The
metadata therefore documents an intended public surface without current runtime validation or enforcement.

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
3. creates the component state;
4. selects the configured state engine;
5. selects the configured render engine;
6. builds an `HTMLElement` subclass;
7. connects attributes and properties to state;
8. connects lifecycle and rendering behavior;
9. registers the resulting class with `customElements`.

The result is a standard browser Web Component.

## State and rendering

A definition-based component uses engine defaults in nested configuration:

```jsonc
{ "xshell": { "defaults": { "component": { "stateEngine": "plain", "renderEngine": "plain" } } } }
```

Module definitions may provide `defaults.component` overrides. Current `component-js` precedence is component `meta`, then module
`defaults.component`, then XShell `defaults.component`.

The selected state and render engines are used while converting the definition into the final Web Component class.

For example:

```text
component definition
    ↓
state engine
    +
render engine
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
* [State](../components/state.md)
* [Lifecycle](../components/lifecycle.md)
* [Resolvers](resolvers.md)
* [Loaders](loaders.md)
* [X Templates](../extensions/x-templates/)
