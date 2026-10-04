# Root Module and Application Metadata

The application **is** the root module. The host's `xshell:app.configPath` points to its `module.jsonc`; no flag inside the file marks it as root.
Bootstrap applies the same exactly-one-local-definition rule used for every module document.

```jsonc
{
    "app": {
        "id": "example",
        "label": "Example",
        "copyright": "",
        "icon": "",
        "version": "0.1.0"
    },
    "modules": {
        "app": {
            "label": "Application module",
            "version": "0.1.0",
            "copyright": "",
            "icon": "",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            }
        },
        "customers": { "configUrl": "url:../customers/module.jsonc" },
        "reports": {
            "configUrl": "url:../reports/module.jsonc",
            "params": { "mode": "compact" }
        }
    },
    "xshell": {
        "areas": {
            "default": "customers",
            "global": [],
            "definitions": {
                "customers": { "prefix": "/customers", "modules": ["customers", "reports"] }
            }
        }
    }
}
```

`modules.app` is local because it has no `configUrl`. The other entries are dependency references. The root has the additional responsibility of
application composition, and its references may contribute configuration just like child references. Every reference requires `configUrl`; no
reference may override `assetsUrl`, though other configuration properties are allowed.

The optional host `xshell:app.params` meta value becomes the local root definition's `params` and effective `app.params`. Bootstrap also supplies
`app.basePath`. No `root`, `isRoot`, `moduleType`, separate root id, or separate application file format is required.

Dependencies merge first and the root merges last. Root composition therefore has final authority. The effective `modules` object contains full
canonical definitions, and `xshell.modules` creates one runtime instance per id.

See [Module Specification](module.md), [Bootstrap](../architecture/bootstrap.md), and [Areas](../subsystems/areas.md).

## Application fields

The effective `app` schema requires `id`, `label`, `copyright`, `icon`, `version`, `basePath`, and `params`. The example above is authored
composition: host/bootstrap supply `basePath` and `params`. `app.configPath` is a bootstrap input, not an accepted effective `app` field.
An application id is metadata independent of the local module key. Extra fields are rejected by the effective schema.
