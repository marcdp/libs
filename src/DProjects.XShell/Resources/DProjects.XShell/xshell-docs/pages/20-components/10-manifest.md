# Component Contract

A definition-based Component or Page may export `contract` alongside its default implementation:

```js
export const contract = {
    description: "Provides a counter API.",
    properties: { count: { type: "integer", default: 0, state: true } },
    methods: { reset: { description: "Resets the count.", returns: { type: "void" } } },
    events: { change: { detail: { count: { type: "integer", required: true } }, bubbles: true } },
    slots: {},
    examples: [{ name: "basic", template: "<example-counter></example-counter>" }]
};
export default {
    meta: { renderEngine: "html", stateEngine: "proxy" },
    template: "<span>Counter</span>",
    controller({ state }) {
        return { reset() { state.count = 0; } };
    }
};
```

The contract schema validates metadata shape before the generated class is returned. The loader also validates the implementation schema and
property/state consistency. Class default exports bypass definition processing and provide their own registration and API.

## Schema surface

`component.contract.schema.json` accepts only these top-level fields; none is required:

| Field | Shape and role |
| --- | --- |
| `description` | String for documentation. |
| `properties` | Map of public property descriptions. Each entry requires `type`. |
| `events` | Map of events with `description`, `detail`, `bubbles`, `composed`, and `cancelable`. |
| `methods` | Map of methods with `description`, `parameters`, and `returns`. |
| `slots` | Map of slot names with `description` and `required`; `""` is the default slot. |
| `examples` | Array with required `name` and nonempty `template`, optional `description` and `state`. |

Value types are `string`, `number`, `integer`, `boolean`, `object`, `array`, `function`, `any`, `date`, and `void`. These are small type labels,
not nested structural types. Event `detail` maps field names to entries with required `type`, optional `description` and `required`.
Method parameters require `name` and `type`, with optional `description`, `required`, and `default`. A `returns` entry requires `type` and may
contain `description`. Example names start with a letter and then use letters, digits, `_`, or `-`.

## Property metadata and runtime behavior

| Field | V0 behavior |
| --- | --- |
| `type` | Required type label; used for attribute/Page query conversion, without general assignment validation. |
| `default` | Canonical public-property default. State-backed defaults enter the state skeleton. |
| `state` | `true` connects a Component accessor to state and includes the property in Page state. |
| `attribute` | Boolean only; `true` observes the derived kebab-case HTML attribute for incoming changes. |
| `reflect` | Components reflect to kebab-case attributes; Pages reflect query-enabled state properties to the Page query. |
| `query` | Page-only input binding; requires `state: true` and string/number/integer/boolean type. |
| `context` | Page-only initialization from creation context, applied after query initialization. |
| `required`, `readonly`, `enum` | Descriptive metadata; no assignment, presence, or mutability enforcement. |
| `description` | Documentation metadata. |

Component `reflect: true` emits an attribute even
without `attribute: true`; that flag controls incoming observation. State-backed reflection requires an engine change notification.
Pages do not install Component-style public property accessors or HTML attribute bindings. See [Properties](20-properties.md) and
[Pages](../architecture/70-pages.md) for conversions and query/context behavior.

Public methods require callable controller methods with the same names and cannot collide with framework/native members. Parameter and return
metadata does not add runtime checking. Component proxies forward all arguments; the current Page command bridge forwards only its first parameter.

Event metadata does not dispatch CustomEvents, validate payloads, or configure emitted flags. Slots are checked against `factory.slots`:
`x` reports compiler-produced slot names, while `html` currently reports none. Slot `required` does not enforce supplied content.
Examples are documentation/tooling data.

See [State](30-state.md), [Events](40-events.md), [Slots](50-slots.md), and [Services](../architecture/100-services.md) for the separate service contract.
