# AGENTS.md

## Scope

These instructions apply only to:

```text
src/DProjects.XShell/**
```

Do not inspect or infer architecture from sibling projects in `marcdp/libs` unless explicitly requested.

When referring to **XShell**, **the project**, or **the repository code**, assume:

```text
marcdp/libs/src/DProjects.XShell
```

Files inside this directory are the authoritative source for XShell.

---

## Source of truth

When determining current behavior, use this order:

1. current implementation;
2. schemas and validation;
3. representative usages and tests;
4. documentation;
5. ADRs.

Documentation marked **Draft**, **TODO**, **proposed**, or **future work** must not be treated as implemented behavior.

If implementation and documentation disagree, investigate the discrepancy rather than silently choosing one.

Do not infer XShell behavior from conventions in other frameworks.

---

## Working procedure

For non-trivial changes:

1. Inspect the relevant implementation.
2. Inspect nearby code implementing similar behavior.
3. Search for consumers and usages.
4. Inspect relevant documentation.
5. Inspect schemas and ADRs where applicable.
6. Preserve existing terminology and architectural boundaries.
7. Implement the smallest coherent change.
8. Update contracts, schemas, tests, demos, documentation, or ADRs when required.
9. Run relevant validation when available.

Do not report tests or validation as passing unless they were actually executed.

---

## Important paths

```text
src/DProjects.XShell/
├── Commands/                       # CLI commands
├── Middlewares/                    # ASP.NET Core resource/dev middleware
├── Services/                       # Server/build-time services
│   └── XTemplate/                  # X Template compiler
└── Resources/DProjects.XShell/
    ├── docs/                       # Canonical documentation
    ├── modules/
    │   ├── x/                      # Core UI module
    │   └── x-demo/                 # Demo/sample module
    └── xshell/                     # Browser runtime
        ├── loaders/
        ├── render-engines/
        ├── state-engines/
        ├── schemas/
        ├── validation/
        └── tests/
```

Everything under:

```text
Resources/DProjects.XShell/
```

is runtime content and may be referenced dynamically.

Do not classify runtime resources as unused based only on static imports.

---

## Architectural invariants

Preserve these distinctions:

```text
resolver           ≠ loader
contract           ≠ implementation
public properties  ≠ private state
state engine       ≠ render engine
loader lifecycle   ≠ engine responsibility
module definition  ≠ module import ≠ runtime module instance
module defaults    ≠ global XShell UI configuration
module             ≠ Area
Page               ≠ Navigation
configuration      ≠ runtime state
configUrl          ≠ assetsUrl
packaging          ≠ runtime ZIP loading
```

Do not collapse these concepts merely to reduce code.

---

## Modules and configuration

Module specifications are normally authored as:

```text
module.jsonc
```

JSONC is intentional.

When changing module configuration:

- preserve JSONC support and useful comments;
- verify fields against implementation and schemas;
- search consumers;
- update specification documentation when contracts change.

Modules may contribute metadata, imports, params, styles, controllers, menus, defaults, configuration, resolvers, UI resources, and public contract metadata.

Do not assume declarative fields are runtime-enforced unless the implementation actually enforces them.

Bootstrap recursively discovers imports and builds canonical module definitions.

Repeated imports of the same canonical module do not imply multiple live runtime instances.

Keep these concepts separate:

```text
module definition
module import
runtime module instance
```

---

## Module defaults

Resolved modules define defaults for:

```text
defaults.component
defaults.page
```

Each currently selects:

```text
renderEngine
stateEngine
```

Resource-level `meta` may override module defaults.

Do not invent a global render/state-engine fallback under `xshell.ui`.

Module defaults and global UI configuration are separate concerns.

---

## Areas and menus

An **Area** is a navigation context composed from participating modules.

Modules contribute reusable menus; Areas compose them.

Do not treat Areas as module instances.

Menu entries are navigation data, not module imports.

When changing Area or menu behavior, inspect:

```text
xshell/areas.js
xshell/navigation.js
docs/subsystems/areas.md
```

plus relevant module definitions and ADRs.

---

## Resource namespace and Service Worker

The current virtual asset namespace is configured through:

```text
xshell.assetsPrefix
```

The checked-in prefix is:

```text
_assets
```

producing URLs such as:

```text
/_assets/x/components/x-button.js
```

Do not describe `/_cdn` as the current namespace.

