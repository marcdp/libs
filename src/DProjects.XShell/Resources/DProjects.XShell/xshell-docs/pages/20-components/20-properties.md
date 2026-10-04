# Component Properties

This document establishes properties as the public programmatic API of an XShell component.

## Conceptual contract

Properties are values that component consumers may read or write. They are distinct from internal state, even when a component explicitly
synchronizes a property with a state value.

`contract.properties[*].default` is the canonical default for every public property. A definition must not supply a competing public-property
default through `definition.state`.

## Attributes

Component observation requires literal `attribute: true`. Names are converted from camelCase to kebab-case.
Although the schema also accepts string aliases, the loader ignores them.

Attribute conversion handles strings directly, numbers with `Number`, booleans as presence except values `"false"`/`"0"`,
and object/array text with `JSON.parse` (falling back to raw text on failure). Integer/date/function/any labels have no special conversion.
These conversions do not enforce all contract types on programmatic assignment.

Empty-object state entries also enable prefixed attribute maps: for example `settings-color="red"` fills `state.settings.color`.
For public entries this needs `state: true`, `attribute: true`, and an empty-object default.
Private empty-object state gets this behavior without descriptor flags. Present map attribute values are strings; removal writes null.

Contract property metadata includes `type`, `default`, `state`, `attribute`, `reflect`, `query`, `context`, `required`, `readonly`, `enum`, and
`description` where applicable. The shared contract schema validates the metadata shape; Page-specific rules for `query` are enforced by the Page
loader.

For Components, `reflect: true` writes the derived kebab-case attribute even when incoming observation was not enabled.
False/null/undefined remove it, true writes an empty attribute, and other values use `setAttribute` coercion, not JSON serialization.
State-backed reflection needs engine notifications; non-state-backed Component setters reflect directly.
For Pages, reflection requires `query: true` as well. No HTML property/attribute bindings are installed on the generated Page class.

`required`, `readonly`, and `enum` are metadata, not runtime value/presence/mutability checks.

## Property-to-state synchronization

A contract may mark a property with `state: true`, expressing an intended explicit relationship with internal state. This must not be interpreted
to mean that every property automatically maps to state.

For a state-backed property, `definition.state` may repeat the property name only as a readability aid. The repeated value must be structurally
equal to the contract default, including nested arrays and plain objects. The loader validates that equality and always initializes runtime state
from `contract.properties[*].default`; it ignores the duplicate definition value after validation. A property without `state: true` must not appear
in `definition.state`.

The Component loader creates public accessors from the contract; the Page loader does not create those accessors. State-backed properties use the
corresponding runtime state entry; other public
properties retain their contract default independently of internal state.

## Query-string initialization

`query: true` is an explicit opt-in used only by the Page loader. It allows the initial value of a state-backed property to be supplied by the query
string in the Page `src`; normal Components accept the metadata but do not read browser or Page URL query values.

The query parameter name is the property name converted from camelCase to kebab-case. For example, `customerId` maps to `customer-id` and
`pageIndex` maps to `page-index`. A query value overrides the contract default before the controller is created and before its `load()` handler runs.

`query: true` requires `state: true` and supports only `string`, `number`, `integer`, and `boolean` properties. Strings are preserved, numbers must be
finite, integers must be whole numbers, and booleans accept `true`, `1`, `false`, or `0` (case-insensitive for the words). Empty string values are
valid for strings; malformed numeric or boolean values reject Page creation. If the parameter is absent, the contract default remains unchanged.

For example, a reflected query-backed Page property follows this lifecycle:

```js
export const contract = {
    properties: {
        pageIndex: {
            type: "integer",
            default: 0,
            state: true,
            query: true,
            reflect: true
        }
    }
};
```

```text
/page.js?page-index=3  →  state.pageIndex === 3
state.pageIndex = 4    →  /page.js?page-index=4
state.pageIndex = 0    →  /page.js
```

`query: true` allows Page query initialization into state. Adding `reflect: true` also reflects later state changes to that Page's query; returning to
the contract default removes the query parameter.

Query initialization does not imply `attribute` or `reflect`. With `reflect: true`, later state changes patch the Page's own query parameters using
replacement semantics; properties without `reflect: true` remain input-only. This behavior is Page-specific and normal Components continue to ignore
`query` at runtime.

## Related documentation

- [Components](index.md)
- [State](30-state.md)
- [Component Contract](10-manifest.md)
- [Properties and State ADR](../adr/0004-properties-and-state.md)
