# Resolvers

A Resolver maps a logical resource such as `icon:x-bell` to a concrete URL and loader metadata. The Loader then obtains the resource. They are
separate responsibilities.

The intended nested rule shape is:

```jsonc
{
    "xshell": {
        "resolver": {
            "icon": {
                "x-{name}": { "url": "/_assets/x/icons/{name}.svg", "loader": "icon-svg" }
            }
        }
    }
}
```

Bootstrap generates conventional rules for module icons, layouts, components, pages, and JavaScript modules. The checked-in default prefix is
`/_assets`. Rules for a canonical module definition are generated once, regardless of repeated imports.

`resolver.js` reads nested `config.xshell.resolver` rule objects. Bootstrap adds defaults for each canonical module id after merging. Resolver
matching and loader dispatch still require the usual resource URL and loader metadata.

## Declarative component and Page dependencies

Values in a Component or Page implementation's `dependencies` object are ordinary logical resource references, such as
`module:/_assets/x/utils/markdown.js`, `icon:x-file`, or `component:x-code-editor`. The Resolver determines which resource definition matches a
reference and maps it to its URL and loader metadata. The Loader then obtains that resource and exposes the value under the declaration's key in
`controller({ dependencies })`.

`dependencies` is therefore a declarative way for a definition-based Component or Page to request known resources through the existing Resolver
and Loader infrastructure; it is not a separate resolver rule set or dependency-injection system.

See [Configuration](configuration.md) and [Loaders](loaders.md).
