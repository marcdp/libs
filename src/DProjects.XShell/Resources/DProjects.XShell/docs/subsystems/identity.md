# Identity

This document describes the minimal identity shape currently exposed by XShell.

## Status

Draft.

## Current shape

Built-in identity providers return a frozen object containing `id`, `name`, `roles`, and `claims`. XShell registers the resolved identity as a runtime service named `identity`.

## Roles and claims

The checked-in providers carry role and claim values but do not by themselves establish authorization policy or enforcement semantics.

## Default identity

Framework configuration selects the anonymous provider by default. That provider returns the identifier `anonymous`, the display name `Anonymous`, and empty role and claim collections.

## TODO

TODO: Define identity immutability depth, role and claim formats, refresh behavior, and authorization responsibilities.

## Related documentation

- [Subsystems](index.md)
- [Authentication](authentication.md)

