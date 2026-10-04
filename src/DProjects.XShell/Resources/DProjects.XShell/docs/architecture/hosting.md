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
    AppConfigPath = "/_resources/DProjects.XShell/x-demo/module.jsonc"
});
app.Run();
```

## Configuration

| Setting | Default and effect |
| --- | --- |
| `AppBasePath` | `""`; application prefix with a trailing-slash entry URL. |
| `AppConfigPath` | `""`; set the root module configuration URL. |
| `AppParams` | Empty string dictionary; host params become root-module/application params. |
| `ResourcesBase` | `""`; prefixes resource middleware route `/_resources/DProjects.XShell`. |
| `XShellBasePath` | `/_resources/DProjects.XShell/xshell`; bootstrap and worker source base. |
| `UnhandledPrefixes` | `["/_", "/api", "/temp"]`; unmatched relative paths with these prefixes bypass SPA fallback. |
| `TempPath` | System temporary directory / `DProjects.XShell` / `temp`. |
| `TempUrl` | `/temp`; independent public Temp prefix. |
| `TempExpirationTime` | One hour. |
| `CSPValue` | Same-origin default policy, data images allowed, objects disabled; HTML CSP meta value. |

The default CSP text is:

```text
default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self';
```

`ResourcesBase`, `XShellBasePath`, and `TempUrl` are independent, not automatically rebased by `AppBasePath`.
Set browser runtime paths consistently when changing resource routing.
Params are serialized as joined query text; values requiring query escaping must be encoded appropriately.
Generated HTML interpolates host settings, so use trusted configuration.

## Request pipeline

1. Resource middleware serves the physical resource tree.
2. Temp middleware handles its prefix.
3. The application base redirects to `AppBasePath + "/"`. Paths outside a nonempty application base redirect too unless under the resource base.
4. `UseRouting()` selects registered endpoints.
5. Generated `index.html`/`sw.js` at the application base are returned first; other already-matched endpoints continue.
6. Remaining requests inside the base receive host HTML unless their relative path starts with an unhandled prefix.

Unhandled matching is case-insensitive textual prefix matching: `/apiary` also matches `/api`.
SPA fallback has no method filter. Path navigation needs this fallback for friendly-URL reloads.

`BoostrapFilesBuilder` emits HTML, CSP meta, `xshell:` startup meta, and the bootstrap script.
Generated `sw.js` imports the configured worker source. Bootstrap registers it with root scope `/`; a host serving it below the origin root must
permit that scope.

## Resources and development

Development behavior requires ASP.NET `IsDevelopment()`.
Development uses assembly ProjectDirectory metadata to locate `Resources/DProjects.XShell`; other environments use the assembly output directory.
The resource directory must exist.

Development middleware disables caching, generates inventories, and compiles normal-module JS/HTML/CSS on demand.
Logical `.js` requests may compile an HTML SFC; direct source `.html` requests return 404. Same-basename HTML/JS conflicts fail compilation.
Development inventories contain physical source paths, possibly authored `.html`; packed inventories contain generated `.js`.
Debugger presence does not select this behavior.

## Server command

The bundled `server` command hosts resources and the demo. Options include `-a` (application base), `-c` (root config), `-r` (resource base),
and repeatable `-p` (params). Its default application is x-demo; it supplies no application authentication pipeline.
Use ASP.NET Development for on-demand compilation.

See [Bootstrap](bootstrap.md), [Navigation](navigation.md), [Packaging](packaging.md), [Service Worker](service-worker.md), and [Temp](../subsystems/temp.md).
