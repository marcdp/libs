# Module Specification

This document provides a conservative outline of the XShell module JSONC configuration.

## Status

Draft.

## Loading and merge

Bootstrap discovers module sources from the application configuration, parses each JSONC document, assigns defaults, normalizes URLs against the module asset path, and merges fields beneath `modules.<name>.*`.

## Confirmed fields

No field in this table has a formally specified required status yet. Bootstrap currently supplies defaults for several fields, but that does not establish
a published schema requirement.

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `name` | String | Not yet specified | Present in checked-in modules. Bootstrap replaces the merged value with the application-side module alias. |
| `label` | String | Not yet specified | Module display label. Defaults to an empty string during bootstrap; the runtime module record falls back to its name. |
| `icon` | String | Not yet specified | Module icon name. Bootstrap currently defaults it to `x-file`. |
| `version` | String | Not yet specified | Used in the module's service-worker rewrite rule. Defaults to an empty string. |
| `depends` | Array of strings | Not yet specified | Defaults to an empty array. No runtime dependency ordering or enforcement was found. |
| `styles` | Array of strings | Not yet specified | Stylesheets resolved and loaded before module handler load commands run. Defaults to an empty array. |
| `handler` | String | Not yet specified | Module-relative JavaScript handler loaded through the generated `module:` resolver. |
| `page.renderEngine` | String | Not yet specified | Module-level page render-engine override. |
| `page.stateEngine` | String | Not yet specified | Module-level page state-engine override. |
| `component.renderEngine` | String | Not yet specified | Module-level component render-engine override. |
| `component.stateEngine` | String | Not yet specified | Module-level component state-engine override. |
| `menus.<menu>[.<area>]` | Array | Not yet specified | Menu items consumed by the Menus service, optionally associated with an area. |
| `global.*` | String in current bootstrap paths | Not yet specified | Contribution copied to the shared configuration after removing `global.`; resolver contributions gain module metadata. |

## Global contributions

Keys prefixed with `global.` are merged into the global configuration after removing that prefix. Resolver contributions receive module and module-path metadata.

## TODO

TODO: Define dependency semantics, handler commands, menu schema, override precedence, extension points, and compatibility rules before publishing a formal schema.

## Sample module.jsonc file

```
{
    // module
    "name": "module1",
    "label": "Module 1 title",
    "icon": "module1-add",
    "version": "0.1.5",
    "handler": "/module.js",
    "styles": ["/css/styles.css"],

    "page.renderEngine": "x",
    "page.stateEngine":"proxy",
    

    // menu
    "menus.main.main": [{
        "label": "Amazon S3",
        "href": "/pages/page0.html",
        "children": [
            { "label": "Block Public Access settings for this account BIG", "href": "/pages/page1.html" },
            { "label": "Page 2", "href": "/pages/page2.html" },
            { "label": "Page 3", "href": "/pages/page3.html" },
            { "label": "Page 4", "href": "/pages/page4.html" },
            { "label": "Objects", "href": "/pages/page5.html", "children":[
                { "label": "Es un hecho establecido hace ", "href": "/pages/page6.html" }
            ] },                
            { "label": "-"},
            { "label": "Page 7", "href": "/pages/page7.js" },
            { "label": "Page 8", "href": "/pages/page8.js" },
            { "label": "Page Not found", "href": "/pages/page-not-found.html" }                
        ]
    }],
    "menus.tools": [
        { "label": "My things", "href": "/pages/page1.html", "aicon":"x-bell" },
        { "label": "Your things", "href": "/pages/page2.html"  }
    ],

    // global areas
    "global.xshell.areas.main.home": "/pages/page0.html"
}
```

## Related documentation

- [Specifications](index.md)
- [Application Specification](application.md)
- [Modules](../architecture/modules.md)
- [Configuration Architecture](../architecture/configuration.md)
