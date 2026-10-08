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
            "requires": ["toast"],
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
        "orders": {
            "label": "Orders", "version": "1.0.0", "copyright": "", "icon": "",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            }
        },
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
    "configUrl": "https://example.test/x/module.jsonc",
    "assetsUrl": "https://example.test/x/",
    "assetsPath": "/_assets/x",
    "files": [],
    "params": { "mode": "compact" },
    "defaults": {
        "page": { "renderEngine": "x", "stateEngine": "proxy" },
        "component": { "renderEngine": "x", "stateEngine": "proxy" }
    }
}
```

`configUrl` is the source document. `assetsUrl` is the physical resource container and defaults to that document's directory. Runtime resource
references use `/_assets/<module-id>/...` rather than the physical URL.
In configuration URL fields, `/foo` starts at the module root; `./foo` and `../foo` use the declaring file's logical module directory and
cannot escape the module root. `app:` and `url:` are available only during configuration/bootstrap. `url:` uses the physical declaring document URL,
including for `configUrl` and `assetsUrl`; `app:` resolves against the XShell application base, including `app:foo` and `app:/foo` under a nested
path. Runtime CSS and template resource references reject both pseudo-schemes. They use logical relative paths and module-root `/foo` when
module-owned. Custom resources without a module path use normal URL resolution against their declaring URL for relative and origin-root paths.
Standard absolute schemes and protocol-relative URLs remain unchanged.

The effective schema requires `label`, `version`, `copyright`, `icon`, `configUrl`, `assetsUrl`, `defaults`, `assetsPath`, and `files`.
Optional effective fields are `params`, `requires`, `menus`, `routes`, and `contract`. `contract` may declare events, actions, and intents; those
declarations remain descriptive metadata and do not by themselves implement runtime dispatch.

## Required services

`requires` is an optional array of unique, non-empty service names that the module expects the runtime to provide:

```jsonc
"requires": ["toast"]
```

XShell validates these names after the service registry has been finalized and before any module controller is loaded or started. The validation is
an existence check and does not instantiate configured lazy services. `requires` is not a module reference, a Component or Page implementation's
resource `dependencies` object, or controller service access. Controllers continue to receive existing services by name, such as
`controller({ toast })`.

## Routes

`routes` is an optional object whose keys are friendly application URL patterns and whose values are module-relative Page targets:

```jsonc
"routes": {
    "/something": "/pages/index.js",
    "/repository/{repositoryId}/projects/{projectId}/items": "/pages/index.js"
}
```

Routes are application URL-to-Page declarations, distinct from `menus`, which define visible navigation structures and concrete destinations. Route
keys are Area-relative friendly paths. They support literal segments and whole-segment `{parameterName}` placeholders only; optional parameters,
wildcards, catch-alls, typed parameters, custom regular expressions, and route priorities are unsupported.

During bootstrap, each authored module-relative route target is normalized into the owning module's canonical
`/_assets/<module-id>/...` resource namespace. Route keys remain unchanged, and neither Area prefixes nor application base paths are stored in the
declaration. The runtime module exposes the normalized object as `module.routes`. Areas compose `area.routes` from `area.modules`, preserving module
order, route declaration order, and duplicate paths.

Navigation resolves incoming Area-relative public paths against that ordered collection. A forward match selects the canonical Page target and adds
decoded path parameters as encoded query parameters. Intrinsic target query parameters initialize the target query, incoming public query values may
replace non-route target values, and route-path parameters win all same-name conflicts. Fragments remain attached.

For browser-facing generation, Navigation selects the first applicable route targeting a canonical Page, substitutes placeholder values from its
query, and consumes those values from the public query. Intrinsic target query parameters are also consumed after they match; unrelated query values
and fragments remain. A missing placeholder makes only that reverse candidate inapplicable. Exact menu aliases retain precedence in both directions.
Ambiguity is resolved strictly by Area composition order without scoring, specificity ranking, or priorities. No match falls through to existing
Navigation behavior, and canonical Page hrefs remain directly usable without a route.

The normalized declarations are available through `xshell.modules.getModuleById(id).routes`. This is the effective configuration object itself;
modules without routes expose a frozen empty object. Route metadata remains declarative—Areas and Navigation compose and interpret it without
mutating the module definition.

For example, this authored route:

```jsonc
"/repository/{repositoryId}/projects/{projectId}/items": "/pages/items.js"
```

becomes this effective runtime entry for module `x-demo`:

```jsonc
"/repository/{repositoryId}/projects/{projectId}/items": "/_assets/x-demo/pages/items.js"
```

See [Root Module](application.md), [Modules](../architecture/30-modules.md), and [Configuration](../architecture/20-configuration.md).

## Inventories and module styles

Bootstrap supplies `assetsPath` and loads `module.files.json` into `files`, whose entries have `path`, `size`, and `hash`.
Inventories describe resources; do not hand-author them as menu or contract declarations.
The optional module-wide `/styles/index.css` is loaded only when its path occurs in the inventory. `Modules` loads it as a `style:` resource through
the standard Resolver → Loader → `style-css` pipeline. Relative CSS imports and URLs resolve against the stylesheet containing them. There is no
accepted effective module `styles` field.
`params` is a free-form object. `menus` maps arbitrary menu names to static arrays, module-relative inventory paths beginning with `/` such as
`"/pages"`, or registered runtime source names. Inventory paths derive Page menus from generated `files`; see [Areas](../subsystems/10-areas.md).

The effective schema requires complete module defaults, source locations, and generated `files`/`assetsPath` metadata.
Bootstrap enriches the object before schema validation. See [Packaging](../architecture/50-packaging.md).
