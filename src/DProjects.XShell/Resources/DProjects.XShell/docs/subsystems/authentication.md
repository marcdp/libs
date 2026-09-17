# Authentication

This document introduces XShell's identity-provider orchestration.

## Status

Draft.

## Login flow

The authentication service reads `xshell.identity` configuration, loads an `idp:<provider>` resource, creates the provider, and calls its `resolve` method with configured parameters. An `authenticated` result supplies the runtime identity.

## Current providers

The repository contains `anonymous` and `config` providers. Both return an authenticated identity without an external protocol; they should not be interpreted as evidence of a complete security boundary.

## Logout

The authentication service clears its identity and calls the provider's `logout` method. Current built-in providers reload the document.

## TODO

TODO: Define provider lifecycle, unauthenticated and error states, redirects, credential handling, and security requirements.

## Related documentation

- [Subsystems](index.md)
- [Identity](identity.md)
- [Application Specification](../specifications/application.md)

