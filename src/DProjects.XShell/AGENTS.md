# AGENTS.md

## Scope

These instructions apply to all files under:

```text
/src/DProjects.XShell
```

This file is specific to the `DProjects.XShell` project.

Do not apply these rules to other projects in the `marcdp/libs` repository unless explicitly requested.

## Project overview

`DProjects.XShell` is the host project for XShell.

XShell is a modular application framework built around:

* Web Components
* optional declarative X Templates extension
* modules
* resource resolution
* pluggable loaders
* browser-side runtime infrastructure
* ASP.NET Core integration

The project contains:

* .NET hosting and integration code;
* browser/runtime resources;
* XShell modules and components;
* samples;
* XShell documentation that is also exposed as runtime content.

## Project structure

The project root is:

```text
src/DProjects.XShell/
```

Important files and directories include:

```text
src/DProjects.XShell/
├── AGENTS.md
├── DProjects.XShell.csproj
├── Extensions.cs
├── Program.cs
├── Services/
|   └── XTemplate/
└── Resources/
    └── DProjects.XShell/
        ├── docs/
        ├── modules/
        ├── samples/
        └── xshell/
```

The XShell documentation is located at:

```text
Resources/DProjects.XShell/docs/
```

The browser/runtime resources are located at:

```text
Resources/DProjects.XShell/
```

The main browser-side modules are under:

```text
Resources/DProjects.XShell/modules/
```

Current modules include:

```text
x/
x-debugger/
x-help/
```

The core XShell module is:

```text
Resources/DProjects.XShell/modules/x/
```

Its structure includes areas such as:

```text
modules/x/
├── components/
├── controllers/
├── css/
├── icons/
├── layouts/
├── pages/
├── utils/
├── vendor/
└── module.jsonc
```

## General working rules

Before modifying XShell behavior:

1. Inspect the relevant implementation.
2. Inspect nearby files that implement similar behavior.
3. Inspect relevant documentation under `Resources/DProjects.XShell/docs/`.
4. Search for consumers before changing public contracts.
5. Preserve existing terminology and architectural boundaries.
6. Prefer small coherent changes over broad refactors.
7. Do not introduce speculative abstractions.

Do not assume that a mechanism works like an equivalent feature in another framework.

The existing implementation and project documentation together define the intended XShell design.

When implementation and documentation disagree:

1. inspect the surrounding code and usages;
2. determine whether the discrepancy is stale documentation or an implementation defect;
3. do not silently choose one interpretation;
4. update both when the requested task establishes the intended behavior.

## Architectural principles

Preserve separation of responsibilities.

Important distinctions include:

* resolver vs loader;
* public properties vs internal state;
* module specification vs module implementation;
* template syntax vs generated JavaScript;
* configuration vs runtime state;
* browser runtime vs .NET hosting;
* documentation content vs the UI used to browse it.

Do not collapse these concepts merely to reduce code.

Prefer explicit contracts and composable mechanisms.

## Modules

Modules are a primary organizational and runtime concept in XShell.

Module specifications are stored in files such as:

```text
module.jsonc
```

When modifying module behavior:

* inspect existing `module.jsonc` files;
* preserve JSONC support;
* preserve comments where useful;
* do not silently convert JSONC to strict JSON;
* verify runtime support before adding new schema fields;
* keep resources organized according to existing module conventions;
* update the corresponding documentation when the module contract changes.

Do not infer module schema fields from documentation alone; verify them against the runtime implementation.

## Resolvers

Resolvers determine how logical XShell resource references are resolved.

Keep resolution separate from loading.

A resolver should primarily determine:

* what resource a logical reference represents;
* how the reference is normalized or resolved;
* which loading mechanism should handle the resolved resource.

Do not move resource loading or transformation logic into resolvers without an explicit architectural reason.

Before introducing a new resolver:

* inspect existing resolver implementations;
* reuse established conventions;
* avoid hard-coded resource-type branching when a pluggable mechanism already exists;
* inspect relevant architecture documentation.

## Loaders

Loaders are responsible for loading or producing concrete resources.

A loader may delegate specialized work to loader-specific engines or processors.

Keep loader responsibilities separate from resolver responsibilities.

When modifying loaders:

* preserve pluggability;
* avoid coupling generic loader infrastructure to a single resource type;
* inspect current loader registration and dispatch behavior;
* preserve URL and module resolution contracts;
* update relevant architecture documentation when behavior changes.

## Web Components

Core components are primarily located at:

```text
Resources/DProjects.XShell/modules/x/components/
```

Before creating or modifying a component:

