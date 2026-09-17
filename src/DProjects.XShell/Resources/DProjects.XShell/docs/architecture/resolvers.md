# Resolvers

Resolvers convert logical resource names into concrete resource URLs.

For example:

```text
icon:x-icon1
```

can resolve to something like:

```text
http://localhost:5000/modules/x/icons/icon1.svg
```

## Resolver rules

Resolver rules are built from the XShell configuration.

They use configuration keys such as:

```text
resolver.icon:x-{name}
resolver.component:x-{name}
resolver.page:/_cdn/x/{path}.js
resolver.module:/_cdn/x/{path}.js
```

Each rule maps a logical resource pattern to a concrete resource path.

For example:

```text
resolver.icon:x-{name}
    =
/_cdn/x/icons/{name}.svg; loader=icon-svg;
```

With that rule:

```text
icon:x-icon1
```

matches:

```text
x-{name}
```

where:

```text
name = icon1
```

and resolves to:

```text
/_cdn/x/icons/icon1.svg
```

## Result

A resolver returns the resolved URL together with metadata from the rule, such as the loader to use.

Conceptually:

```text
logical resource
    ↓
resolver rules from config
    ↓
match pattern
    ↓
replace placeholders
    ↓
resolved URL + loader metadata
```

For example:

```text
icon:x-icon1
    ↓
resolver.icon:x-{name}
    ↓
/_cdn/x/icons/icon1.svg
    +
loader=icon-svg
```

The Resolver only determines where the resource is and how it should be loaded.

The actual loading is handled by the [Loader](loaders.md).

## Related documentation

* [Configuration](configuration.md)
* [Loaders](loaders.md)
* [Modules](modules.md)
