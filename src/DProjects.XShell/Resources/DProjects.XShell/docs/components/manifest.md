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
`description`, `events`, `properties`, `methods`, and `slots`.
Observed property metadata includes `type`, `default`, `state`, `attribute`, `reflect`, `query`, `required`, `readonly`, `enum`, and `description`.
Event metadata can include `description` and a typed `detail` shape.

`query` is shared contract metadata, but its runtime meaning is Page-specific: `query: true` allows the Page loader to initialize a state-backed
property from the Page `src` query string. When it is combined with `reflect: true`, later state changes patch that Page's query representation.
`reflect` applies to every external representation explicitly enabled by the property, so it does not imply `attribute` or `query`. The Component
loader validates and ignores `query`; it does not read or rewrite URL query values.

## Slots

The optional `slots` section declares the slots that a Web Component exposes as part of its public composition API. Each key is a Web Component
slot name. The empty string `""` represents the default unnamed slot; named keys, such as `"header"`, represent named Web Component slots.

Currently supported slot metadata is:

- `description`: Human-readable description of the slot.
- `required`: Optional boolean indicating whether consumers are expected to provide content for the slot.

`slots` is contract and documentation metadata. It does not itself create or render `<slot>` elements; the component implementation remains
responsible for defining the corresponding slots. The loader validates that every slot used by the template is declared in `contract.slots`.
The reverse is not required: a declared slot may be absent from the current template.

Duplicate uses of one slot name are allowed because the contract describes the public slot interface, not individual template insertion points.
An undeclared template slot causes component loading to fail before the component is registered. This validation belongs to the component
contract/loader layer, not to the render engine.

## Runtime relationship

The component loader reads `module.contract` and uses it to create the public property and method surface. It also validates template slot usage
against `contract.slots` before registering the component. Its property defaults are canonical:
state-backed public properties populate runtime state from `contract.properties[*].default`, while `definition.state` supplies private state. The
default runtime definition separately provides fields including `meta`, `style`, `template`, `state`, and `controller`.
The component-level `controller(...)` function returns named lifecycle, command, and event handlers such as `load`, `stateChange`, or `click`.
The loader retains that controller privately and invokes each handler with the controller object as `this`. The generated Web Component is available
through the injected `host` dependency. X Template handlers resolve against the controller, while methods declared by `contract.methods` are the
only controller methods exposed as public Web Component proxies. Contract method metadata is never used as the executable implementation.

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
