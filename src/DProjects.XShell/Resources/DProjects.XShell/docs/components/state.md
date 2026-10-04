# Component State

State is instance-owned implementation data. Public properties are a separate API even when explicitly backed by state.

## Defaults and initialization

The loader builds one skeleton from private `definition.state` entries and defaults of `contract.properties` with `state: true`.
Public defaults always come from `contract.properties[*].default`. A state-backed public name may also appear in `definition.state` only with a
structurally equal value, including nested arrays/objects. The loader checks equality and retains the contract default.
A non-state-backed public property cannot appear in implementation state.

State entries are ordinary values, not descriptors with `type`, `attr`, `prop`, or `reflect` flags. Public-property behavior belongs to the contract.

```js
export const contract = {
    properties: { count: { type: "integer", default: 0, state: true } }
};
export default {
    template: '<span x-text="state.count"></span>',
    state: { busy: false },
    controller({ state }) {
        return { increment() { state.count += 1; } };
    }
};
```

For Pages, query-enabled values are initialized before controller construction; context-enabled values are copied afterwards and may override
query values. See [Pages](../architecture/pages.md).

## Engine selection

Resource `meta.stateEngine` overrides the owning module's `defaults.component.stateEngine` or `defaults.page.stateEngine`.
There is no engine fallback under `xshell.ui`.

| Engine | Implemented behavior |
| --- | --- |
| `none` | Returns `null`; use only definitions without state access, state-backed properties, or Page query/context writes. |
| `plain` | Returns a JSON-cloned skeleton; mutations do not notify or invalidate rendering. |
| `proxy` | Returns a JSON-cloned skeleton behind a Proxy; observes top-level assignment and notifies the loader. |

JSON cloning does not preserve functions, prototypes, Date instances, or undefined members and rejects cycles.
Non-state-backed Component properties are stored separately; their object defaults are not cloned by the state engine.

## Proxy changes

`proxy` observes `state.name = value`, not `state.item.label = value` or `state.items.push(value)`.
Assign a fresh object or array to make a change observable:

```js
state.items = [...state.items, newItem];
state.item = { ...state.item, label: "Updated" };
```

The comparison uses loose inequality for scalars, so assigning `"0"` over `0` is ignored, retaining the old value without notification. Object
reassignment can notify even with the same
reference; assigning the same array reference with its current length does not detect earlier in-place changes. This is not deep change detection.

The engine exposes `addEventListener("change:<property>", listener)` and corresponding removal.
Listeners receive `{ prop, newValue, oldValue }`. A detected assignment invokes the loader's change handler and invalidation callback.
The loader batches `stateChange` and rendering through `requestAnimationFrame`; reflected state properties depend on these notifications.

## State-engine boundary

The engine owns state creation, change detection, notification, and invalidation. It does not own contracts, public API, controllers, lifecycle,
rendering, or navigation. The loader coordinates engines, lifecycle, and queued commands.

See [Properties](properties.md), [Lifecycle](lifecycle.md), [Rendering Engines](rendering.md), and [ADR-0004](../adr/0004-properties-and-state.md).
