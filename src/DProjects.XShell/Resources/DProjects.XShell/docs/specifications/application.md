# Root Module and Application Metadata

The application **is** the root module. The host's `xshell.app_config_url` points to its `module.jsonc`; recursive imports compose the application.
This page describes only the root-specific `app` section, not a second application specification.

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
            "imports": {
                "x": { "url": "url:../x/module.jsonc", "params": { "var1": 1111 } }
            }
        }
    },
    "xshell": { "navigation": { "mode": "hash" } }
}
```

`app` contains root/application metadata. `modules.test` is the root's canonical definition; `imports.x` names a desired live instance and its params.
`xshell` contributes framework-wide settings. The checked-in `modules/test/module.jsonc` follows this nested shape. Required fields and validation
rules are not finalized.

**Migration status:** the server command still defaults to a legacy `samples/sample1/app.jsonc` file with dotted keys. Runtime services also read
dotted keys, so this example describes the authored target shape rather than a fully working end-to-end host configuration.

See [Module Specification](module.md), [Bootstrap](../architecture/bootstrap.md), and [Areas](../subsystems/areas.md).
