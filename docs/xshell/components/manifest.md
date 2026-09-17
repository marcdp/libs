# Component Manifest

This document introduces component metadata as the description of a component's public contract.

## Status

Draft.

## Illustrative shape

```js
export const manifest = {
    properties: {
        label: {
            type: "string",
            default: "",
            attr: true,
            state: true
        }
    }
};
```

The manifest concept separates public metadata from runtime implementation details. It can describe what a consumer may set, observe, or call without exposing all internal state.

## Fields verified in current components

Current component files export this metadata under the name `declaration`, not `manifest`. Observed top-level fields are `description`, `events`, `properties`, and `methods`. Observed property metadata includes `type`, `default`, `attr`, `state`, and `description`; event metadata can include `description` and a typed `detail` shape.

These observations describe the checked-in files only. They do not establish validation rules or runtime support for every field.

## Runtime relationship

The component loader currently imports the default export as the runtime definition and does not consume the named `declaration`. That definition uses fields including `meta`, `style`, `template`, `state`, and `script`.

## TODO

TODO: Decide whether `manifest` or `declaration` is the public term and define how metadata is validated and connected to runtime component registration.

## Related documentation

- [Components](index.md)
- [Properties](properties.md)
- [State](state.md)

