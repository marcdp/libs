# Authentication

This document records the current authentication-subsystem status in XShell.

## Status

Not implemented.

## Current runtime behavior

XShell currently has no authentication service, login flow, logout flow, or identity-provider orchestration. It does not load `idp:<provider>`
resources as part of startup and does not register an authentication or identity runtime service.

The `xshell.identity` configuration shape and the `idp` resolver rule remain in the default configuration, but they do not implement authentication.

## Future work

Define provider lifecycle, unauthenticated and error states, redirects, credential handling, and security requirements before adding an
authentication subsystem.

## Related documentation

- [Subsystems](index.md)
- [Identity](identity.md)
- [Application Specification](../specifications/application.md)
