# Module Configuration Specification

A module configuration is a JSONC document whose `modules` object contains exactly one local definition and zero or more external references.

## Local definition

The local definition is the one entry without `configUrl`. Its object key is the canonical module id.

```jsonc
{
    "modules": {
        "orders": {
            "label": "Orders",
            "version": "1.0.0",
            "copyright": "",
            "icon": "",
            "styles": ["/css/styles.css"],
            "controller": "/js/module.js",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            },
            "menus": {},
            "contract": { "events": {}, "actions": {}, "intents": {} }
        }
    }
}
```

Do not duplicate identity in `name`, `id`, or `moduleId`. Property order does not determine ownership. A document with no local definition or more
than one local definition is invalid.

## External reference

Every non-local entry must declare `configUrl`:

```jsonc
{
    "modules": {
        "orders": { "label": "Orders", "version": "1.0.0" },
        "x": { "configUrl": "url:../x/module.jsonc" },
        "customers": { "configUrl": "url:../customers/module.jsonc" }
    }
}
```

The key is the expected identity. `modules.x` must resolve to a document whose local definition is `modules.x`. Bootstrap rejects identity mismatch,
conflicting URLs for the same id, wrong ids for an already known URL, and cycles.

A reusable non-root module may declare dependencies but must not supply dependency `params`. Only the root application can compose those values.
No reference may override `assetsUrl`; the referenced local definition owns its physical resource location.

## Normalized effective module

Bootstrap resolves references recursively and the final effective entry is a complete definition:

```jsonc
"x": {
    "label": "X",
    "version": "1.0.0",
    "copyright": "",
    "icon": "",
    "configUrl": "https://example.test/modules/x/module.jsonc",
    "assetsUrl": "https://example.test/modules/x/",
    "params": { "mode": "compact" },
    "defaults": {
        "page": { "renderEngine": "x", "stateEngine": "proxy" },
        "component": { "renderEngine": "x", "stateEngine": "proxy" }
    }
}
```

`configUrl` is the source document. `assetsUrl` is the physical resource container and defaults to that document's directory. Runtime resource
references use `/_assets/<module-id>/...` rather than the physical URL.

The effective schema requires `label`, `version`, `copyright`, `icon`, `configUrl`, `assetsUrl`, and `defaults`. Optional effective fields include
`params`, `styles`, `controller`, `menus`, and `contract`. `contract` may declare events, actions, and intents; those declarations are metadata and
do not by themselves implement runtime dispatch.

See [Root Module](application.md), [Modules](../architecture/modules.md), and [Configuration](../architecture/configuration.md).
