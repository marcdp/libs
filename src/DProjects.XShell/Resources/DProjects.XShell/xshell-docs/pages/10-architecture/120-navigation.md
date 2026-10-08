# Navigation

One Navigation subsystem maps browser location to Pages. Hash and path modes are both implemented on the same Page and stack model.

An Area is a navigation context within a mode. Its `xshell.areas.definitions.<id>.prefix` identifies that context, while a module's
`/_assets/<module-id>/...` URL identifies a page resource. Navigation mode, Area, and resource ownership are separate. The hash-mode
`hashPrefix = "#!"` marks the browser fragment; it is not an Area prefix.

```text
Module.routes
    ↓ Bootstrap normalization
module.routes
    ↓ Area composition
area.routes
    ↓
Navigation
  ├─ forward: public route → canonical Page href → x-page
  └─ reverse: canonical Page href → public route → x-anchor
```

## Menus, routes, and canonical targets

Areas also expose an ordered `area.routes` collection composed from their participating modules. These route declarations remain separate from menus:
Area composition preserves each route's application-facing `path`, canonical Page `href`, source `module`, and declaration order without applying the
Area prefix or interpreting placeholders. Navigation compiles and matches these declarations when resolving incoming friendly paths.

Navigation has a private compiler for the intentionally small route syntax: literal path segments and whole-segment named parameters such as
`/repository/{repositoryId}/projects/{projectId}/items`. Optional parameters, wildcards, catch-alls, typed parameters, custom regular expressions,
partial-segment placeholders, duplicate parameter names, and route priorities are unsupported. The compiler records parameter names in declaration
order and creates an internal escaped matcher; the same compiled segment metadata is used for forward matching and reverse path construction.

Navigation also has an internal Area-relative matcher that scans `area.routes` in composition order and returns `{ route, params }` for the first
match, or `null` when none matches. It removes query and fragment suffixes for matching and decodes each captured segment independently, so encoded
slashes remain within one parameter value.

For forward resolution, Navigation first preserves the existing exact menu-path lookup. If no menu alias matches, it resolves the Area, removes that
Area's prefix for matching, and scans `area.routes` in composition order. The first match becomes the route's canonical Page `href`, with the Area
prefix reapplied. Route parameters are encoded as query parameters; target query parameters and incoming public query parameters are preserved, with
incoming values replacing same-name target values unless that name is a route parameter. Route parameters win all same-name conflicts. Incoming
fragments are retained. For example,
`/demo/repository/12/projects/7/items?sort=name#summary` can resolve internally to
`/demo/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7&sort=name#summary`.

This conversion is internal before `x-page.src` is assigned. Browser-facing URLs remain friendly, canonical URLs continue to work directly, and an
unmatched path follows the existing fallback.

For reverse generation, Navigation preserves exact menu aliases first, then inspects the relevant Areas and their routes in deterministic order. A
route applies when its target identity matches, its intrinsic target query parameters match, and every declared placeholder has a canonical query
value. Placeholder values are encoded as individual path segments and consumed from the public query; intrinsic target parameters are also consumed,
while unrelated parameters and the fragment remain. Missing placeholder values make that candidate inapplicable without throwing, so later routes or
the canonical fallback can be used.

For example, canonical
`/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7&sort=name` becomes public
`/demo/repository/12/projects/7/items?sort=name`. Reverse routing remains private to Navigation; `x-anchor` continues to request a browser-facing URL
without understanding route syntax.

Route ambiguity is resolved only by deterministic Area composition order: module declaration order followed by route declaration order within each
module. Forward matching uses the first matching route, and reverse generation uses the first applicable route. Navigation performs no specificity
scoring, literal weighting, parameter-count ranking, sorting, or priority lookup. Exact duplicate paths contributed by different modules remain in
the ordered collection and therefore follow the same first-match rule.

Route failure normally preserves established Navigation fallback behavior. A forward path with no matching route continues as the existing
Navigation target. Reverse generation with no applicable route retains the concrete menu alias when one exists, otherwise the Area-aware canonical
href. A reverse candidate missing any required placeholder value is skipped without throwing, allowing a later candidate or canonical fallback.
Canonical Page URLs remain directly navigable whether or not a route exists. Bad route declarations still fail during route compilation, and malformed
percent encoding in a matched parameter remains an invalid-URL error rather than a silent no-match.

