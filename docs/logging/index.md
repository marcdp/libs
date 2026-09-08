# Logging

This area will document the repository's logging abstractions and their integration with logging providers and the wider .NET ecosystem.

Future documentation should explain:

- What responsibilities belong to the repository's logging abstractions?
- How do they interoperate with `Microsoft.Extensions.Logging`?
- Where does OpenTelemetry integration fit?
- Which concerns belong in provider-specific projects?
- How does log storage relate to logging where the repository establishes that relationship?
- Which behaviors and integration boundaries are verified by tests?
