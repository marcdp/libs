# Subsystems

Implemented V0 runtime capabilities:

- [Areas](10-areas.md) — Module menus, routes, and navigation homes.
- [Bus](20-bus.md) — Asynchronous application messages.
- [Dialogs](30-dialogs.md) — Helpers and results from dialog Pages.
- [i18n](40-i18n.md) — Language configuration, translations, and date/time formatting.
- [Temp](50-temp.md) — Upload/download identifiers and server cleanup.

[Authentication](60-authentication.md) and [Identity](70-identity.md) describe subsystems outside V0; there is no `xshell.identity` configuration.
XShell does not log in or register auth/identity services during startup.

See [Services](../10-architecture/100-services.md) for runtime registrations and configured service contracts.
