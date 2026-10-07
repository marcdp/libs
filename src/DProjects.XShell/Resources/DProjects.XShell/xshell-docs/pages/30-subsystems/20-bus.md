# Bus

`bus` is the core message service used by modules, Areas, Navigation, and Pages.
Bus events are asynchronous fire-and-forget notifications. `emit()` queues delivery and returns immediately. Listener completion is not awaited by the
emitter.
Bus communicates facts and events, not synchronization or request-response operations.

| API | Behavior |
| --- | --- |
| `addEventListener(name, listener)` | Adds a named listener; `*` listens to all events. |
| `removeEventListener(name, listener)` | Removes registrations of that function for that name. |
| `emit(name, detail)` | Posts asynchronously through MessageChannel; returns no completion promise. |

Listeners receive `{ type, ts, detail }`, with `ts` set by `Date.now()` at emission.
MessageChannel structured-clones details, which must therefore be cloneable. Named listeners run before wildcard listeners.
Listener return values/promises are not awaited.

```js
controller({ bus, events }) {
    return {
        load() {
            events.on(bus, "orders:changed", event => console.log(event.detail));
        },
        save() {
            bus.emit("orders:changed", { id: 42 });
        }
    };
}
```

The `events` helper removes this registration on final unload. With direct registration, retain the function and explicitly remove it on destruction.
Bus messages have no DOM bubbling, composed path, or cancellation.
`module.contract.events` and service event contracts are metadata, without automatic validation/dispatch.

Area consumers listen for `xshell:area:change` and `xshell:menus:change`; Navigation emits `xshell:navigation:end`.
See [Areas](10-areas.md), [Navigation](../architecture/120-navigation.md), and [Component Events](../components/40-events.md).
