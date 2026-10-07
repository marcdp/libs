# Component Lifecycle

This document outlines the lifecycle implemented by the JavaScript component loader.

## Ownership boundaries

For definition-based Components and Pages, the loader owns the runtime composition and lifecycle:

```text
loader
 ├─ contract / properties / public API
 ├─ controller / lifecycle
 ├─ state engine → reactive state
 └─ render engine → rendered output
```

State engines own reactive state only. Render engines own rendered output only. The loader creates and coordinates both engines; it owns `load`,
`mount`, `unmount`, and `unload`, controller methods, public methods, contracts, property and attribute semantics, and service access. Page hosts and
Navigation own navigation-specific orchestration.

## Definition loading

The loader imports and validates the contract/implementation, prepares style and the state/render factories, validates factory-reported slots,
loads dependencies, initializes the render factory, and defines the custom element. State/controller instances are created on element construction.

Definition styles are processed once while the Component or Page class is created. Relative CSS URLs use the definition resource as their base;
recursive `@import` URLs use each imported stylesheet as their base. Page CSS is scoped after imports are expanded. Each Page mount creates its own
scoped stylesheet and removes it on unmount.

## Construction and loading

Construction creates a shadow root, creates state through the state engine, exposes selected services to the controller, retains its returned
named handlers privately, and invokes the `load` handler. Every controller handler runs with the controller object as `this`. The generated Web
Component is available only through the explicitly requested `host` dependency, so DOM access uses forms such as `host.dispatchEvent(...)` and
`host.shadowRoot`. Lifecycle handlers remain private controller methods rather than public Web Component methods.

## Mount and render

On connection, the loader creates and mounts a render-engine instance, invokes `mount`, and schedules a render. State invalidations are coalesced
through `requestAnimationFrame`; immediately before rendering, the loader invokes `stateChange` with accumulated changes.

## Instance and connection lifetimes

Component and Page lifecycle handlers have two different ownership scopes:

```text
instance creation
    load

connection #1
    mount
    unmount

connection #2
    mount
    unmount

...

final destruction
    unload
```

`load` and `unload` each run once for an instance. `mount` runs whenever that instance becomes connected or mounted, and `unmount` runs whenever
it becomes disconnected or unmounted. In particular, a native `disconnectedCallback()` is not evidence of permanent destruction: the browser can
reconnect the same custom-element instance later.

The controller context is stable across these calls. A lifecycle handler can use `this.refresh()` to invoke another controller method, while an
operation on the underlying custom element must use the injected `host`.

The runtime keeps controller, state, `Timer`, `Events`, and registered `_disposables` for the complete `load` → `unload` lifetime. Render-engine
instances and page-specific adopted stylesheets are mount-owned resources; they are created/adopted for `mount` and unmounted/de-adopted for
`unmount`.

For definition-based components, ordinary DOM disconnection invokes only `unmount`; it does not invoke `unload` or dispose helpers. A caller that
knows a component is being destroyed permanently must explicitly call its idempotent `unload()` method. The browser custom-element API has no
reliable universal signal that distinguishes a temporary disconnection from permanent destruction.

For Pages, `x-page` unmounts its current Page when disconnected and remounts the same Page on reconnection. Replacing a Page, or using the
explicit `removePage()` destruction path, unmounts the old Page and then unloads it before discarding the instance. `unload()` is idempotent as a
safety measure, but lifecycle owners should not use it as normal replacement control flow.
Closing a dialog uses `removePage()`: its Page unmounts and unloads before the dialog host is removed and the result promise completes.

Invalidation is also mount-scoped. Requests made before mount or after unmount do not render, and an animation-frame callback queued for a prior
render engine is ignored once that engine has been unmounted or replaced.

## Related documentation

- [Components](index.md)
- [State](30-state.md)
- [Loaders](../architecture/90-loaders.md)

## Awaiting handlers

Component construction invokes `load` without awaiting its result; connection/disconnection similarly invoke `mount`/`unmount` synchronously.
Explicit Component `unload()` awaits its handler and disposes helpers in a final cleanup path.
Page `load`, `mount`, `unmount`, and `unload` use asynchronous lifecycle methods that await their handlers.
Use the host's Page load sequence when completion matters.

See [Dialogs](../subsystems/30-dialogs.md) for dialog final destruction and result handling.