Area prefixes form the boundary between public URLs and Area-relative routes. Route paths in `module.routes` and `area.routes` never acquire the
prefix. For forward resolution, Navigation selects the Area and removes its prefix before matching; it then applies that prefix exactly once to the
canonical Page href. Reverse resolution removes an existing canonical Area prefix for target comparison and applies the selected Area's prefix
exactly once to the generated route. An empty/root prefix performs neither operation, preserving the route's single leading slash. When one module
participates in multiple Areas, the existing explicit href, originating Page, target-module, and current/default Area selection rules choose the
public context.

For example, a route `/repository/{repositoryId}/items` in Area `demo` round-trips as follows:

```text
canonical: /_assets/x-demo/pages/items.js?repositoryId=12
public:    /demo/repository/12/items
forward:   /demo/repository/12/items → /demo/_assets/x-demo/pages/items.js?repositoryId=12
```

`x-anchor` preserves its logical `href` as the canonical target and delegates its rendered native `<a href>` to
`Navigation.buildUrlAbsolute(...)`. The native href can therefore expose the friendly route for status-bar previews, copying, middle-click, and new
tabs, while intercepted navigation still sends the unchanged canonical href to Navigation. The component does not read `area.routes`, parse route
patterns, or substitute placeholders. Path and hash modes apply `App.BasePath` and the configured hash prefix only after Navigation has selected the
menu alias, reverse route, or canonical fallback.

A menu item may provide both `path` and `href`. `path` is an optional friendly/public navigation path; `href` is the canonical XShell navigation
target. `path` is an alias, not a replacement for `href`. A menu item without `path` remains valid and menu-facing UI naturally falls back to
`href` through `menuitem.path || menuitem.href`.

For example, a module may contribute:

```jsonc
{ "label": "Components", "path": "/components", "href": "/_assets/x-demo/pages/01-components/index.js" }
```

For an Area with prefix `/main`, the effective item uses Area-aware values for both fields:

```jsonc
{ "label": "Components", "path": "/main/components", "href": "/main/_assets/x-demo/pages/01-components/index.js" }
```

`x-menu`, `x-page-menu`, and search results pass `path || href` to their generic navigation elements. `x-menuitem` and `x-anchor` receive only an
`href`; neither knows about menu paths or performs path-to-href translation. Breadcrumb lookup starts with the canonical Area-aware `href`, and
the returned breadcrumb entries carry their `path` for UIs that choose to expose the friendly link.

Navigation accepts either form. When a browser/navigation URL matches `Areas.resolvePath(value)`, Navigation replaces it with that effective menu
item's canonical `href` before setting `x-page.src`; when no path matches, it preserves the original value. Thus `/main/components` and direct
`/main/_assets/x-demo/pages/01-components/index.js` navigation both remain valid. Navigation translates to the Area-aware canonical href, not to
the final module resource URL.

For browser-facing links, Navigation performs the inverse lookup through `Areas.resolveHref(href, areaId)`. For an unprefixed module resource,
Areas derives the module id from `/_assets/<module-id>/...`, searches the effective menus of Areas associated with that module, and returns the
matching item, including items materialized from dynamic menu sources. The originating Page's Area is tried first when applicable; otherwise the
target module's participating Areas determine the public path. Query parameters and fragments remain attached to the public path. When no matching
item or friendly `path` exists, the Area-aware canonical href remains the browser-facing fallback. The logical target retained by `x-anchor` stays
canonical, so normal clicks and native browser link actions share the same destination identity without making the component aware of Areas or menus.

`x-page` receives that canonical Area-aware href, identifies its Area, and removes only the Area prefix before asking the loader/resolver for the
module resource. The loader/resolver therefore sees `/_assets/x-demo/pages/01-components/index.js`; it does not know about menu `path` values or
perform path-to-href translation.

## Ordinary Page anchors

Inside `x-page`, ordinary internal anchors (including relative Page links and `.md` documents) call Navigation with the originating Page context.
Fragment-only links such as `#section`, explicit URI schemes (`https:`, `mailto:`, `tel:`, etc.), and protocol-relative URLs (`//example.com/path`)
remain native browser links. The `#!` application-navigation prefix is preserved.

Modified clicks, non-primary buttons, already-prevented events, downloads, and explicit non-self browsing targets also retain native behavior.
An absent/empty target or `_self` permits interception of an internal link. `x-anchor` remains an explicit XShell navigation component.

## Hash mode

Navigation listens for hash changes, decodes the page stack, and updates `x-page` elements. The configured `hashPrefix` is `#!`. Because the
destination is in the fragment, the server needs no path fallback for deep links.

After reverse route generation, hash mode wraps the public application path with `App.BasePath` and `hashPrefix`. With `App.BasePath` `/app`, Area prefix
`/demo`, and public route `/repository/12/items`, the browser href is `/app/#!/demo/repository/12/items`.

