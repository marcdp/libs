# ADR-0002: Preserve Crypto v1 as a compatibility surface

## Status

Accepted

## Context

`DProjects.Crypto` v1 exposes established APIs and persisted-data formats used by existing consumers. Its AES format provides encryption but does
not provide authenticated encryption or integrity guarantees. During review, the repository needed to decide whether stronger modern security
semantics should replace or alter the existing subsystem.

## Options considered

### Option A: Redesign Crypto v1 in place

Replace existing behavior and formats with modern authenticated mechanisms.

### Option B: Retrofit stronger guarantees into the existing format

Add authentication and versioning while retaining the same public conceptual surface.

### Option C: Preserve v1 and reserve incompatible guarantees for v2

Maintain v1 contracts and persisted formats, strengthen validation and compatibility verification, document the limitations explicitly, and place
incompatible future security evolution in a separately designed Crypto v2.

## Decision

Option C was chosen. Existing consumers and persisted data depend on v1 behavior, so silent format or semantic changes would create compatibility
risk. AES-CBC confidentiality must not be represented as authentication or integrity. Compatibility maintenance and a new security architecture are
separate concerns: regression and negative tests can make v1 behavior more trustworthy without presenting it as a modern authenticated protocol.

This decision does not design Crypto v2 or select an algorithm for it.

## Consequences

### Positive

- Compatibility expectations are explicit and historical persisted data remains readable.
- Security limitations are documented rather than hidden.
- Tests protect known v1 format behavior while a future design remains free to make incompatible choices.

### Trade-offs

- Crypto v1 remains unsuitable where authenticated encryption or integrity is required.
- Historical algorithms remain available for compatibility.
- Consumers needing stronger guarantees require a separately designed v2 and migration path.

## Related evidence

- [Crypto v1 compatibility and security boundary](../crypto/index.md)
- [Support and status](../support.md)
- [Repository verification](../verification.md)
- [`DProjects.Crypto.Test`](../../test/DProjects.Crypto.Test/)
