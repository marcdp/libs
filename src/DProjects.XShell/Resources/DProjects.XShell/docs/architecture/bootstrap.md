# Bootstrap

This document describes the verified startup path from the ASP.NET Core host to the first XShell navigation.

## Status

Draft.

## Host page and bootstrap inputs

`Extensions.UseXShell` serves the XShell resources and generates the SPA host HTML. The host page provides three values as metadata:

- `xshell.app_base_url` from `Extensions.Configuration.AppBase`;
- `xshell.app_config_url` from `Extensions.Configuration.AppConfig`;
- `xshell.sw_url`, built from the application base and `/sw.js`.

The page then loads `xshell/bootstrap.js` from the configured resource base. The command-line server uses the checked-in sample `app.jsonc` when no
application configuration path is supplied, but an embedding application can provide another URL through `Extensions.Configuration`.

`bootstrap.js` reads the application configuration URL from the `xshell.app_config_url` meta element and resolves it with
`new URL(..., document.baseURI)`. It uses `document.currentScript.src` to locate the framework configuration beside the bootstrap script.

## Configuration assembly

Bootstrap assembles one flat configuration map in this order:

1. It fetches `xshell.jsonc`, removes comments, parses it, normalizes its URLs, records its URL as `xshell.src`, and copies its framework defaults.
2. It fetches the application JSONC URL, removes comments, parses it, normalizes its URLs, and copies its keys over the framework values. It also sets
   `app.base` from the host metadata.
3. It discovers module names from keys beginning with `modules.`, using the second dotted segment as the module name.
4. It fetches each `modules.<name>.src` concurrently and parses the returned `module.jsonc` document.
5. For each module, it assigns current defaults for `icon`, `label`, `version`, `depends`, and `styles`, normalizes module-relative URLs,
   and merges the result.
6. It adds conventional resolver definitions for that module's icons, layouts, components, pages, and JavaScript modules.

Ordinary module fields become `modules.<name>.<field>`. A `global.*` field has the prefix removed and is written into the shared configuration. A
global resolver contribution also receives the module name and module asset path as resolver metadata. See [Configuration](configuration.md) for precedence
and normalization details.

## Service worker and import map

After configuration assembly, bootstrap registers the application service worker and sends it rewrite rules for the framework and every configured module.
Each rule maps the stable `/<assetsPrefix>/...` namespace to the source directory from which the corresponding JSONC file was loaded. Bootstrap waits for
service-worker readiness and reloads if the current page is not yet controlled.

Once the service worker acknowledges its initialization message, bootstrap builds an import map from the assembled `resolver.import:*` entries. It then
imports the `xshell` mapping and calls `xshell.init(config)`.

## Runtime initialization

`xshell.init` constructs the configuration, resolver, loader, navigation, module, menu, authentication, and other runtime services, then registers them in
the service registry. The `Config` service receives the fully assembled map and freezes that top-level map before other runtime services consume it.

Initialization then proceeds as follows:

1. Authentication resolves the configured identity provider and registers the resulting identity.
2. Module initialization starts each configured stylesheet load and, when `handler` is present, loads the handler through `module:<handler>`.
3. After those tasks complete, every module controller receives `onCommand("load", params)`. The parameters are the values under
   `modules.<name>.params.*`.
4. Navigation initializes. In hash mode it restores the URL stack or navigates to the default area's `home` value when no hash is present.
5. Menus are built from the merged module configuration.

Module handlers therefore run after their handler module and styles have loaded, and before initial navigation is started. A handler can use
already-registered XShell services, as the `x-debugger` handler does when registering a dynamic menu source.

Navigation initialization creates the initial `x-page`, which begins its own asynchronous resolver/loader flow. `xshell.init` does not wait for that
page's `load` event. Its completion means runtime service, module, navigation, and menu initialization has returned, not that the first page has finished
rendering.

## End-to-end flow

```text
ASP.NET Core host
    -> generated host HTML and XShell meta values
    -> bootstrap.js
    -> xshell.jsonc framework defaults
    -> application JSONC overrides
    -> module discovery and parallel module.jsonc loading
    -> URL normalization, module/global merge, and generated resolvers
    -> service-worker rules and import map
    -> XShell services and identity
    -> module styles, handlers, and handler load commands
    -> navigation initialization and initial x-page load starts
    -> menu initialization
```

## Development and production hosting

In development, or while a debugger is attached, the ASP.NET host serves resources directly from the project's `Resources/DProjects.XShell` directory and
adds no-cache response headers. In production it serves the copied resources beside the built assembly. The browser bootstrap sequence itself is the same
in both modes.

## TODO

TODO: Define a public readiness signal, if one is intended, and specify bootstrap failure/recovery behavior. The current implementation exposes neither a
ready event nor a documented retry contract beyond the service-worker control reload.

## Related documentation

- [Architecture](index.md)
- [Configuration](configuration.md)
- [Modules](modules.md)
- [Service Worker](service-worker.md)