1. Inspect similar existing components.
2. Inspect the component documentation.
3. Follow the current lifecycle conventions.
4. Reuse existing helpers where appropriate.
5. Preserve existing custom element naming patterns.
6. Do not introduce a parallel component model.

Use existing components as the primary reference for implementation style.

For component manifest work, inspect existing component manifests before changing manifest behavior.

A useful reference implementation is:

```text
Resources/DProjects.XShell/modules/x/components/x-datafields.js
```

Do not assume that one component demonstrates every supported manifest feature.

## Component manifests

Component manifests describe component metadata and public contracts.

When modifying manifests:

* preserve existing field names and semantics;
* verify runtime support before adding fields;
* keep public API declarations explicit;
* avoid placing arbitrary runtime state in manifests;
* do not invent manifest fields based on conventions from other libraries;
* update the component manifest documentation when the contract changes.

Example shape:

```js
export const manifest = {
    properties: {
        label: {
            type: "string",
            default: "",
            attribute: true,
            state: true
        }
    }
};
```

This is illustrative only. Supported fields must be verified against the current runtime.

## Properties and state

Properties and state are different concepts.

### Properties

Properties represent the public API of a component.

They describe values exposed to component consumers.

### State

State represents internal reactive component data.

State is generally a private implementation detail and should not automatically become public API.

### Property-to-state synchronization

Do not automatically map every property to state.

Synchronization must be explicit.

For example:

```js
label: {
    type: "string",
    default: "",
    attribute: true,
    state: true
}
```

may indicate that the public `label` property participates in internal reactive state.

This does not mean properties and state are the same abstraction.

Internal-only reactive values should remain state and should not be promoted to public properties solely for convenience.

If the property/state model changes, update both the implementation and the corresponding documentation or ADR.

## X template language

X Templates are an optional XShell-owned extension. They integrate with the core component model but are not part of it.

Do not assume syntax or semantics from:

* JSX
* Vue
* Angular
* Svelte
* Lit
* Razor
* Handlebars
* other template systems

When modifying the X template compiler:

1. Inspect the existing compiler.
2. Inspect real templates in this project.
3. Inspect the template language documentation.
4. Identify the currently supported syntax.
5. Preserve backward compatibility unless explicitly changing the language.
6. Keep parsing and code generation conceptually separate.
7. Avoid accidentally widening the supported JavaScript expression subset.

The existing compiler and templates are the source of truth for implemented behavior.

The documentation is the source of truth for intended public language contracts where those contracts are explicitly specified.

Do not invent undocumented syntax.

Any intentional syntax change must be reflected in the template documentation.

## JavaScript

The browser-side XShell runtime is primarily JavaScript.

Follow the style used by nearby files.

Do not introduce TypeScript unless the task explicitly requires it.

Prefer:

* native browser APIs;
* existing project utilities;
* explicit APIs;
* minimal dependencies.

Avoid adding third-party JavaScript dependencies for functionality already available in the browser or existing XShell utilities.

For framework APIs:

* use stable names;
* avoid unnecessary hidden side effects;
* preserve compatibility where possible;
* keep public contracts explicit.

## C#

The project targets:

```text
net10.0
```

Nullable reference types and implicit usings are enabled.

The project uses ASP.NET Core through:

