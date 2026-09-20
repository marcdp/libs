# Root Module and Application Metadata

The application **is** the root module. The host's historical `xshell.app_config_url` meta value points to its `module.jsonc`. Recursive
imports compose the application; there is no separate application specification.

```jsonc
{
    "app": {
        "name": "example",
        "label": "Example"
    },
    "modules": {
        "app": {
            "script": "/js/module.js",
            "imports": [
                { "url": "url:../customers/module.jsonc" },
                { "url": "url:../inventory/module.jsonc" },
                { "url": "url:../reports/module.jsonc" }
            ]
        }
    },
    "xshell": {
        "areas": {
            "default": "customers",
            "definitions": {
                "customers": { "prefix": "/customers", "label": "Customers", "home": "/pages/home.js", "modules": ["customers", "reports"] },
                "inventory": { "prefix": "/inventory", "label": "Inventory", "modules": ["inventory", "reports"] }
            }
        }
    }
}
```

`app` contains application metadata. `modules.app` is the root's canonical definition and `xshell` composes Areas. The two Areas select
the same `reports` module, yielding separate effective menus but one live `reports` instance. Child modules provide named menu contributions;
they do not choose their Area. This example shows the **intended composition**. Current bootstrap rewrites the leading-slash Area prefixes,
and prefixed page URLs do not resolve to module resources yet. See [Areas](../subsystems/areas.md#current-implementation-limits).

Bootstrap uses the first key of the root file's `modules` object as `xshell.module.root`. The optional `xshell.app_params` host meta value becomes the
root definition's `params`. The framework default `xshell.module.rootParams` exists but bootstrap does not fill it from that value. Root
status is application composition information, not a `modules.app.root` flag.

The checked-in `modules/test/module.jsonc` follows this nested shape. Required fields and validation rules are not finalized. The server
command still defaults to the legacy `samples/sample1/app.jsonc`, so its default host path does not demonstrate this root-module format.

See [Module Specification](module.md), [Bootstrap](../architecture/bootstrap.md), and [Areas](../subsystems/areas.md).
