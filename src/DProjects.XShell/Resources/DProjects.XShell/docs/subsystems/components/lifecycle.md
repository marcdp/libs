# Component Lifecycle

This document outlines the lifecycle implemented by the JavaScript component loader.

## Status

Draft.

## Definition loading

The loader imports the component module, prepares style, state, and render engines, initializes the render engine factory, and defines a custom element for the requested resource name.

## Construction and loading

Construction creates a shadow root, creates state, exposes selected services to the component script, assigns returned methods to the instance, and invokes the `load` command.

## Mount and render

On connection, the runtime creates and mounts a render-engine instance, invokes `mount`, and schedules a render. State invalidations are coalesced through `requestAnimationFrame`; immediately before rendering, the runtime invokes `stateChange` with accumulated changes.

## Unmount and cleanup

On disconnection, the runtime invokes `unmount` and `unload`, unmounts the render engine, and disposes registered helpers.

## TODO

TODO: Define reconnection behavior, error propagation, command ordering guarantees, and whether `load` is intended to run once per element lifetime.

## Related documentation

- [Components](index.md)
- [State](state.md)
- [Loaders](../../architecture/loaders.md)
