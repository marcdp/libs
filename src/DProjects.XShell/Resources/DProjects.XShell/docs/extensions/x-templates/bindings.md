# X Template Bindings

This document outlines the data and event binding forms recognized by the X template compiler.

## Status

Draft.

## Attribute bindings

`x-attr:name` and `:name` bind an expression to an HTML attribute. `x-attr` and `:` support object-style expansion. Dynamic argument names in brackets are present in the compiler but need focused tests.

## Property bindings

`x-prop:name` and `.name` bind an expression to a DOM property. The compiler converts kebab-case property names to camelCase.

## Event bindings

`x-on:event` and `@event` forward events to a named component command. Dot-separated event modifiers are interpreted later by the render engine.

## Class and model bindings

`x-class:name` conditionally includes a CSS class. `x-model` establishes a value/checked-style property binding and a change handler for supported form controls.

## TODO

TODO: Verify object expansion, dynamic names, modifier combinations, multi-select values, radio values, and model update timing with executable tests.

## Related documentation

- [X Templates](index.md)
- [Syntax](syntax.md)
- [Component Events](../../subsystems/components/events.md)
