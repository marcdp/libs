# Verification

Verification in `DProjects.Libs` follows architectural boundaries rather than optimizing for a raw test count. Confidence comes from combining
focused behavior tests, reusable contract suites, provider-specific checks, explicit integration categorization, selected live-provider CI, and a
repository-wide build and package pass.

```text
focused semantics + shared contracts + provider differences + integrations
                                  ↓
                 restore → Release build → tests → pack
```

Each layer answers a different question. None is a substitute for the others.

## Focused tests

Focused tests isolate a public behavior or regression: parsing, boundary values, cancellation, stream ownership, disposal, serialization, security
handling, error categories, and lifecycle. They are especially valuable where a small edge case changes a public contract, such as a partial stream
crossing its limit or a database reader advancing through mixed read methods.

Hardening tests exercise invalid and hostile inputs or resource cleanup rather than only the successful path. Their value is the behavior they pin,
not the number of methods or classes touched.

## Shared contract tests

Reusable contract suites run the same observable expectations against multiple implementations. `FilesystemTests` checks common filesystem path,
entry, mutation, and sync/async behavior. Database tests reuse `DBConnectionTests<T>` and cursor assertions to ensure reader access paths share one
position and result-set semantics.

A provider is covered by a contract only when its test fixture actually runs that suite. Inheriting or referencing a shared suite does not provide
ordinary CI evidence if the fixture requires external infrastructure and is filtered as integration-only.

## Provider-specific tests

Adapters also need tests for behavior that should not become a generic promise: SQL dialect and metadata mapping, transport details, backend
capabilities, protocol parsing, serialization quirks, and resource ownership. A provider-specific assertion documents a deliberate difference; it
should not silently redefine the shared abstraction for every provider.

The reverse distinction matters too. A unit test for generated SQL or a mocked transport does not prove that a live service accepts the request or
implements the same lifecycle under failure.

## Integration tests

Tests requiring a real database, service, credentials, platform facility, or environment configuration carry the `Category=Integration` trait. This
keeps the normal suite deterministic and credential-free while preserving executable checks for environments that can supply their dependencies.

Integration filtering is an explicit evidence boundary. Passing normal CI does not prove every credential-dependent provider or external integration.
Likewise, the presence of focused tests does not turn implementation-specific behavior into a provider-independent or operational guarantee.

## Subsystem evidence patterns

The maintained families use different forms of evidence according to their architecture:

- **Filesystem:** `FilesystemTests` is a reusable provider contract. Core providers run it in the deterministic suite; HTTP and S3 inherit the same
  expectations in integration-classified provider projects. Focused tests add path safety, cancellation, read-only, composition, metadata, and
  ownership checks.
- **Database:** reusable reader and connection contracts establish cursor, lifecycle, transaction, parameter, and schema behavior. Provider projects
  add SQL, type, and metadata checks, while CI runs one selected live PostgreSQL schema-discovery contract.
- **Logging and log storage:** focused tests cover structured fields, scopes, `EventId`, exceptions, async-flow behavior, serializers, lifecycle,
  query/retention behavior, and an in-process OpenTelemetry pipeline. These are focused implementation suites, not one shared storage-provider
  contract.
- **Cache:** filesystem-backed tests cover payload and metadata persistence, replacement, expiration and cleanup, get-or-create behavior, safe key
  mapping, cancellation, stream ownership, null-cache behavior, and URL factories. They do not establish distributed cache coordination.
- **Queues:** filesystem-backed tests cover write/read framing, immediate and bounded empty reads, claim and duplicate-claim prevention, deletion,
  purge, cancellation, concurrent readers, and independent queue objects sharing one filesystem. They do not establish FIFO, redelivery, leases,
  durable acknowledgement, or distributed operation.
- **Secrets:** manager tests cover seal state, wrong passwords, password rotation, CRUD, pattern listing, encrypted JSON persistence,
  plaintext-leakage assertions, corrupt data, cancellation, null behavior, and factories. External and platform-specific providers remain separate
  evidence gaps.
- **Repositories:** the filesystem implementation is exercised across JSON, YAML, and YAML front matter for CRUD, listing/filtering, malformed data,
  invalid formats, ID/path safety, and cancellation. The tests do not imply transactions or stronger concurrency than the supplied filesystem.
- **Mail:** tests verify recipient expansion into database records, EML content, BCC privacy, message-ID domain behavior, cancellation, database
  failure propagation, temporary-resource cleanup, null behavior, and factories. They verify acceptance into the database spool path, not delivery.

Other focused suites exercise commands, data objects and types, functional results, expressions, text serializers, and text readers. Their presence
adds evidence for those specific public behaviors without creating a single repository-wide coverage metric or a uniform support classification.

## XVault interoperability fixtures

`DProjects.XVault.Test` consumes sanitized, committed fixtures generated by the external canonical `marcdp/xvault` implementation. JSON, JSONC,
YAML, ENV, and Markdown therefore verify that the .NET reader accepts another implementation's real crypto-v1 output, which is stronger evidence
than a self-generated encrypt/decrypt round trip. The suite also verifies wrong passwords, missing and malformed metadata, unsupported versions,
invalid or truncated tokens, authenticated-decryption failure after tampering, metadata removal, and configuration registration where supported.

The XML fixture uses canonical crypto-v1 values in the .NET reader's XML envelope because the inspected canonical XVault revision does not implement
XML parsing/stringifying. Platform keyring integration is likewise outside the deterministic normal suite. These boundaries are recorded on the
[XVault interoperability](xvault/index.md) page rather than being implied by a passing fixture suite.

## Current CI pipeline

The GitHub Actions workflow runs on pushes and pull requests to `main`. It:

1. checks out the repository and installs the SDK family selected for the solution;
2. restores the full solution;
3. builds the full solution in Release configuration;
4. runs the repository-wide suite while excluding `Category=Integration`;
5. provisions PostgreSQL and runs the selected `Category=CIProviderContract` schema-discovery checks;
6. packs the solution without rebuilding.

The PostgreSQL step is useful live-provider evidence for that selected contract. It is not evidence for every PostgreSQL behavior, other database
servers, cloud filesystems, mail delivery, platform secret stores, or any provider requiring credentials not present in the workflow.

## Local verification sequence

The repository's standard local sequence mirrors the broad CI stages:

```bash
dotnet restore DProjects.Libs.sln

dotnet build DProjects.Libs.sln \
  --configuration Release \
  --no-restore

dotnet test --solution DProjects.Libs.sln \
  --configuration Release \
  --no-build \
  --no-restore \
  -- \
  --filter-not-trait "Category=Integration" \
  --ignore-exit-code 8

dotnet pack DProjects.Libs.sln \
  --configuration Release \
  --no-build \
  --output artifacts/packages
```

Focused projects should normally run first for faster feedback, followed by the broad sequence when practical. Live integration checks should run
only when their infrastructure is available. A reported validation result should distinguish tests that passed, tests filtered out, tests skipped,
and checks that could not be run.

## Reading verification evidence

Good verification demonstrates a compatibility rule, failure boundary, or provider distinction. Test counts alone do not show whether public
contracts, cancellation, ownership, persisted formats, or security-sensitive failures are covered. Documentation therefore calls out both the
evidence that exists and material gaps without converting every untested method into a defect list.

See [Repository architecture](architecture.md), [Support and status](support.md), or return to the [documentation index](index.md).
