# Component Contract and Manifest Direction

This document distinguishes the checked-in component contract export from the proposed manifest concept.

## Status

Draft.

## Current export

```js
export const contract = {
    description: "A sample component.",
    properties: {
        label: {
            type: "string",
            default: "",
            attribute: true,
            state: true
        }
    },
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

Representative checked-in components such as `x-datafields` and `x-error` export this metadata as `contract`. Observed top-level fields are
`description`, `events`, `properties`, `methods`, and `slots`. Observed property metadata includes `type`, `default`, `attribute`, `state`, and
`description`; event metadata can include `description` and a typed `detail` shape.

## Slots

The optional `slots` section declares the slots that a Web Component exposes as part of its public composition API. Each key is a Web Component
slot name. The empty string `""` represents the default unnamed slot; named keys, such as `"header"`, represent named Web Component slots.

Currently supported slot metadata is:

- `description`: Human-readable description of the slot.
- `required`: Optional boolean indicating whether consumers are expected to provide content for the slot.

`slots` is contract and documentation metadata. It does not itself create or render `<slot>` elements; the component implementation remains
responsible for defining the corresponding slots.

These observations describe the checked-in files only. They do not establish validation rules or runtime support for every field.

## Runtime relationship

The component loader reads `module.contract` and passes it into class construction, but class construction does not currently make substantial use of
the object. The loader separately consumes the default runtime definition, whose fields include `meta`, `style`, `template`, `state`, and `script`.
The component-level `script(...)` function is unrelated to a module definition's `controller` property. It returns named command and event handlers,
such as `load`, `stateChange`, or `click`. The loader retains those handlers in a private script object and dispatches each command to its matching
function with the component instance as `this`; it does not assign lifecycle handlers onto the `HTMLElement` instance. Methods declared by the
component contract remain available as public component methods.

## Future manifest concept

A manifest is an architectural direction for separating public metadata from runtime implementation details. It could describe what a consumer may
set, observe, or call without exposing all internal state. `manifest` is not the current export name and has no finalized runtime contract.

## TODO

TODO: Decide whether and how the future manifest concept supersedes or wraps the current `contract` export, and define validation and registration
semantics.

## Related documentation

- [Components](index.md)
- [Properties](properties.md)
- [Slots](slots.md)
- [State](state.md)
