# Factories

This area will document factories as an architectural extension mechanism, including why URL-based creation is used to decouple callers from concrete implementations.

Future documentation should explain:

- Why does `IFactoryByUrl<T>` exist?
- How do protocols select and configure providers?
- How are factories discovered and registered?
- How does the mechanism keep callers independent of concrete implementations?
- Why are protocol strings compatibility boundaries?
- What are the trade-offs versus central switches, explicit construction, DI-only registration, service location, and plugin registries?
