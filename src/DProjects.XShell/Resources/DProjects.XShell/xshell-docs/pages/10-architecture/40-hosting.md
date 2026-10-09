# ASP.NET Hosting

`services.AddXShell()` is currently an empty extension hook. Browser services are initialized by XShell.
`app.UseXShell(configuration)` installs resource/Temp middleware, generated bootstrap files, and SPA fallback.

```csharp
using DProjects.XShell;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddXShell();
var app = builder.Build();
app.MapGet("/api/health", () => "ok");
app.UseXShell(new Extensions.Configuration {
    App = new Extensions.AppConfig {
        Description = "",
        BasePath = "",
        ConfigPath = "/_resources/DProjects.XShell/x-demo/module.jsonc",
        Params = new Dictionary<string, string>()
    },
    Resources = new Extensions.ResourcesConfig {
        BasePath = ""
    },
    XShell = new Extensions.XShellConfig {
        BasePath = "/_resources/DProjects.XShell/xshell"
    },
    Server = new Extensions.ServerConfig {
        Environment = null,
        HeaderCSP = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; ",
        UnhandledPrefixes = new[] { "/_", "/api", "/temp" }
    },
    Temp = new Extensions.TempConfig {
        Path = Path.Combine(Path.GetTempPath(), Extensions.ResourceName, "temp"),
        BasePath = "/temp",
        ExpirationTime = TimeSpan.FromHours(1),
        FileSizeLimit = 100 * 1024 * 1024
    }
});
app.Run();
```

## Configuration

| Setting | Default and effect |
| --- | --- |
| `App.Description` | `""`; HTML description meta value. |
| `App.BasePath` | `""`; valid root hosting. A nonempty value must be a rooted request path such as `/app`. |
| `App.ConfigPath` | `""` is a placeholder rejected by `UseXShell()`; supply a nonempty root module configuration path or URL. |
| `App.Params` | Empty string dictionary; host params become root-module/application params. |
| `Resources.BasePath` | `""`; prefixes resource middleware route `/_resources/DProjects.XShell`. |
| `XShell.BasePath` | `""` is a placeholder rejected by `UseXShell()`; supply a rooted bootstrap and worker source base. |
| `Server.Environment` | `null`; use ASP.NET's environment name. An explicit value overrides XShell's own environment and development resource behavior. |
| `Server.HeaderCSP` | Same-origin default policy, data images allowed, objects disabled; HTML CSP meta value. |
| `Server.UnhandledPrefixes` | `["/_", "/api", "/temp"]`; unmatched relative paths with these prefixes bypass SPA fallback. |
| `Temp.Path` | `""` is a placeholder rejected by `UseXShell()`; supply a nonempty physical temporary-file storage root. |
| `Temp.BasePath` | `/temp`; independent public Temp prefix. |
| `Temp.ExpirationTime` | One hour. |
| `Temp.FileSizeLimit` | `104857600` bytes (100 MiB); maximum accepted upload size. |

The default CSP text is:

```text
default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self';
```

`UseXShell()` rejects invalid static host settings before resource lookup, middleware registration, or bootstrap generation. It requires positive Temp limits,
rooted request paths for `Temp.BasePath`, `XShell.BasePath`, and each `Server.UnhandledPrefixes` entry. Nonempty `App.BasePath` and
`Resources.BasePath` values must also be rooted; empty values remain valid. Temp directory creation remains in `TempMiddleware`.
`Resources.BasePath`, `XShell.BasePath`, and `Temp.BasePath` are independent, not automatically rebased by `App.BasePath`.
Set browser runtime paths consistently when changing resource routing.
The host URL-encodes each `App.Params` key and value, joins them as query text, and HTML-encodes the resulting meta attribute value.
Generated host HTML attribute values are HTML encoded. This serialization safety does not replace semantic host configuration validation.

## Request pipeline

1. Resource middleware serves the physical resource tree.
2. Temp middleware handles its prefix.
3. The application base redirects to `App.BasePath + "/"`. Paths outside a nonempty application base redirect too unless under the resource base.
4. `UseRouting()` selects registered endpoints.
5. Generated `index.html`/`sw.js` at the application base are returned first; other already-matched endpoints continue.
6. Remaining requests inside the base receive host HTML unless their relative path starts with an unhandled prefix.

Unhandled matching is case-insensitive textual prefix matching: `/apiary` also matches `/api`.
SPA fallback has no method filter. Path navigation needs this fallback for friendly-URL reloads.

`BootstrapFilesBuilder` emits HTML, CSP meta, `xshell:` startup meta, and the bootstrap script.
Generated `sw.js` imports the configured worker source. Bootstrap registers it with the application base-path scope
`App.BasePath + "/"` (root scope only when `App.BasePath` is empty).

## Resources and development

XShell uses the effective `Configuration.Environment` when supplied, otherwise ASP.NET's environment name. A case-insensitive `Development`
value enables XShell development resources; an explicit `Production` disables them even if the ASP.NET host is Development. This does not modify
ASP.NET's own environment object.
Development uses assembly ProjectDirectory metadata to locate `Resources/DProjects.XShell`; other environments use the assembly output directory.
The resource directory must exist.

Development middleware disables caching, generates inventories, and compiles normal-module JS/HTML/CSS on demand.
Logical `.js` requests may compile an HTML SFC; direct source `.html` requests return 404. Same-basename HTML/JS conflicts fail compilation.
Development inventories contain physical source paths, possibly authored `.html`; packed inventories contain generated `.js`.
Debugger presence does not select this behavior.

## Server command

The bundled `server` command hosts resources and the demo. Options include `-a` (application base), `-c` (root config), `-r` (resource base),
`-e` / `--environment` (XShell environment), and repeatable `-p` (params). Its default application is x-demo; it supplies no application
authentication pipeline. Select `Development` for on-demand compilation.

See [Bootstrap](10-bootstrap.md), [Navigation](120-navigation.md), [Packaging](50-packaging.md), [Service Worker](110-service-worker.md), and [Temp](../subsystems/50-temp.md).
