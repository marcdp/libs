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
            "label": "Test module",
            "version": "1.0.0",
            "copyright": "",
            "icon": "",
            "configUrl": "https://example.test/modules/test/module.jsonc",
            "assetsUrl": "https://example.test/modules/test/",
            "controller": "/_assets/test/js/module.js",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            },
            "menus": { "navigation": [{ "label": "Home", "path": "/", "href": "/pages/home.js", "default": true }] },
            "imports": [
                { "configUrl": "https://example.test/modules/x/module.jsonc", "params": { "mode": "compact" } }
            ]
        }
    },
    "xshell": {
        "build": "2026-09-21",
        "debug": false,
        "version": "0.9.0",
        "environment": "Development",
        "assetsPrefix": "_assets",
        "configUrl": "https://example.test/_resources/DProjects.XShell/xshell/xshell.jsonc",
        "assetsUrl": "https://example.test/_resources/DProjects.XShell/xshell/",
        "defaults": {
            "layout": {
                "default": "x-layout-default",
                "dialog": "x-layout-dialog",
                "main": "x-layout-main",
                "stack": "x-layout-stack",
                "embed": "x-layout-embed"
            },
            "dialog": {
                "confirm": "/_assets/x/pages/dialog-confirm.js",
                "message": "/_assets/x/pages/dialog-message.js",
                "prompt": "/_assets/x/pages/dialog-prompt.js",
                "picker": "/_assets/x/pages/dialog-picker.js"
            },
            "component": {
                "lazy": "x-lazy",
                "error": "x-error"
            }
        },
        "navigation": { "mode": "hash" },
        "identity": { "provider": "anonymous" },
        "resolver": {},
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
fragment, not a separate application format. Bootstrap records each resolved definition's `configUrl`, `assetsUrl`, and params on its
`modules.<module-id>` entry. The module id is the key; a duplicate `name` field is unnecessary.

`modules.<id>.menus.<name>` holds a reusable, area-independent named menu contribution. Its value is either a static array of menu items or a
string naming a dynamic menu source registered at runtime through `Areas.registerSource()`. The string is a source name, not a URL; resolver
entries and their `url` values are unrelated. The effective configuration remains deeply frozen: a registered source provides runtime menu data
rather than mutating configuration. A menu item's `path` is optional and provides its friendly/public navigation alias; `href` is its canonical
XShell navigation target. Both can be supplied, and Areas prefixes both local values for each effective Area menu. `xshell.areas.default` selects
a default Area and
`xshell.areas.definitions.<id>` describes application composition, including `prefix` and the participating `modules` array. Areas derives
`home` from the first depth-first navigation item marked `default: true`; it does not use a configured Area home.
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

**Module defaults** are `modules.<id>.defaults`. Each resolved module requires this object, its `page` and `component` objects, and non-empty
`renderEngine` and `stateEngine` strings in both objects. They define how definition-based resources owned by that module execute:

```text
Page render engine: definition.meta.renderEngine → modules.<moduleId>.defaults.page.renderEngine
Page state engine:  definition.meta.stateEngine  → modules.<moduleId>.defaults.page.stateEngine

Component render engine: definition.meta.renderEngine → modules.<moduleId>.defaults.component.renderEngine
Component state engine:  definition.meta.stateEngine  → modules.<moduleId>.defaults.component.stateEngine
```

There is no XShell-global render-engine or state-engine fallback. A module may provide layout resources or Page resources used as dialogs, but
module defaults do not configure application-wide layouts or standard dialog infrastructure.

**XShell defaults** are `xshell.defaults`: global runtime/UI infrastructure and presentation defaults, rather than module execution policy. The
schema requires its `layout`, `component`, and `dialog` groups. Layout lookup uses `xshell.defaults.layout.<context>`; the built-in layout contexts
are `default`, `dialog`, `main`, `stack`, and `embed`, and the schema allows additional named layout contexts. `xshell.defaults.component.lazy`
and `.error` identify the global lazy-loading and error components.

`xshell.defaults.layout.dialog` is the layout context used for a Page opened in dialog context. It is distinct from
`xshell.defaults.dialog.confirm`, `.message`, `.prompt`, and `.picker`, which identify the standard dialog Page resources used by dialog
operations. There is no `module.defaults.dialog` group.

## URLs and immutability

Bootstrap resolves `url:` references against each JSONC source and maps module-relative paths into the configured asset namespace, currently
`/_assets/<module-id>/...`. `configUrl` is the URL of a module configuration document; `assetsUrl` is the physical directory or package containing
that module's assets. Bootstrap defaults `assetsUrl` to the configuration directory and uses it as the Service Worker mapping destination. Normal
resource consumers continue to use the virtual namespace and do not depend on the physical representation. Resolver entries retain their distinct
`url` field: the resolver selects that concrete resource URL and a loader. Area prefixes are preserved as navigation metadata during normalization.

Bootstrap deeply freezes the effective configuration before passing it to XShell. Configuration is distinct from mutable runtime state. JSON
Schema validation is split across phases: bootstrap does not validate, while the X module controller validates the effective configuration during
its `start()` method and throws on failure. This is not environment-gated.

The canonical checked-in schema is `xshell/schemas/config.schema.json`. It describes the final merged configuration and includes `app`, `modules`,
`xshell`, module defaults, menus, and module contracts. Effective module entries require `label`, `version`, `copyright`, `icon`, `configUrl`, and
`assetsUrl`; bootstrap supplies the two URLs during normalization, so authored descriptors need not repeat them. The schema file's `$id` still uses
the stale `https://xshell.dev/schemes/config.scheme.json` identifier even though that is not its repository path.

See [Module Specification](../specifications/module.md), [Resolvers](resolvers.md), and [ADR-0002](../adr/0002-jsonc-specifications.md).
