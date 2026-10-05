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

## Template validation

Component templates may declare native `<slot>` elements. Every slot reported by the selected render-engine factory must be explicitly declared in
`contract.slots`.
The default slot, `<slot></slot>`, maps to the empty-string contract key. A named slot, such as `<slot name="actions"></slot>`, maps to the
corresponding named key.

For example, this contract and template agree:

```js
export const contract = {
    slots: {
        "": {
            description: "Default content"
        },
        actions: {
            description: "Action buttons"
        }
    }
};
```

```html
<slot></slot>
<slot name="actions"></slot>
```

With the `x` engine, this template is invalid because `actions` is absent from the public component contract:

```js
export const contract = {
    slots: {
        "": {
            description: "Default content"
        }
    }
};
```

```html
<slot name="actions"></slot>
```

Component loading fails when factory metadata reports a slot not declared in `contract.slots`. Duplicate occurrences of the same slot name
are allowed; the contract declares the slot interface, not each insertion point. A slot declared in `contract.slots` is not required to appear in
the template. XTemplate slot names are static; dynamic `name` bindings are rejected. The server compiler deduplicates slot names in first-seen order
and the render-engine factory exposes them through `factory.slots`. Validation occurs when the component definition is loaded, before the component is
registered, and compares that factory metadata with the contract. It never scans raw template source in the browser. Validation belongs to the
component contract/loader layer, not to the render engine.

Slot metadata currently supports:

- `description`: Human-readable description of the slot.
- `required`: Optional boolean indicating whether consumers are expected to provide content for the slot.

The `slots` section is contract/documentation metadata. It does not create or render `<slot>` elements. A component implementation must define the
corresponding `<slot>` elements itself, and each factory-reported slot must be represented in the contract.

## Related documentation

- [Components](index.md)
- [Component Contract](10-manifest.md)

## V0 limits

The `x` engine reports slots from compiler metadata. `html` currently reports `[]`, so the loader does not discover its raw slots.
`required` is consumer guidance, without runtime content-presence enforcement.
The Page loader rejects factory-reported slots rather than supporting a public Page slot contract.
