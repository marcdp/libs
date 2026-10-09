# Browser Platform Baseline

XShell V0 targets Web Platform capabilities that are **Baseline 2026** or earlier. It is intentionally browser-native and Web Platform first: the
runtime uses standardized browser capabilities directly rather than adding a compatibility layer.

Representative capabilities include ES modules, dynamic import, JSON import attributes, Web Components, Shadow DOM, constructable stylesheets and
`adoptedStyleSheets`, CSS `@scope`, Service Workers, IndexedDB, Fetch, and the standard `URL` and `URLSearchParams` APIs. This is descriptive,
not a separately maintained exhaustive browser-feature matrix; the normative compatibility floor is Baseline 2026.

## V0 compatibility contract

XShell does not provide legacy-browser compatibility, browser polyfills, or transpiled/downlevel browser builds. Support for browsers below the
Baseline 2026 platform target is outside V0.

## Secure context

XShell requires a secure context because it relies on browser platform capabilities such as Service Workers. Production applications must therefore
be served over HTTPS. Trusted local development origins supported by browsers, such as `localhost`, may use the browser's normal secure-context
development exception; this does not change the production HTTPS requirement.

The Baseline 2026 target is a platform compatibility contract. It does not claim that every browser is continuously exercised by a particular test
matrix.
