# Authentication

This document records the current authentication-subsystem status in XShell.

## Status

Not implemented.

## Current runtime behavior

XShell currently has no authentication service, login flow, logout flow, or identity-provider orchestration. It does not register an
authentication or identity runtime service. V0 has no `xshell.identity` configuration or `idp` resolver entry.

## Future work

Define provider lifecycle, unauthenticated and error states, redirects, credential handling, and security requirements before adding an
authentication subsystem.

## Related documentation

- [Subsystems](index.md)
- [Identity](70-identity.md)
- [Application Specification](../specifications/application.md)