Treat changes to `/_assets` as architectural and potentially breaking.

Before changing resource URL behavior, inspect bootstrap, Service Worker mappings, resolvers, import maps, module URLs, documentation, and ADR-0001.

Keep:

```text
configUrl
```

and:

```text
assetsUrl
```

conceptually separate.

Do not assume ZIP-backed loading, arbitrary remote/CDN sources, or automatic module-file-manifest consumption unless the current implementation supports them.

---

## Resolvers and loaders

The normal resource flow is:

```text
logical resource
    ↓
Resolver
    ↓
URL + loader
    ↓
Loader
    ↓
resource
```

A resolver determines what a logical reference means and which loader should handle it.

A loader obtains or constructs the concrete resource.

Do not move generic loading, lifecycle, state, or rendering responsibilities into resolvers.

Preserve loader pluggability and resolver/loader separation.

Definition-based Components and Pages may declare:

```js
dependencies: {
    helper: "module:/some/resource.js"
}
```

These dependencies must use the normal Resolver → Loader pipeline.

This is declarative resource loading, not a separate dependency-injection framework.

---

## Components and Pages

Core components live under:

```text
Resources/DProjects.XShell/modules/x/components/
```

A component module may default-export either:

```text
Web Component class
```

or:

```text
component definition object
```

For definition-based components, the loader constructs the final Web Component class.

Definition-based Pages reuse the same general contract, controller, properties/state, dependencies, render-engine, and state-engine model.

Conceptually:

```text
Page = Component + Navigation
```

Do not move navigation responsibilities into generic component infrastructure.

Before modifying Components or Pages:

- inspect similar implementations;
- inspect their public contract;
- follow current lifecycle conventions;
- preserve existing naming and resource conventions;
- reuse current utilities where appropriate.

Do not introduce a parallel component model.

---

## Component contract and implementation

The current public metadata export is:

```js
export const contract = {
    ...
};
```

Do not rename or describe it as `manifest` unless the implementation changes accordingly.

The contract schema is:

```text
xshell/schemas/component.contract.schema.json
```

The implementation schema is:

```text
xshell/schemas/component.schema.json
```

The contract describes public API such as:

```text
description
properties
events
methods
slots
examples
```

The default export describes runtime implementation, including concepts such as:

```text
dependencies
meta
style
template
templateRenderer
state
controller
```

Keep contract and implementation separate.

Before adding fields, verify both schema support and runtime consumption.

Do not invent contract fields.

---

## Properties, state, methods, and slots

Properties are public API.

State is private reactive implementation data.

Do not treat them as the same abstraction.

Public property defaults come from:

```text
contract.properties[*].default
```

A property may explicitly participate in state through:

```js
state: true
```

This does not mean every property automatically maps to state.

Follow current loader behavior and ADR-0004 when changing property/state rules.

Controller methods are private by default.

Only methods declared in:

```text
contract.methods
```

should become public Web Component methods.

Do not automatically expose every controller function.

Public slots belong in:

```text
contract.slots
```

The empty key:

```js
""
```

represents the default slot.

Slot metadata describes the public composition API; it does not create `<slot>` elements.

Generic slot-contract validation belongs to the loader/contract layer, not to a specific render engine.

---

## State and render engines

State engines live under:

```text
xshell/state-engines/
```

Render engines live under:

```text
xshell/render-engines/
```

A state engine owns reactive state behavior.

A render engine owns rendered output.

Neither should own generic loader responsibilities such as:

- public contracts;
- controller construction;
- navigation;
- general lifecycle orchestration.

Do not merge state-engine and render-engine responsibilities.

Module defaults select engines for definition-based Components and Pages unless overridden by resource `meta`.

---

## X Templates

X Templates are an optional XShell rendering extension, not the core component model.

Server/compiler code lives under:

```text
Services/XTemplate/
```

When changing X Templates:

- inspect the parser/compiler;
- inspect real templates;
- inspect X Template documentation;
- preserve parsing/code-generation boundaries;
- update language documentation when syntax changes.

Do not infer syntax or semantics from JSX, Vue, Angular, Svelte, Lit, Razor, Handlebars, or similar systems.

Do not invent undocumented syntax.

---

## Navigation

When modifying navigation:

- inspect `xshell/navigation.js`;
- inspect Areas and menus;
- inspect Page behavior;
- inspect navigation documentation;
- inspect ADR-0003.

