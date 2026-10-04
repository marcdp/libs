# Identity

This document records the current identity-subsystem status in XShell.

## Status

Not implemented.

## Current runtime behavior

XShell does not currently resolve an identity provider, create an identity object, expose `xshell.identity`, or register an `identity` runtime service.
The `xshell.identity` configuration shape and the `idp` resolver rule remain in the default configuration, but they do not activate identity runtime
behavior.

## Future work

Define the identity shape, provider lifecycle, configuration semantics, and authorization responsibilities before introducing identity runtime
behavior.

## Related documentation

- [Subsystems](index.md)
- [Authentication](60-authentication.md)
