# Database

The database subsystem combines provider-independent connection, tabular-data, reader/writer, and portable schema contracts with reusable execution
and schema-reconciliation behavior. Provider packages translate those contracts to PostgreSQL, SQLite, SQL Server, and Oracle without adding their
client libraries to the shared layers.

## Project boundaries and dependencies

`DProjects.Db.Abstractions` targets `netstandard2.0`. It defines `IDBConnection`, `IDBReader`, `IDBWriter`, `DBTable`/`DBRow`/`DBColumn`, and the
portable schema model for tables, columns, keys, indexes, views, sequences, procedures, functions, records, and scripts. Its dependencies are
Microsoft logging abstractions, XML support, and utilities—not an ADO.NET provider package.

`DProjects.Db` also targets `netstandard2.0`. It implements the `DBConnection` base class, ADO.NET reader ownership, in-memory and text readers,
format writers, schema comparison/application, and URL factories for readers and writers. It depends on the database abstractions and shared factory,
filesystem, logging, expression, text-reader, and YAML packages.

Provider projects depend on `DProjects.Db` and own their drivers:

| Provider package | Target | Driver | Factory protocol |
| --- | --- | --- | --- |
| `DProjects.Db.Postgresql` | `net10.0` | Npgsql | `postgresql:` |
| `DProjects.Db.Sqlite` | `net10.0` | Microsoft.Data.Sqlite | `sqlite:` |
| `DProjects.Db.SqlServer` | `netstandard2.0` | Microsoft.Data.SqlClient | `sqlserver:` |
| `DProjects.Db.Oracle` | `netstandard2.0` | Oracle.ManagedDataAccess | `oracle:` |

Each package exposes an `IAssembly` marker and an `IFactoryByUrl<IDBConnection>` implementation. Registration is explicit through the
[factory subsystem](../factories/index.md); provider assemblies are not referenced or scanned by the abstraction or core package.

## Connection contract and reusable behavior

`IDBConnection` wraps an owned `DbConnection` and covers lifecycle, execution, commands, transactions, schema discovery/reconciliation, DDL, and
dialect-specific SQL fragments. `DBConnection` supplies the common lifecycle and execution policy:

- operations automatically open a closed connection;
- `Close` releases an active transaction but leaves the wrapper reusable;
- `Dispose` is idempotent and permanently ends the wrapper lifetime;
- only one active transaction is supported, and commit/rollback require one;
- commands inherit the active transaction and `CommandTimeout`;
- caller `?` placeholders are validated and converted to native provider parameters for execution;
- readers returned by execution own the internally created command, and the raw-reader wrapper preserves that ownership.

`ParseStatement` renders literal SQL for diagnostics and tests. It is deliberately distinct from execution, which binds native parameters. Code must
not use rendered SQL as a substitute for parameterization.

Synchronous and asynchronous methods represent the same operations. Async execution passes cancellation to ADO.NET provider calls; the shared tests
also require cancellation and provider exceptions to retain their identity while commands are still disposed.

## Reader and writer contracts

`IDBReader` is a forward cursor. `Read()` and `Read(object?[])`, plus their async counterparts, are four access paths over one cursor—not independent
enumerations. After the final row, row-returning calls return `null`, array-filling calls return `false`, and both `NextResult` forms use equivalent
result-set semantics. Column count and sync/async column metadata describe the same logical result set.

The core adapts `DbDataReader`, `DBTable`, CSV, raw/plain text, JSON/JSON Lines, XML, YAML, and document-oriented variants. `DBReaderView` projects,
renames, filters, sorts, offsets, and limits another reader. Reader factories accept a `TextReader` as their second factory argument.

`IDBWriter` provides value, `DBRow`, and dictionary writes plus flush and async disposal. Core writers cover CSV, JSON, JSON Lines, XML, YAML,
Markdown, HTML, plain/raw text, and domain-specific formats. Writer factories accept a caller-owned `TextWriter`. These formats are part of the core
data-transfer layer; they do not make a text reader a database connection.

`DBRow` is a fixed-schema ordered dictionary backed by its table columns. Value updates are permitted and mark the table changed; structural
dictionary changes are rejected because they would diverge from the table schema.

## Portable schema and provider differences

The schema model is portable but not lossless for every database feature. Providers map native types and metadata into the model and may canonicalize
several portable types to one backend type. Schema comparison therefore has a strict base rule that a provider can override with evidence-based type
equivalence.

`GetSchema` without filters requests every schema category. The filtered overload treats an empty array as “omit this category,” `*` as all objects,
and other values as name patterns. A requested unsupported category raises `NotSupportedException`. In `ApplySchemaChanges`, tables are always
managed; views, sequences, and procedures are managed only when their target collections are non-empty. A dry run logs planned scripts, while apply
mode executes them without promising an atomic migration.

Provider capabilities are intentionally unequal:

- PostgreSQL implements table metadata, indexes, foreign keys, view enumeration, sequences, paging, type mapping, and provider-aware schema
  equivalence. It rejects native index, constraint, or foreign-key semantics that the portable model cannot represent safely; view definitions and
  procedures remain unsupported.
- SQL Server implements table/view/sequence discovery, substantial DDL and database operations, paging, identities, and native type mapping. It also
  rejects unrepresentable constraint-owned unique indexes.
- SQLite supports normal sync/async execution and a useful subset of SQL generation. Type affinities collapse portable types; schema discovery,
  sequences, and table-rebuild operations such as altering columns or adding existing-table keys/defaults are explicitly unsupported.
- Oracle constructs an Oracle ADO.NET connection; focused tests verify parameter, quoting, test-query, timestamp, and selected type-mapping
  primitives. Schema discovery and generation, paging, identities, database administration, and several literal encodings are explicitly
  unsupported; it is not covered by the shared live connection contract.

These failures are architectural signals, not placeholders: callers must either restrict requests to supported categories or use provider-specific
workflows. Returning partial or guessed metadata would make reconciliation destructive.

## Verification

`DProjects.Db.Test` verifies the reusable layer without an external server. `DBReaderContractTests` mixes all four read forms against table, raw, CSV,
plain, XML, XML-document, view, and ADO.NET readers. Hardening tests cover native parameter binding, placeholder validation, cancellation, transaction
state, exception preservation, command/reader ownership, connection lifecycle, schema filter semantics, dependency ordering, and dry-run versus apply
behavior. `DBRowDictionaryTests` fixes the ordered, non-structural dictionary contract.

`DBConnectionTests<T>` is the shared live-provider suite for open/close, automatic opening, parameter rendering, execution, transactions, table DDL,
and paging. PostgreSQL, SQLite, and SQL Server inherit it and mark it `Integration`. SQLite additionally has in-memory non-integration coverage for
execution, parameters, transactions, type affinity, and explicit limitations. PostgreSQL and SQL Server have extensive non-network dialect and
metadata-materialization tests; Oracle tests only its retained primitives and unsupported boundary.

CI runs the non-integration suite and separately provisions PostgreSQL 17 for the `CIProviderContract` schema-discovery test. It does not run the full
credential-dependent PostgreSQL suite or the SQLite/SQL Server shared integration classes, and it performs no live Oracle validation.

## Related documentation

- [Factories](../factories/index.md)
- [Repository architecture](../architecture.md)
- [Documentation index](../index.md)
