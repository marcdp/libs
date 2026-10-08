# Resolvers

A Resolver maps a logical resource such as `icon:x-file` to a concrete resource location and loader metadata. The Loader then obtains the resource.
They are separate responsibilities. Generated resolver entries currently use application-root-relative virtual Paths such as
`/_assets/x/icons/x-file.svg`. See [Configuration](20-configuration.md#path-and-url-terminology).

A custom rule may use this nested shape:

```jsonc
{
    "xshell": {
        "resolver": {
            "icon": {
                "x-{name}": { "src": "/_assets/x/icons/x-{name}.svg", "loader": "icon-svg" }
            }
        }
    }
}
```

Bootstrap generates conventional rules for module icons, layouts, components, pages, and JavaScript modules. The checked-in default prefix is
`/_assets`. Rules for a canonical module definition are generated once, regardless of repeated dependency references.

`resolver.js` reads nested `config.xshell.resolver` rule objects. Bootstrap adds defaults for each canonical module id after merging. Resolver
matching and loader dispatch still require the usual resource location and loader metadata.

Resolver rule `src` may be an application Path or an absolute URL. `Resolver.resolve()` returns `path` as the resolved application Path (or `null`
for an absolute URL source) and `url` as the resolved absolute URL.

Resolver entries may set `cache: true` and optionally select `cacheMode: "full"` or `cacheMode: "path"`. The default `full` mode includes the query
in Loader cache identity. The `path` mode excludes the query from cache identity without changing resolution or the URL passed to the
resource-specific loader. Bootstrap uses `path` for its generated Page rules; unrelated generated rules retain the default `full` behavior.

## Declarative component and Page dependencies

Values in a Component or Page implementation's `dependencies` object are ordinary logical resource references, such as
`module:/_assets/x/utils/markdown.js`, `icon:x-file`, or `component:x-code-editor`. The Resolver determines which resource definition matches a
reference and maps it to its URL and loader metadata. The Loader then obtains that resource and exposes the value under the declaration's key in
`controller({ dependencies })`.

`dependencies` is therefore a declarative way for a definition-based Component or Page to request known resources through the existing Resolver
and Loader infrastructure; it is not a separate resolver rule set or dependency-injection system.

See [Configuration](20-configuration.md) and [Loaders](90-loaders.md).

Bootstrap also generates exact `contract:<id>` rules from module inventory entries under `/contracts/`.
These use `object-json` and preserve module ownership metadata; see [Services](100-services.md).
Resolver generation happens after inventory loading, before effective schema validation.
