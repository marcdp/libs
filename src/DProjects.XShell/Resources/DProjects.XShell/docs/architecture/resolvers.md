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
`/_assets`. Rules for a shared module definition should be generated once, regardless of the number of live imports.

**Current gap:** `resolver.js` reads dotted `resolver.*` keys and parses encoded semicolon strings, while bootstrap now generates nested rule objects.
Structured rule dispatch is not yet implemented in that service.

See [Configuration](configuration.md) and [Loaders](loaders.md).