```text
host page#!/_assets/test/pages/test1.js → Navigation → x-page → page resource
```

## Path mode

Path mode uses `history.pushState()`, `history.replaceState()`, and `popstate` with the same Pages and encoded stack data as hash mode. It removes the
configured application base path before interpreting the browser URL. Direct loads and refreshes require host collaboration so a deep application
path returns the XShell host page. `Extensions.UseXShell()` provides that SPA fallback within `App.BasePath`, while allowing configured reserved
prefixes and already-selected ASP.NET endpoints to continue through the pipeline.

After reverse route generation, path mode prepends `App.BasePath` directly. With `App.BasePath` `/app`, Area prefix `/demo`, and public route
`/repository/12/items`, the browser href is `/app/demo/repository/12/items`.

```jsonc
{ "xshell": { "navigation": { "mode": "path", "hashPrefix": "#!" } } }
```

The nested names match `xshell.jsonc`; its checked-in default is currently `path`. The runtime `Navigation` constructor reads
`config.xshell.navigation.mode` and `hashPrefix`.

On a fresh load at the mode's empty/root URL, Navigation uses the default Area's `home`, derived from the first depth-first navigation item marked
`default: true`, with a fallback to the first depth-first item having `path || href`. No visibility flag is tested.
Startup fails clearly if that home is absent. With an existing URL, Navigation restores the encoded page stack. When the root page
finishes loading, it emits `xshell:navigation:end`; Areas resolves the current Area from the longest matching prefix. `x-page` selects its
breadcrumb Area from the URL itself so lookup does not depend on the later navigation-end event.

A non-empty Area prefix wraps both friendly paths and canonical hrefs. For example, the browser can show `#!/customers/reports` in hash mode or
`/customers/reports` in path mode, while `x-page` receives `/customers/_assets/reports/pages/report.js`. `x-page` removes the Area prefix before
resolving the module resource; the `/_assets/reports/...` segment still denotes the resource owner.

The query portion of a Page destination remains part of its canonical Page `src`. Navigation preserves that input, while `page-js` exposes raw
query access to a definition-based Page controller as `controller({ query })` and separately interprets query parameters for contract properties
explicitly marked `query: true`. Navigation parses or owns neither mechanism; see [Pages](70-pages.md#controller-services).

## Query-only Page updates

Despite its name, `Page.replaceQuery(query)` patches only that Page's existing query parameters; it does not replace the complete query map. The Page
delegates to `Navigation.replacePageQuery(page, changes)`, which locates the Page's `x-page` host in the current stack, clones only the matching stack
item's `params`, preserves its `href` and `nav` metadata, and calls `_stackToBrowser(stack, { replace: true })`.

```js
page.replaceQuery({
    "page-index": "3", // set or replace
    filter: null         // remove
});
```

Each value sets or replaces that query parameter, `null` removes it, and `undefined` leaves it unchanged; unrelated existing query parameters are
preserved. The Page and its host synchronize their logical source internally without invoking the normal `src` loading setter.

Because `_stackToDom()` compares `href` rather than query parameters, a query-only reflection does not recreate, reload, unmount, or mount the Page.
Pages opened as dialogs or embeds are outside the browser navigation stack; their local query may be patched, but they do not rewrite the main browser
URL. Contract-property reflection is decided by `page-js`: only `query: true` plus `reflect: true` participates, and query values use the property's
declared scalar contract type.

The legacy `x-page` `replace`/`navigate` listener code is commented out, so it is not an active navigation API.
Use Navigation methods and browser-history paths.

## Intents

Navigation intents are outside V0. V0 defines neither `module.contract.intents` nor an intent registry/dispatch API; use supported Page hrefs/routes.
A future intent feature may define its configuration/schema together with its runtime semantics.

See [Pages](70-pages.md) and [ADR-0003](../adr/0003-navigation.md).

See [Hosting](40-hosting.md) for path fallback, [Dialogs](../subsystems/30-dialogs.md) for helper Pages, and [Bus](../subsystems/20-bus.md) for notifications.

## Opening modes

`navigate` supports `top` (replace the logical root), `stack` (append), `dialog` (separate dialog Page), and `embed` (named outlet in the originating
Page). `replace: true` controls browser-history replacement for stack operations.
The default `auto` requires the originating `page`: it updates that Page's stack position, or its local host if it is a dialog/embed.
Supply `open: "top"` when navigating without an originating Page.
Current embed handling accepts an outlet name string and looks for `x-page[outlet="name"]` under `page.host`; passing an element is not implemented.
