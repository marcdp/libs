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

See [Configuration](configuration.md) and [Loaders](loaders.md).
