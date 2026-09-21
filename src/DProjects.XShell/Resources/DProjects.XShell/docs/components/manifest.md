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
            attr: true,
            state: true
        }
    }
};
```

Representative checked-in components such as `x-datafields` and `x-error` export this metadata as `contract`. Observed top-level fields are
`description`, `events`, `properties`, and `methods`. Observed property metadata includes `type`, `default`, `attr`, `state`, and `description`;
event metadata can include `description` and a typed `detail` shape.

These observations describe the checked-in files only. They do not establish validation rules or runtime support for every field.

## Runtime relationship

The component loader reads `module.contract` and passes it into class construction, but class construction does not currently make substantial use of
the object. The loader separately consumes the default runtime definition, whose fields include `meta`, `style`, `template`, `state`, and `script`.
The component-level `script(...)` function is unrelated to a module definition's `controller` property. Methods returned by `script(...)` are assigned
directly to the `HTMLElement` instance with `Object.assign`, and lifecycle commands are dispatched through `onCommand`.

## Future manifest concept

A manifest is an architectural direction for separating public metadata from runtime implementation details. It could describe what a consumer may
set, observe, or call without exposing all internal state. `manifest` is not the current export name and has no finalized runtime contract.

## TODO

TODO: Decide whether and how the future manifest concept supersedes or wraps the current `contract` export, and define validation and registration
semantics.

## Related documentation

- [Components](index.md)
- [Properties](properties.md)
- [State](state.md)
