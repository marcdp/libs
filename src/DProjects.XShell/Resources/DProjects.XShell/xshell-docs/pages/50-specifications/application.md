# Root Module and Application Metadata

The application is the root module selected by a release. `release.root` names its packaged `module.json`; development hosting selects the same
role through `xshell:app.configPath`. Any module can be selected as the root of a release. No `main`, `isRoot`, or `moduleType` field marks a module,
and there is no separate authored application descriptor or application package.

```jsonc
{
    "modules": {
        "app": {
            "label": "Example",
            "version": "1.0.0",
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

`modules.app` is local because it has no `configUrl`. The other entries are dependency references. The root composes the application and its
references may contribute configuration just like child references. Every reference requires `configUrl`; no reference may override `assetsUrl`.
Dependencies merge first and the root merges last. The effective `modules` object contains full canonical definitions.

Bootstrap constructs effective `config.app` from the selected root module's id, label, version, copyright, and icon, plus the bootstrap deployment
basePath, baseUrl, and parameters. The optional `xshell:app.params` meta value becomes the root module's `params` and the same effective
`config.app.params` value. The effective schema requires `app`, `modules`, and `xshell`; that requirement does not permit an authored top-level
`app` object. Authored module and XShell descriptors containing one are rejected.

`xshell:app.configPath` is a bootstrap input, not an effective `app` field. Pack derives it from `release.root` when generating `index.html`.
Pack also generates root `sw.js`; both are stable deployment entry files outside immutable packages and `release.packages`.

See [Module Specification](module.md), [Bootstrap](../architecture/10-bootstrap.md), and [Packaging](../architecture/50-packaging.md).
