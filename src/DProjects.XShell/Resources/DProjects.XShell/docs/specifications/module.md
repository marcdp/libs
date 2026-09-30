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
            "routes": {
                "/something": "/pages/index.js",
                "/repository/{repositoryId}/projects/{projectId}/items": "/pages/index.js"
            },
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

Any reference may contribute `params`, definition fields, or other configuration to the canonical effective module. `configUrl` remains required
and identifies the referenced definition document. No reference may override `assetsUrl`; the referenced local definition owns its physical
resource location.

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
`params`, `styles`, `controller`, `menus`, `routes`, and `contract`. `contract` may declare events, actions, and intents; those declarations are
metadata and do not by themselves implement runtime dispatch.

## Routes

`routes` is an optional object whose keys are friendly application URL patterns and whose values are module-relative Page targets:

```jsonc
"routes": {
    "/something": "/pages/index.js",
    "/repository/{repositoryId}/projects/{projectId}/items": "/pages/index.js"
}
```

Routes are application URL-to-Page declarations, distinct from `menus`, which are user-visible navigation declarations. Route placeholders such as
`{repositoryId}` are declarative only at this stage. During bootstrap, route target values are normalized into the owning module's canonical
`/_assets/<module-id>/...` resource namespace. Route keys are preserved unchanged. Area prefixes and application base paths are not applied, and
route values are not otherwise rewritten.

Areas compose routes from participating modules, and Navigation resolves incoming Area-relative friendly paths against that ordered collection. A
match selects the canonical Page target and adds decoded path parameters as encoded query parameters. Exact menu-path aliases retain precedence.
For browser-facing generation, Navigation can select the first applicable route targeting a canonical Page, substitute placeholder values from its
query, and preserve unused query parameters. Missing placeholder values make a reverse candidate inapplicable. Exact menu aliases retain precedence.

After bootstrap, the runtime module instance exposes the normalized declarations through `xshell.modules.getModuleById(id).routes`. This is the
effective configuration object itself; modules without routes expose a frozen empty object. Route metadata remains declarative—Areas and Navigation
compose and interpret it without mutating the module definition.

For example, this authored route:

```jsonc
"/repository/{repositoryId}/projects/{projectId}/items": "/pages/items.js"
```

becomes this effective runtime entry for module `x-demo`:

```jsonc
"/repository/{repositoryId}/projects/{projectId}/items": "/_assets/x-demo/pages/items.js"
```

See [Root Module](application.md), [Modules](../architecture/modules.md), and [Configuration](../architecture/configuration.md).
