# Identity

This document records the current identity-subsystem status in XShell.

## Status

Not implemented.

## Current runtime behavior

XShell does not currently resolve an identity provider, create an identity object, or register an `identity` runtime service. V0 has no
`xshell.identity` configuration surface or `idp` resolver entry.

## Future work

Define the identity shape, provider lifecycle, configuration semantics, and authorization responsibilities before introducing identity runtime
behavior.

## Related documentation

- [Subsystems](index.md)
- [Authentication](60-authentication.md)
