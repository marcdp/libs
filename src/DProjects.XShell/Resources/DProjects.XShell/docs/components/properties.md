# Component Properties

This document establishes properties as the public programmatic API of an XShell component.

## Status

Draft.

## Conceptual contract

Properties are values that component consumers may read or write. They are distinct from internal state, even when a component explicitly
synchronizes a property with a state value.

`contract.properties[*].default` is the canonical default for every public property. A definition must not supply a competing public-property
default through `definition.state`.

## Attributes

Current contract metadata can mark a property with `attribute`. Runtime state definitions separately use `attr` to observe an HTML attribute and
`reflect` to propagate selected state changes back to an attribute.

Contract property metadata includes `type`, `default`, `state`, `attribute`, `reflect`, `query`, `required`, `readonly`, `enum`, and
`description` where applicable. The shared contract schema validates the metadata shape; Page-specific rules for `query` are enforced by the Page
loader.

## Property-to-state synchronization

A contract may mark a property with `state: true`, expressing an intended explicit relationship with internal state. This must not be interpreted
to mean that every property automatically maps to state.

For a state-backed property, `definition.state` may repeat the property name only as a readability aid. The repeated value must be structurally
equal to the contract default, including nested arrays and plain objects. The loader validates that equality and always initializes runtime state
from `contract.properties[*].default`; it ignores the duplicate definition value after validation. A property without `state: true` must not appear
in `definition.state`.

The runtime loader creates public accessors from the contract. State-backed properties use the corresponding runtime state entry; other public
properties retain their contract default independently of internal state.

## Query-string initialization

`query: true` is an explicit opt-in used only by the Page loader. It allows the initial value of a state-backed property to be supplied by the query
string in the Page `src`; normal Components accept the metadata but do not read browser or Page URL query values.

The query parameter name is the property name converted from camelCase to kebab-case. For example, `customerId` maps to `customer-id` and
`pageIndex` maps to `page-index`. A query value overrides the contract default before the controller is created and before its `load()` handler runs.

`query: true` requires `state: true` and supports only `string`, `number`, `integer`, and `boolean` properties. Strings are preserved, numbers must be
finite, integers must be whole numbers, and booleans accept `true`, `1`, `false`, or `0` (case-insensitive for the words). Empty string values are
valid for strings; malformed numeric or boolean values reject Page creation. If the parameter is absent, the contract default remains unchanged.

Query initialization is input-only. It does not imply `attribute` or `reflect`, does not update the browser URL, and does not add state-to-query
reflection.

## Related documentation

- [Components](index.md)
- [State](state.md)
- [Component Contract](manifest.md)
- [Properties and State ADR](../adr/0004-properties-and-state.md)
