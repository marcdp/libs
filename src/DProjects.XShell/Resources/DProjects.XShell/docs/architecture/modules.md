# Modules

An XShell application is composed from one or more modules.

Conceptually:

```text
Application = Module 1 + Module 2 + ... + Module N
```

Each module contributes configuration and static resources to the application.

## Module structure

A module consists of:

```text
module = module.jsonc + static resources
```

A typical module can contain:

* `components/`
* `pages/`
* `layouts/`
* `icons/`
* `css/`
* `libs/`
* `utils/`
* an optional ES module handler such as `module.js`

## Module configuration

A module is described by a `module.jsonc` file.

For example:

```jsonc
{
    // module
    "name": "module1",
    "label": "Module 1 title",
    "icon": "module1-add",
    "version": "0.1.5",
    "handler": "/module.js",
    "depends": [],
    "styles": ["/css/styles.css"],

    // page defaults
    "page.renderEngine": "x",
    "page.stateEngine": "proxy",

    // menus
    "menus.main.main": [{
        "label": "Amazon S3",
        "href": "/pages/page0.html",
        "children": [
            {
                "label": "Page 1",
                "href": "/pages/page1.html"
            },
            {
                "label": "Page 2",
                "href": "/pages/page2.html"
            }
        ]
    }],

    "menus.tools": [
        {
            "label": "My things",
            "href": "/pages/page1.html",
            "aicon": "x-bell"
        },
        {
            "label": "Your things",
            "href": "/pages/page2.html"
        }
    ],

    // global areas
    "global.xshell.areas.main.home": "/pages/page0.html"
}
```

## Module-local configuration

Normal module fields are added to the runtime configuration under:

```text
modules.<module>.*
```

For example:

```text
page.renderEngine → modules.module1.page.renderEngine
styles            → modules.module1.styles
version           → modules.module1.version
handler           → modules.module1.handler
```

This keeps module-specific configuration isolated by module name.

## Global contributions

A module can also contribute configuration to the shared application namespace.

Keys beginning with:

```text
global.
```

have that prefix removed when the final runtime configuration is assembled.

For example:

```text
global.page.layout.main
→ page.layout.main

global.xshell.dialog.confirm
→ xshell.dialog.confirm

global.resolver.import:marked
→ resolver.import:marked
```

This allows a module to extend or configure application-wide behavior.

See [Configuration](configuration.md).

## Module resources

All resource paths beginning with `/` are relative to that module.

For example:

```jsonc
"styles": [
    "/css/styles.css"
]
```

for module `module1` becomes a runtime resource such as:

```text
/_cdn/module1/css/styles.css
```

The same applies to pages, components, icons, libraries, handlers, and other module-owned static files.

## Module handler

A module can optionally define a JavaScript handler through:

```jsonc
"handler": "/module.js"
```

The handler is an ES module whose default export is a module controller.

For example:

```js
export default class Module1Module {
    onCommand(command, params) {
        if (command == "load") {
            // module load
        }
    }
}
```

During module initialization, XShell loads the handler and sends it lifecycle commands.

The `load` command is called after the module handler and styles have been loaded and before initial navigation starts.

Module parameters from:

```text
modules.<name>.params.*
```

are passed to the handler's `load` command.

Conceptually:

```text
module.jsonc
    ↓
handler: /module.js
    ↓
load ES module
    ↓
create module controller
    ↓
onCommand("load", params)
```

## Generated resolvers

Bootstrap generates conventional resolvers for each module.

These provide standard locations for:

```text
icons
layouts
components
pages
JavaScript modules
```

For example, a module named `x` can expose resources through logical references such as:

```text
icon:x-{name}
layout:x-layout-{name}
component:x-{name}
```

and page resources under the module asset namespace.

See [Resolvers](resolvers.md).

## Application composition

The application configuration selects which modules participate in the application.

For example:

```jsonc
{
    "modules.x.src": "url:../../modules/x/module.jsonc",
    "modules.orders.src": "url:./modules/orders/module.jsonc",
    "modules.admin.src": "url:./modules/admin/module.jsonc"
}
```

Bootstrap loads each module definition and combines them into the final runtime configuration.

Conceptually:

```text
app.jsonc
    ↓
modules.x.src
modules.orders.src
modules.admin.src
    ↓
load module.jsonc files
    ↓
merge module configuration
    ↓
register module resources
    ↓
Application
```

## Runtime initialization

After configuration is assembled, XShell initializes the configured modules.

A module can contribute:

* stylesheets;
* configuration;
* global configuration;
* resolvers;
* pages;
* components;
* areas and menus;
* icons;
* JavaScript modules;
* an optional module handler.

Module handlers and styles are initialized before initial navigation starts.

## Related documentation

* [Bootstrap](bootstrap.md)
* [Configuration](configuration.md)
* [Components](components.md)
* [Pages](pages.md)
* [Resolvers](resolvers.md)
* [Module Specification](../specifications/module.md)
