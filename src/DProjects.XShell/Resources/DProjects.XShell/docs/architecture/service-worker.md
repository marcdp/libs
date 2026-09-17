# Service Worker

This document outlines how the XShell service worker maps stable application asset URLs to framework and module source locations.

## Status

Draft.

## Registration

Bootstrap registers the configured service-worker script at the application base scope, waits for it to become ready and control the page, and sends rewrite rules through a message channel.

## Rewrite rules

Rules associate an application-visible source prefix with a destination directory and include module name, version, and exception data. Fetch handling applies the matching rule before requesting the destination resource.

## Current namespace

The checked-in XShell configuration sets `xshell.assetsPrefix` to `_assets`. Bootstrap therefore constructs paths such as `/_assets/xshell` and `/_assets/<module>` relative to the application base.

## TODO

TODO: Define caching, upgrades, offline behavior, and error recovery. Existing cache-related code is not sufficient to claim an offline contract.

## Related documentation

- [Architecture](index.md)
- [Asset URL Namespace](../adr/0001-asset-url-namespace.md)
