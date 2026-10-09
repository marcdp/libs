# Configuration

XShell authors JSONC fragments and produces one normalized **effective configuration** with `app`, `modules`, and `xshell` sections.

## Path and URL terminology

In XShell documentation and normalized/effective runtime configuration, a **Path** is an application-root-relative value. It starts with `/`, has no
scheme or origin, and is relative to the XShell application URL namespace. For example:

```text
/my-app-prefix
/_assets/x/page.js
/_assets/codemirror/components/editor.js
```

A **URL** is a fully qualified absolute browser URL. It contains a scheme and, where applicable, an origin, so it can be passed directly to browser
URL, fetch, or import APIs. For example:

```text
https://server/my-app-prefix/
https://server/my-app-prefix/_assets/x/page.js
https://cdn.example.com/codemirror/
```

This establishes the naming convention for normalized/effective/runtime values: `...Path` denotes an application-root-relative Path and `...Url`
denotes a fully qualified absolute URL. A Path is a logical application resource location; it is not interchangeable with the browser-facing physical
URL for that resource. Convert a Path when a browser or native-link API requires an absolute URL, for example:

```js
new URL("_assets/x/page.js", config.app.baseUrl)
```

The application base preserves that distinction:

```text
app.basePath = "/prefix"
app.baseUrl  = "https://server/prefix/"
```

`app.basePath` is a Path prefix and does not require a trailing slash. `app.baseUrl` is an absolute base-directory URL and intentionally ends in `/`
so ordinary URL resolution works:

```js
new URL("file.js", config.app.baseUrl)
```

Authored configuration is a separate input layer. It may contain configuration coordinates / URL expressions such as `app:/foo`, `url:./foo`,
`/foo`, `./foo`, and `../foo`; these are not necessarily final runtime URLs. Bootstrap normalizes them before runtime:

```text
authored configuration expression
    ↓ bootstrap/config normalization
effective Path or absolute URL
    ↓ runtime
```

For example, an authored `"assetsUrl": "url:./"` becomes an absolute effective `assetsUrl`, while authored
`"assetsBasePath": "app:/_assets"` becomes the effective Path `/_assets`.

## Authored module configuration

Every module configuration has exactly one local definition and zero or more external references:

```jsonc
{
    "modules": {
        "app": {
            "label": "Application",
            "version": "1.0.0",
            "copyright": "",
            "icon": "",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            }
        },
        "x": {
            "configUrl": "url:../x/module.jsonc",
            "params": { "mode": "compact" }
        }
    }
}
```

`app` is local because it has no `configUrl`. `x` is a reference. Both root and child references may contribute `params` or other configuration
to the canonical effective module.

## Effective configuration

After discovery and normalization, each reference is replaced through merging by its complete canonical definition.
This excerpt shows location/params fields only, omitting required metadata and defaults:

```jsonc
{
    "modules": {
        "app": {
            "label": "Application",
            "configUrl": "https://example.test/app/module.jsonc",
            "assetsUrl": "https://example.test/app/",
            "assetsPath": "/_assets/app"
        },
        "x": {
            "label": "X",
            "version": "1.0.0",
            "configUrl": "https://example.test//x/module.jsonc",
            "assetsUrl": "https://example.test/x/",
            "assetsPath": "/_assets/x",
            "params": { "mode": "compact" }
        }
    }
}
```

The module key is the identity; duplicate `name`, `id`, or `moduleId` fields are unnecessary. `configUrl` locates `module.jsonc`, while `assetsUrl`
locates the physical resources. Bootstrap generates `assetsPath` as the application-root-relative virtual package base in the Service Worker
namespace. The local definition owns `assetsUrl`; references cannot override it. `assetsPath` is effective runtime metadata and is not authored in
`module.json`, `module.jsonc`, or `xshell.jsonc`.

## Merge and precedence

| Values | Result |
| --- | --- |
| Plain object + plain object | Recursively merge properties. |
| Array + array | Concatenate in merge order. |
| Scalar or other value | Later value replaces earlier value. |

Merge order is deterministic:

```text
XShell defaults
    -> dependency module definitions
    -> their dependents
    -> root application configuration
```

Bootstrap derives that order from the dependency graph, retains sibling declaration/discovery order, and rejects cycles. Reference contributions
merge after the referenced local definition; the root merges last and therefore has final authority. Fetch completion order has no effect.

## Defaults, URLs, and validation

Every effective module requires generated `files`/`assetsPath`, source locations, descriptive metadata, and `defaults.page` and `defaults.component`,
each with non-empty `renderEngine` and `stateEngine`. These module defaults
are separate from global `xshell.ui` settings.

In authored configuration URL expressions, `/foo` starts at the module root, while `./foo` and `../foo` use the declaring file's directory in logical
module coordinates. Paths that traverse above the module root are invalid. `app:` and `url:` are configuration-only; runtime CSS and template resource
references reject both. In configuration, `url:` uses the physical declaring document URL. `app:` uses the XShell application base URL
built once from `document.location.origin` and the host's `xshell:app.basePath` meta value. For example, with a base of
`https://example.com/myapp/`, both `app:images/logo.svg` and `app:/images/logo.svg` resolve to
`https://example.com/myapp/images/logo.svg`. Absolute schemes, including `https:`, `data:`, and `blob:`, remain unchanged.

`xshell.assetsBasePath` defines the application-relative base of XShell's virtual asset namespace. The checked-in value is
`"assetsBasePath": "app:/_assets"`. For an application base of `https://example.com/myapp/`, this resolves under
`https://example.com/myapp/_assets`. The effective configuration stores the normalized application-relative path `/_assets`; the framework and
module `assetsPath` values are derived from that one base. For example, module `x` has `assetsPath: "/_assets/x"`, which the host serves under
`/myapp/_assets/x`. `assetsUrl` remains the separate physical source location.

Bootstrap gives each local definition its document URL as `configUrl` and defaults `assetsUrl` to the document directory. It maps module-relative
runtime paths into the normalized virtual namespace.

The canonical schema describes the final merged object, not partial authored references. Bootstrap generates resolver rules after Service Worker
initialization and inventory loading, including exact contract rules from inventory.
It imports XShell, awaits `xshell.validateConfig(config)` for the complete enriched object, and then deeply freezes it.
`xshell.init(config)` validates again before constructing runtime services. Validation errors abort startup; there is no environment gate.
The X module currently has no module controller; configuration validation occurs during bootstrap and initialization.
`xshell.ui.component.markdown` selects the Component used by `page-md` for Markdown Pages; the current X module sets it to `x-markdown`.
The required `xshell.i18n` section supplies the current language, available languages, date/time formats, and translation dictionaries. The X module
provides the baseline values, and application composition may override them through the normal merge precedence.
An effective module may optionally declare `routes`, an object mapping friendly application Path patterns to module-relative Page targets. During
bootstrap, route target values are normalized into canonical module resource Paths in the owning module's `/_assets/<module-id>/...` namespace;
route keys remain Area-relative application Path patterns unchanged. Routes are distinct from module `menus`: menus describe user-visible navigation
declarations, while routes are application
Path-to-Page declarations. Runtime modules expose the normalized object as `module.routes`; Areas compose ordered `area.routes` from participating
modules without adding their prefixes, and Navigation performs forward and reverse route resolution. See [Modules](30-modules.md),
[Areas](../subsystems/10-areas.md), and [Navigation](120-navigation.md).
Bootstrap deeply freezes the result before `xshell.init(config)`.

See [Modules](30-modules.md), [Bootstrap](10-bootstrap.md), and [Specifications](../specifications/).
