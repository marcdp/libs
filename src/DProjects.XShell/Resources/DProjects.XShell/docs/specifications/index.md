# Specifications

This section documents the formal configuration formats and contracts consumed by XShell bootstrap.

## Status

Draft.

## Documents

- [Application Specification](application.md) — Application metadata, module sources, and application-level overrides.
- [Module Specification](module.md) — Module metadata, resources, menus, handlers, and contributed global settings.

## Format

Checked-in application and module files use the `.jsonc` extension, and bootstrap strips line and block comments before parsing the result as JSON.

## TODO

TODO: Produce formal schemas only after field ownership, validation, defaults, compatibility, and extension rules are settled.

## Related documentation

- [XShell documentation](../)
- [JSONC Specifications ADR](../adr/0002-jsonc-specifications.md)
- [Modules](../architecture/modules.md)
