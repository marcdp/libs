# Application Specification

This document provides a conservative outline of the XShell application JSONC configuration.

## Status

Draft.

## Loading

Bootstrap obtains the application configuration URL from the `xshell.app_config_url` meta element, fetches the file, removes JavaScript-style comments, parses JSON, normalizes URL-like values, and merges it over framework defaults.

## Confirmed groups

The sample application uses `app.*` metadata, `modules.<name>.src` module references, module parameters, XShell debugging and identity settings, and area configuration. These examples do not yet constitute a complete schema.

## URL values

The current normalizer gives special treatment to values prefixed with `url:` and to root-relative or dot-relative strings. The precise normalization contract remains a TODO.

## TODO

TODO: Define required fields, allowed types, unknown-key handling, environment overrides, validation errors, and version compatibility.

## Related documentation

- [Specifications](index.md)
- [Module Specification](module.md)
- [JSONC Specifications ADR](../adr/0002-jsonc-specifications.md)

## Sample app.jsonc file

```
{
    // app
    "app.name": "sample-app",
    "app.label": "Sample app",
    "app.version": "0.1.0",
    "app.copyright": "",
    "app.icon": "",
    // module x
    "modules.x.src": "url:../../modules/x/module.jsonc",
    "modules.x-debugger.src": "url:../../modules/x-debugger/module.jsonc",
    "modules.x-help.src": "url:../../modules/x-help/module.jsonc",
    // module 1
    "modules.module111.src": "url:./modules/module1/module.jsonc",
    "modules.module111.params.var1": "11111",
    "modules.module111.params.var2": "2222",
    // module 2
    "modules.module2.src": "url:./modules/module2/module.jsonc",
    // xshell
    "xshell.debug": true,
    // xshell identity    
    "xshell.identity.provider": "config",
    "xshell.identity.params.id": "123",
    "xshell.identity.params.name": "login1",
    "xshell.identity.params.roles": [ "role1", "role2" ],
    "xshell.identity.params.claims": {
        "xxxxxxxxxxx": "yyyyyyyyyyyyyy",
        "aaaaaaaaaa": "bbbbbbbbbbbbb",
        "cccccccccc": "ddddddddddd"
    },
    // xshell areas
    "xshell.areas.main.label": "Main area",
    "xshell.areas.main.icon": "x-bell"
    //"xshell.areas.main.home": "/module111/pages/page0.html"

}
```
