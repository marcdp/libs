# Root Module and Application Metadata

The application **is** the root module. The host's `xshell:app.configPath` meta value points to its `module.jsonc`. Recursive imports compose the
application; there is no separate application specification.

```jsonc
{
    "app": {
        "name": "example",
        "label": "Example",
        "copyright": "",
        "icon": "",
        "version": "0.1.0",
        "basePath": "https://example.test/app",
        "params": { "tenant": "north" }
    },
    "modules": {
        "app": {
            "controller": "/js/module.js",
            "imports": [
                { "url": "url:../customers/module.jsonc" },
                { "url": "url:../inventory/module.jsonc" },
                { "url": "url:../reports/module.jsonc" }
            ]
        }
    },
    "xshell": {
        "environment": "development",
        "areas": {
            "default": "customers",
            "definitions": {
                "customers": { "prefix": "/customers", "label": "Customers", "modules": ["customers", "reports"] },
                "inventory": { "prefix": "/inventory", "label": "Inventory", "modules": ["inventory", "reports"] }
            }
        }
    }
}
```

`app` contains `name`, `label`, `copyright`, `icon`, and `version`, plus the host-derived `basePath` and `params`. `xshell.environment` is runtime and
hosting configuration, not application metadata. `modules.app` is the root's canonical definition and `xshell` composes Areas. The two Areas select
the same `reports` module, yielding separate effective menus but one live `reports` instance. Child modules provide named menu contributions;
they do not choose their Area. The imported reports module can declare a top-level navigation item marked `default: true`; Areas derives each
home from its first such contribution in module order. Bootstrap preserves Area prefixes, and `x-page` removes them for module resource
resolution. See [Areas](../subsystems/areas.md).

Bootstrap treats the first key of the root file's `modules` object as the root id while loading and normalizing the configuration. The optional
`xshell:app.params` host meta value becomes both the root definition's `params` and `app.params`. The effective configuration does not retain a
separate root id or root-params object, and there is no `xshell.module` section. Root status is application composition information, not a
`modules.app.root` flag.

The checked-in `modules/test/module.jsonc` follows this nested shape. Required fields and validation rules are not finalized. The server command
defaults to `/_resources/DProjects.XShell/modules/test/module.jsonc` when no explicit application config is supplied.

See [Module Specification](module.md), [Bootstrap](../architecture/bootstrap.md), and [Areas](../subsystems/areas.md).
