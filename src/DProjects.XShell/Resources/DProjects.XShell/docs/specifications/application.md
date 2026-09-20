# Root Module and Application Metadata

The application **is** the root module. The host's historical `xshell.app_config_url` meta value points to its `module.jsonc`. Recursive
imports compose the application; there is no separate application specification.

```jsonc
{
    "app": {
        "name": "test-app",
        "label": "Test app",
        "version": "0.1.0",
        "icon": ""
    },
    "modules": {
        "test": {
            "label": "Test module",
            "imports": [
                { "url": "url:../x/module.jsonc", "params": { "mode": "compact" } }
            ]
        }
    },
    "xshell": { "navigation": { "mode": "hash" } }
}
```

`app` contains application metadata. `modules.test` is the root's canonical definition and `xshell` contributes shared settings. Bootstrap
uses the first key of the root file's `modules` object as `xshell.module.root`. The optional `xshell.app_params` host meta value becomes the
root definition's `params`. The framework default `xshell.module.rootParams` exists but bootstrap does not fill it from that value. Root
status is application composition information, not a `modules.test.root` flag.

The checked-in `modules/test/module.jsonc` follows this nested shape. Required fields and validation rules are not finalized. The server
command still defaults to the legacy `samples/sample1/app.jsonc`, so its default host path does not demonstrate this root-module format.

See [Module Specification](module.md), [Bootstrap](../architecture/bootstrap.md), and [Areas](../subsystems/areas.md).
