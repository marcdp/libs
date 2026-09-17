# X Template Expressions

This document describes how expressions are embedded in X templates without attempting to define a separate expression language.

## Status

Draft.

## Evaluation model

The current compiler inserts directive values and interpolation contents into a generated JavaScript render function. Expressions therefore use JavaScript syntax and execute with parameters including `state`, handler and invalidation callbacks, utility functions, internationalization, and a render count.

## Common contexts

Expressions appear in text interpolation, conditionals, iteration sources, keys, attributes, properties, classes, visibility, child-node values, and model bindings.

## Safety and errors

Compilation uses the `Function` constructor. The repository does not currently define a sandbox, expression allow-list, or formal diagnostic contract.

## TODO

TODO: Define scoping, error reporting, trusted-template requirements, and Content Security Policy implications before treating expression evaluation as a stable public specification.

## Related documentation

- [X Templates](index.md)
- [Syntax](syntax.md)
- [Compiler](compiler.md)

