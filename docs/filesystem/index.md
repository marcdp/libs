# Filesystem

This area will document the architecture of the filesystem contracts, reusable implementations, and technology-specific providers.

Future documentation should explain:

- What behavior does the filesystem contract require?
- How do sync-first, async-first, and independent sync/async implementations differ?
- Which operations are reusable behavior, and which are provider primitives?
- How are optional capabilities and unsupported operations represented?
- Where are provider-specific dependencies and semantics kept?
- How do shared contract tests verify implementations?