Preserve the distinction between:

```text
friendly navigation path
canonical Page/resource href
```

Do not make generic loaders responsible for translating menu paths.

Global application UI resources under:

```text
xshell.ui
```

configure infrastructure such as layouts, dialogs, lazy components, and error components.

They are not module render/state-engine defaults.

---

## Schemas and validation

Schemas live under:

```text
xshell/schemas/
```

Validation code lives under:

```text
xshell/validation/
```

When changing a contract or configuration model:

1. inspect the schema;
2. inspect validation;
3. inspect runtime consumers;
4. update affected layers consistently;
5. update documentation.

Do not use documentation alone as proof that a field is supported.

---

## Packaging

Modules are authored as expanded directories.

The project also provides explicit module packaging using:

```text
Commands/Pack.cs
Services/ModuleFilesIndexer.cs
Services/ModuleFileCompiler.cs
```

Packaging can create immutable ZIP packages and physical file inventories.

Do not infer from packaging support that the browser runtime can load modules directly from ZIP files.

Do not manually list every module file in `module.jsonc` merely to support packaging inventory.

The documentation currently records an unresolved naming inconsistency between:

```text
module.files.json
modules.files.json
```

Do not establish either as a permanent public contract unless the implementation is unified.

---

## Documentation and ADRs

Canonical documentation lives under:

```text
Resources/DProjects.XShell/docs/
```

It is runtime content.

Do not create a second canonical documentation tree.

Documentation section landing pages use:

```text
index.md
```

not `README.md`.

Use relative links.

When public behavior or architecture changes, update the relevant documentation.

Architecture Decision Records live under:

```text
docs/adr/
```

ADR filenames follow:

```text
NNNN-short-description.md
```

Do not renumber existing ADRs.

Before reversing an architectural decision, inspect and update the relevant ADR.

---

## Tests and x-demo

Runtime tests live under:

```text
xshell/tests/
```

The `x-demo` module provides representative examples of supported behavior.

Use tests and demo code as supporting evidence, but do not assume one example defines the entire public contract.

When changing public behavior, inspect whether tests and `x-demo` should also change.

---

## Coding rules

### JavaScript

Prefer:

- native ES modules;
- browser APIs;
- existing XShell utilities;
- explicit APIs;
- minimal dependencies.

Preserve CSP compatibility.

Do not introduce TypeScript unless explicitly requested.

Vendored third-party code should include the applicable license.

### C#

The project currently targets:

```text
net10.0
```

Prefer .NET and ASP.NET Core functionality over unnecessary dependencies.

Keep hosting/build-time concerns separate from browser runtime concerns.

Do not move browser framework logic into C# without an architectural reason.

---

## Backward compatibility

Treat these as potentially public contracts:

- module configuration fields and ids;
- component/custom-element names;
- public contracts;
- properties, methods, events, and slots;
- public JavaScript exports;
- render/state-engine names;
- X Template syntax;
- resolver resource types and loader names;
- asset URL semantics;
- navigation semantics;
- documentation URLs.

Do not break them accidentally.

For intentional breaking changes, update known consumers, schemas, tests/demo code, documentation, and ADRs as applicable.

---

## Avoid

Do not:

- inspect sibling projects as XShell architectural evidence unless requested;
- redesign unrelated subsystems;
- introduce speculative abstractions;
- duplicate existing mechanisms;
- merge Resolver and Loader responsibilities;
- merge render and state engine responsibilities;
- equate public properties with private state;
- expose controller methods unnecessarily;
- invent contract fields;
- call the current `contract` export `manifest`;
- invent X Template syntax;
- silently convert JSONC to JSON;
- describe `/_cdn` as the current asset namespace;
- casually rename `/_assets`;
- treat Areas as module instances;
- treat menus as module imports;
- treat ZIP packaging as ZIP runtime loading;
- assume dynamically referenced files are dead code;
- introduce unnecessary dependencies;
- create documentation `README.md` indexes;
- document future work as implemented behavior;
- claim tests or validation were run when they were not.

---

## When uncertain

Inspect the current project before deciding.

Prefer:

```text
implementation
+ schemas
+ usages/tests
+ documentation
+ ADRs
```

over conventions from other frameworks.

If sources disagree, identify the inconsistency rather than inventing a new convention.

Preserve existing architectural boundaries and compatibility unless the requested task explicitly changes them.