```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

Resources under:

```text
Resources/**
```

are copied to the output directory.

When modifying C# code:

* preserve the existing project style;
* prefer .NET and ASP.NET Core platform functionality;
* avoid new NuGet dependencies unless clearly necessary;
* keep hosting concerns separate from browser-runtime concerns;
* do not move JavaScript framework behavior into C# without an explicit architectural reason.

## Runtime resources

Everything under:

```text
Resources/DProjects.XShell/
```

is runtime content.

This includes:

* modules;
* samples;
* XShell runtime files;
* documentation.

Be careful when renaming, moving, or deleting files.

Resources may be referenced dynamically and therefore may not have static import references.

Before deleting or moving a resource:

1. search JavaScript;
2. search module specifications;
3. search templates;
4. search URLs and string references;
5. search loader/resolver configuration;
6. inspect samples;
7. inspect documentation.

Do not classify a resource as unused based only on static imports.

## Documentation as runtime content

The documentation is intentionally located under:

```text
Resources/DProjects.XShell/docs/
```

This is not only repository documentation.

It is part of the XShell runtime resources so that XShell can browse and present its own documentation.

Treat this as an intentional architectural decision.

Do not move the documentation outside `Resources/DProjects.XShell/` unless explicitly requested.

Do not duplicate the documentation into a separate repository-level documentation tree.

Prefer one canonical documentation source.

The help or documentation UI should consume this documentation rather than maintaining a second copy of the same content.

## Documentation structure

Every documentation directory should use:

```text
index.md
```

as its landing page.

Do not use `README.md` as a documentation section index.

Prefer a structure such as:

```text
Resources/DProjects.XShell/docs/
├── index.md
├── architecture/
│   └── index.md
├── components/
│   └── index.md
├── subsystems/
│   └── index.md
├── extensions/
│   ├── index.md
│   └── x-templates/
│       └── index.md
├── specifications/
│   └── index.md
└── adr/
    └── index.md
```

`architecture/` documents system design. `components/` documents the core Web Component model. `subsystems/` documents runtime services such as authentication, identity, and i18n. `extensions/` documents optional capabilities; X Templates live under `extensions/x-templates/` and are not part of the core component model. `specifications/` contains formal configuration contracts, and `adr/` records architectural decisions.

Use relative Markdown links.

Prefer directory-style links where they can naturally resolve to `index.md`.

For example:

```md
[Components](components/)
```

This keeps the documentation compatible with standard documentation browsers and XShell's own documentation browser.

## Documentation rules

Documentation must be source-backed.

Before documenting implementation behavior:

1. inspect the relevant source;
2. inspect representative usages;
3. verify terminology against existing documentation;
4. avoid turning assumptions into contracts.

If behavior cannot be established confidently, use an explicit placeholder such as:

```text
TODO: Document this once the runtime contract is finalized.
```

Do not invent framework behavior to make documentation appear complete.

Keep documentation focused on:

* architecture;
* concepts;
* public contracts;
* supported behavior;
* design decisions;
* language specifications;
* configuration specifications.

Avoid duplicating trivial implementation details that are better expressed by the source code.

## Documentation responsibilities

When a code change modifies any of the following:

* public behavior;
* architecture;
* component contracts;
* manifest semantics;
* module specifications;
* template syntax;
* navigation behavior;
* resolver behavior;
* loader behavior;
* service-worker behavior;
* reserved URL formats;

inspect and update the corresponding documentation in the same task when appropriate.

Conversely, documentation changes that define a new contract should not silently diverge from implementation.

If documentation intentionally describes a future design rather than current behavior, mark that distinction clearly.

## Documentation browsing

The documentation is intended to be browsable from XShell itself.

When changing documentation paths or navigation:

* preserve stable relative links where practical;
* preserve `index.md` conventions;
* avoid assumptions tied to a single external documentation generator;
* consider how XShell resolves and loads Markdown resources;
* keep the documentation tree directly navigable.

Do not add a static documentation framework unless explicitly requested.

Do not introduce generated documentation output into the runtime resource tree unless that becomes an explicit architectural decision.

## Architecture Decision Records

Architecture decisions belong under:

```text
Resources/DProjects.XShell/docs/adr/
```

Use ADRs for decisions that explain why XShell behaves or is structured a certain way.

Examples include:

* properties versus state;
* JSONC specifications;
* navigation strategy;
* reserved resource URL namespaces;
* documentation as runtime content.

Do not silently reverse an architectural decision in implementation code.

When making a significant architectural change:

1. inspect the relevant ADR;
2. update it or add a new decision document when appropriate;
3. keep implementation and architectural documentation consistent.

ADR filenames use stable four-digit numeric prefixes in the form `NNNN-short-description.md`. Assign the next sequential number to new ADRs; never reuse or renumber existing ADR numbers.

## Service worker and reserved URLs

XShell may use reserved URL namespaces for resources managed by its browser runtime or service worker.

A prefix such as:

```text
/_cdn
```

may form part of the runtime contract.

Do not rename reserved URL prefixes as cleanup.

Before changing one:

* identify all producers;
* identify all consumers;
* inspect service-worker handling;
* inspect resolvers;
* inspect loaders;
* inspect generated URLs;
* inspect corresponding architecture documentation;
* consider compatibility.

Treat such changes as architectural changes.

When the design changes, update the corresponding ADR.

## Navigation

Navigation is part of the XShell runtime architecture.

When modifying navigation:

* inspect the existing navigation implementation;
* inspect navigation documentation and ADRs;
* preserve current URL semantics unless the task explicitly changes them;
* distinguish hash-based navigation from path-based navigation;
* do not mix both approaches accidentally;
* consider browser history behavior and direct URL loading;
* consider documentation navigation when changes affect generic resource browsing.

Do not introduce a navigation framework solely to replace existing XShell mechanisms.

## Authentication, identity, and i18n

Authentication, identity/IDP, and internationalization are intentional XShell subsystems.

Do not remove them merely because they are not required by every application.

Keep subsystem boundaries clear.

Avoid adding unrelated application infrastructure to XShell core unless it belongs at framework level.

When subsystem contracts change, update the corresponding documentation.

## Configuration

Prefer declarative configuration where XShell already provides configuration mechanisms.

Do not add parallel configuration systems.

For configuration formats:

* preserve JSONC where used;
* preserve comments when useful;
* distinguish configuration from runtime state;
* validate new fields against runtime consumers;
* update specification documentation when configuration contracts change.

## Samples

Samples under:

```text
Resources/DProjects.XShell/samples/
```

should reflect supported framework behavior.

When changing a public XShell feature, inspect whether a relevant sample should be updated.

Do not modify samples to demonstrate behavior that is not actually supported by the runtime.

Samples may also be useful for manual validation when automated tests do not cover browser behavior.

## Backward compatibility

Treat the following as potentially public contracts:

* module specification fields;
* component names;
* custom element names;
* manifest fields;
* public JavaScript exports;
* X template syntax;
* resolver behavior;
* loader behavior;
* resource URL formats;
* documentation URLs and navigation paths;
* navigation semantics.

Do not break these accidentally.

If a requested task requires a breaking change:

* make the change explicit;
* update all known consumers;
* update relevant samples;
* update corresponding documentation;
* update ADRs when the architectural decision changes.

## Refactoring

Prefer focused refactoring.

Do not combine unrelated cleanup with feature changes.

Do not rename concepts merely for stylistic preference.

Before removing an abstraction, verify whether it participates in:

* dynamic loading;
* module configuration;
* URL resolution;
* runtime registration;
* template compilation;
* service-worker behavior;
* documentation browsing;
* documented public contracts.

Avoid premature consolidation of subsystems that have intentionally different responsibilities.

## Dependencies

Avoid adding dependencies unless necessary.

For JavaScript:

* prefer native browser APIs;
* prefer existing utilities;
* avoid framework dependencies.

For C#:

* prefer the .NET platform and ASP.NET Core;
* avoid unnecessary NuGet packages.

For documentation:

* do not add Docusaurus, MkDocs, VitePress, DocFX, or another documentation framework unless explicitly requested;
* do not introduce a build-time documentation dependency merely to browse Markdown that XShell can already load.

Any new dependency should have a clear architectural or functional justification.

## Validation

For C# changes, build the project when possible:

```bash
dotnet build src/DProjects.XShell/DProjects.XShell.csproj
```

For JavaScript, template, module, or runtime changes:

* inspect affected consumers;
* run relevant tests if they exist;
* exercise the closest sample when practical;
* verify browser/runtime assumptions;
* verify module and resource resolution where affected.

For URL, loader, resolver, service-worker, or documentation-serving changes, validate the complete resource flow rather than only the modified function.

For documentation changes, verify:

* relative links;
* `index.md` navigation;
* source paths;
* terminology;
* consistency with implementation;
* consistency with related ADRs and specifications;
* that the documentation remains loadable from its runtime resource location.

Do not report tests or validation as passing unless they were actually executed.

## Working procedure

For non-trivial changes:

1. Identify the relevant subsystem.
2. Inspect nearby implementation files.
3. Inspect relevant documentation.
4. Search for usages and consumers.
5. Understand the existing architectural boundary.
6. Implement the smallest coherent change.
7. Update samples when required.
8. Update documentation when behavior or contracts change.
9. Update ADRs when architectural decisions change.
10. Run appropriate validation.
11. Summarize unresolved architectural questions or inconsistencies.

## Avoid

Do not:

* redesign unrelated subsystems;
* introduce speculative abstractions;
* duplicate existing mechanisms;
* merge resolver and loader responsibilities;
* equate properties with state;
* expose private state unnecessarily;
* invent manifest fields;
* invent X template syntax;
* silently change module schemas;
* convert JSONC files to strict JSON without reason;
* rename reserved URLs casually;
* add dependencies without justification;
* assume dynamically loaded files are dead code;
* replace XShell mechanisms with third-party frameworks without explicit instruction;
* let documentation and implementation silently diverge;
* create `README.md` files as documentation indexes;
* create a second canonical copy of the XShell documentation outside the runtime resource tree.

## When uncertain

Inspect the project before deciding.

Prefer existing XShell patterns over generic framework conventions.

Consult both implementation and documentation.

If two implementations disagree, identify the inconsistency instead of silently choosing a new convention.

If implementation and documentation disagree, call out the discrepancy and determine which one reflects the intended contract.

Preserve compatibility unless resolving the inconsistency is explicitly part of the task.
