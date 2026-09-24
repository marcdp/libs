# Component Lifecycle

This document outlines the lifecycle implemented by the JavaScript component loader.

## Status

Draft.

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
`mount`, `unmount`, and `unload`, controller methods, public methods, contracts, property and attribute semantics, services, and navigation.

## Definition loading

The loader imports the component module, prepares style, state, and render engines, initializes the render engine factory, and defines a custom
element for the requested resource name.

## Construction and loading

Construction creates a shadow root, creates state through the state engine, exposes selected services to the component script, retains its returned
named handlers privately, and invokes the `load` handler. A handler runs with the component instance as `this`, so it can use component APIs such as
`dispatchEvent` and `shadowRoot` without replacing runtime lifecycle methods.

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

The runtime keeps controller, state, `Timer`, `Events`, and registered `_disposables` for the complete `load` → `unload` lifetime. Render-engine
instances and page-specific adopted stylesheets are mount-owned resources; they are created/adopted for `mount` and unmounted/de-adopted for
`unmount`.

For definition-based components, ordinary DOM disconnection invokes only `unmount`; it does not invoke `unload` or dispose helpers. A caller that
knows a component is being destroyed permanently must explicitly call its idempotent `unload()` method. The browser custom-element API has no
reliable universal signal that distinguishes a temporary disconnection from permanent destruction.

For Pages, `x-page` unmounts its current Page when disconnected and remounts the same Page on reconnection. Replacing a Page, or using the
explicit `removePage()` destruction path, unmounts the old Page and then unloads it before discarding the instance. `unload()` is idempotent as a
safety measure, but lifecycle owners should not use it as normal replacement control flow.

Invalidation is also mount-scoped. Requests made before mount or after unmount do not render, and an animation-frame callback queued for a prior
render engine is ignored once that engine has been unmounted or replaced.

## Related documentation

- [Components](index.md)
- [State](state.md)
- [Loaders](../architecture/loaders.md)
