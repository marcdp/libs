# Component Slots

Component contracts may declare the slots exposed by a Web Component. This describes the component's public composition API for consumers.

## Contract metadata

Use the optional `slots` section of the named `contract` export:

```js
export const contract = {
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

The empty string `""` represents the default unnamed slot. Named keys represent named Web Component slots.

Slot metadata currently supports:

- `description`: Human-readable description of the slot.
- `required`: Optional boolean indicating whether consumers are expected to provide content for the slot.

The `slots` section is contract/documentation metadata. It does not create or render `<slot>` elements. A component implementation must define the
corresponding `<slot>` elements itself.

## Related documentation

- [Components](index.md)
- [Component Contract](manifest.md)
