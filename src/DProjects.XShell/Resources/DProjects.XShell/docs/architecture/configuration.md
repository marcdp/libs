# Configuration

XShell authors nested JSONC. Framework defaults, the root module, and imported module definitions contribute to one **effective configuration**:

```jsonc
{
    "app": {
        "name": "test-app",
        "label": "Test app",
        "copyright": "",
        "icon": "",
        "version": "0.1.0",
        "basePath": "https://example.test/app",
        "params": { "mode": "compact" }
    },
    "modules": {
        "test": {
            "controller": "/_assets/test/js/module.js",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" }
            },
            "label": "Test module",
            "menus": { "navigation": [{ "label": "Home", "href": "/pages/home.js", "default": true }] },
            "imports": [
                { "url": "url:../x/module.jsonc", "params": { "mode": "compact" } }
            ]
        }
    },
    "xshell": {
        "environment": "Development",
        "defaults": {
            "page": {
                "layout": {
                    "default": "x-layout-default",
                    "dialog": "x-layout-dialog",
                    "main": "x-layout-main",
                    "stack": "x-layout-stack",
                    "embed": "x-layout-embed"
                },
                "renderEngine": "plain",
                "stateEngine": "plain"
            },
            "dialog": {
                "confirm": "/_assets/x/pages/dialog-confirm.js",
                "message": "/_assets/x/pages/dialog-message.js",
                "prompt": "/_assets/x/pages/dialog-prompt.js",
                "picker": "/_assets/x/pages/dialog-picker.js"
            },
            "component": {
                "lazy": "x-lazy",
                "error": "x-error",
                "renderEngine": "x",
                "stateEngine": "proxy"
            }
        },
        "navigation": { "mode": "hash" },
        "areas": {
            "default": "main",
            "definitions": {
                "main": { "prefix": "", "modules": ["test"] }
            }
        }
    }
}
```

`app` holds application metadata and host-derived values: `name`, `label`, `copyright`, `icon`, `version`, `basePath`, and `params`.
The host/bootstrap supplies an absolute `basePath` and copies root-module params into `app.params`. `modules` holds canonical definitions keyed by
module id. `xshell` holds runtime and hosting configuration, including `environment`, plus shared contributions. This is a root `module.jsonc`
fragment, not a separate application format. Bootstrap records each resolved definition's source `url` and params on its
`modules.<module-id>` entry. The module id is the key;
a duplicate `name` field is unnecessary.

`modules.<id>.menus.<name>` holds reusable, area-independent menu contributions. `xshell.areas.default` selects a default Area and
`xshell.areas.definitions.<id>` describes application composition, including `prefix` and the participating `modules` array. Areas derives
`home` from the first top-level navigation item marked `default: true`; it does not use a configured Area home.
Root ownership of Area composition is an architectural convention; imported fragments can technically contribute `xshell` settings.

## Merge and precedence

| Earlier and later values | Result |
| --- | --- |
| Plain object + plain object | Recursively merge properties |
| Array + array | Concatenate in order |
| Scalar or other value | Later value replaces earlier value |

The intended order is XShell defaults, imported dependencies, their importers, then the root module. The current bootstrap instead uses reverse
registration order, which does not guarantee dependency-first precedence for every graph. See [Bootstrap](bootstrap.md).

Repeated-import params follow a different rule from configuration merging: **the first registered import wins**. Later imports of the same
definition URL neither replace nor merge its params. That URL produces one canonical definition and one live module instance. Registration order
depends on bootstrap's module discovery order.

Because `area.modules` is an array, contributions to the same Area from multiple fragments concatenate under the generic merge rule. There is
no Area-specific replacement rule.

## Defaults

`xshell.defaults.page` supplies Page render/state defaults and maps presentation contexts to layouts. `xshell.defaults.dialog` is a sibling that
identifies the page resources used by `confirm`, `message`, `prompt`, and `picker`; it is not part of the Page defaults. In particular,
`defaults.page.layout.dialog` selects a layout, while `defaults.dialog.confirm` selects the Page resource used for a confirmation operation.
`xshell.defaults.component` supplies Component render/state defaults plus the lazy and error component names.

The module schema also permits `modules.<id>.defaults`. Component loading applies render/state engines in this order: XShell default, module
`defaults.component`, then component `meta`. In the checked-in implementation, Page loading still reads the legacy `modules.<id>.page` object rather
than `modules.<id>.defaults.page`, then applies Page `meta`. The X module already authors `defaults.page`, so this migration is incomplete. Dialog
operations read only `xshell.defaults.dialog`; module `defaults.dialog` is not applied.

## URLs and immutability

Bootstrap resolves `url:` references against each JSONC source and maps module-relative paths into the configured asset namespace, currently
`/_assets/<module-id>/...`. Resolver entries are nested objects; the resolver selects a URL and loader, and the loader obtains the resource.
Area prefixes are preserved as navigation metadata during this normalization.

Bootstrap deeply freezes the effective configuration before passing it to XShell. Configuration is distinct from mutable runtime state. JSON
Schema validation is split across phases: bootstrap does not validate, while the X module controller validates the effective configuration during
its `start()` method and logs failures with `console.error`. This is not environment-gated.

The checked-in `xshell/schemes/config.scheme.json` describes the intended final merged configuration, but it is still transitional. Module
definitions include `defaults`, while legacy top-level module `page` and `component` fields are also accepted. In addition,
`$defs/modulePage` references the missing `#/$defs/dialog`. Treat the schema as an evolving validation aid rather than a finalized authoritative
contract.

See [Module Specification](../specifications/module.md), [Resolvers](resolvers.md), and [ADR-0002](../adr/0002-jsonc-specifications.md).
