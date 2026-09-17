# Module Specification

This document provides a conservative outline of the XShell module JSONC configuration.

## Status

Draft.

## Loading and merge

Bootstrap discovers module sources from the application configuration, parses each JSONC document, assigns defaults, normalizes URLs against the module asset path, and merges fields beneath `modules.<name>.*`.

## Confirmed fields

Checked-in modules use `name`, `label`, `icon`, `version`, `depends`, `styles`, and optional `handler`. They also configure page and component engines, menus, dialog pages, resolver entries, and other global values.

## Global contributions

Keys prefixed with `global.` are merged into the global configuration after removing that prefix. Resolver contributions receive module and module-path metadata.

## TODO

TODO: Define dependency semantics, handler commands, menu schema, override precedence, extension points, and compatibility rules before publishing a formal schema.

## Sample app.jsonc file

```
{
    // module
    "name": "module1",
    "label": "Module 1 title",
    "icon": "module1-add",
    "version": "0.1.5",
    "handler": "/module.js",
    "depends": [],
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

