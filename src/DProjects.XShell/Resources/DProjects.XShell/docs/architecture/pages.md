# Pages Architecture

A Page is an XShell Component used as a navigation destination.

Pages and Components share the same definition model and runtime infrastructure.

Conceptually:

```text
Component
    ├── regular component
    └── page
```

A Page is therefore defined in the same way as any other XShell Component.

## Page definition

A Page is a standalone JavaScript file.

Like a Component, its default export can be either:

```text
page.js
    ↓
default export
    ├── class  → component/page class
    └── object → component/page definition
```

A definition-based Page can also export a `declaration` describing its public interface.

For example:

```js
// declaration
export const declaration = {
    description: "Shows a customer.",

    properties: {
        customerId: {
            type: "string",
            default: "",
            state: true,
            description: "Customer identifier."
        },

        tab: {
            type: "string",
            default: "details",
            state: true,
            description: "Selected tab."
        }
    },

    events: {},
    methods: {}
};

// implementation
export default {
    template: `
        <h1 x-text="state.customerId"></h1>
        <div x-text="state.tab"></div>
    `,

    state: {},

    script({ }) {
        return {
            onCommand(command, params) {
                if (command == "load") {
                    // page loaded
                }
            }
        };
    }
};
```

The same concepts therefore apply to both Pages and Components:

```text
declaration
    public interface

default export
    implementation
```

See [Components](components.md).

## Shared component infrastructure

Pages use the same core infrastructure as Components:

```text
JavaScript definition
    ↓
Loader
    ↓
state engine
    +
render engine
    ↓
runtime component class
```

They can use the same:

* declarations;
* properties;
* state model;
* render engines;
* state engines;
* templates;
* styles;
* scripts;
* events;
* lifecycle infrastructure.

The main difference is how the resulting component is used.

A normal Component is embedded inside another document or component.

A Page is loaded and hosted by XShell Navigation as the current navigation destination.

```text
Component
    → used inside UI

Page
    → used as navigation content
```

## Page resources

Modules normally expose pages from their `pages/` directory.

For example:

```text
module1/
└── pages/
    ├── page1.js
    ├── page2.js
    └── customer.js
```

Bootstrap creates conventional page resolvers for each module.

Navigation requests the page resource, which is resolved and loaded through the standard XShell Resolver and Loader infrastructure.

Conceptually:

```text
Navigation
    ↓
page URL
    ↓
Resolver
    ↓
Loader
    ↓
Page Component
    ↓
mount
```

See [Resolvers](resolvers.md), [Loaders](loaders.md), and [Navigation](navigation.md).

## Query string properties

The URL query string is mapped to Page properties.

For example:

```text
/pages/customer.js?customer-id=123&tab=orders
```

can initialize Page properties such as:

```text
customerId = "123"
tab = "orders"
```

Conceptually:

```text
URL query string
    ↓
Page properties
    ↓
Page state
```

This allows navigation parameters to use the same public property model as normal component inputs.

A Page can therefore declare navigation inputs as properties instead of introducing a separate Page-specific parameter model.

For example:

```js
export const declaration = {
    properties: {
        customerId: {
            type: "string",
            default: "",
            state: true
        },

        tab: {
            type: "string",
            default: "details",
            state: true
        }
    }
};
```

and navigate with:

```text
/pages/customer.js?customer-id=123&tab=orders
```

The Page receives those values through its properties.

## Page lifecycle

Because a Page is a Component, it follows the same basic component lifecycle.

Navigation adds the additional responsibility of loading, activating, and replacing Page instances as the user navigates.

At a high level:

```text
navigate
    ↓
load Page component
    ↓
create instance
    ↓
set URL properties
    ↓
mount
    ↓
render
    ↓
navigate away
    ↓
unmount
```

Page-specific navigation behavior should remain separate from the shared Component implementation model.

## Pages and Components

The architectural relationship is:

```text
Component infrastructure
    ├── definition
    ├── declaration
    ├── properties
    ├── state
    ├── events
    ├── state engine
    ├── render engine
    └── lifecycle
           ↓
        Component
           ↓
    ┌──────┴──────┐
    │             │
regular UI      Page
component       navigation destination
```

Pages should therefore avoid introducing a second component model.

Where possible, Page behavior should be expressed through the same Component concepts and infrastructure.

## Summary

The main model is:

```text
Page = Component + Navigation
```

A Page:

* is defined like a Component;
* can expose a public `declaration`;
* uses the same implementation format;
* uses the same state and render engines;
* uses the same property model;
* receives query-string values through its properties;
* is loaded and hosted by Navigation.

## Related documentation

* [Components](components.md)
* [Navigation](navigation.md)
* [Resolvers](resolvers.md)
* [Loaders](loaders.md)
* [Modules](modules.md)
* [X Templates](../extensions/x-templates/)
