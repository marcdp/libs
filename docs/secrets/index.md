# Secrets

The secrets family separates read-only lookup from mutable secret management. It supplies memory, JSON-file, user-secrets, and platform-specific
implementations plus factory integration. This is a configuration boundary; it does not by itself prove that secret values are protected in memory,
at rest, in process arguments, or in logs.

```text
DProjects.Secrets
        ↓
DProjects.Secrets.Abstractions
```

The concrete package depends on filesystem, crypto, database abstractions, and factories. Provider behavior is not uniform, so callers need to choose
between the smaller `ISecretProvider` lookup contract and the stateful `ISecretManager` contract deliberately.

## Identifiers, values, and lookup

An `ISecretProvider` accepts a secret identifier and returns a nullable `Secret` synchronously or asynchronously. A `Secret` has a `Name`, descriptive
and lifecycle metadata, tags, and a dictionary of values. `GetValue()` reads the entry named `default`; named values are distinct from the identifier.

For example, `${secret:database-password}` contains the identifier `database-password`, not the password itself. `FactoryByUrl<T>` recognizes that
syntax, requests the identifier from the configured `ISecretProvider`, and substitutes the secret's default value before dispatching the resulting
URL. A missing secret produces `KeyNotFoundException`; a missing provider is a dependency-injection failure.

Substitution means the resolved value temporarily exists in a normal string and may become URL user info or a query value. Factory substitution does
not redact downstream exceptions, logs, aliases, or persisted configuration. Store references rather than resolved values where possible, and do not
persist or log URLs after substitution without explicit redaction.

## Providers and managers

Read-only providers include:

- a flat JSON file read through local file utilities;
- a user-secrets JSON document read through an injected filesystem;
- a DProjects Tools provider that loads an encrypted file under the user profile and obtains its password from Windows Credential Manager.

Missing JSON properties return `null`. Missing or malformed files may throw. The DProjects Tools provider is Windows-specific
because it calls the Windows credential API; its async method wraps synchronous lookup and is not cancellable I/O.

`ISecretManager` adds sealed-state operations plus list, get, set, and delete. Implementations are:

- `SecretManagerMem`, an in-process dictionary guarded by a password-controlled sealed flag;
- `SecretManagerJson`, an in-memory collection persisted as an AES-encrypted JSON document over `IFilesystem`;
- `SecretManagerUserSecrets`, a cached plaintext string dictionary stored through `IFilesystem`;
- `SecretManagerNull`, which remains sealed, returns no secrets, and discards mutations.

The JSON manager uses the repository's AES v1 format and retains the password so later mutations can be saved automatically. Its persisted-format
compatibility and security guarantees are therefore bounded by the [Crypto v1](../crypto/index.md) contract, including the lack of authenticated
encryption or ciphertext integrity. Successful unsealing is not proof that persisted ciphertext was authentic or untampered. Future Crypto v2
security work is separate from this existing persisted-format contract and would require an explicit migration design.

User-secrets storage is plain JSON; the name does not imply encryption. Memory sealing controls API access to the dictionary but does not erase
values from managed memory.

## Factory protocols and lifecycle

Manager factories use `json:`, `mem:`, `user-secrets:`, and `null:`. Provider factories use `file:`, `user-secrets:`, and `dprojectstools:`. Factory
dispatch is generic-type-specific, so the same protocol name may select different behavior for `ISecretManager` and `ISecretProvider`.

Nested manager URLs can contain filesystem URLs, and `mem:` currently accepts its password in a query parameter. These are compatibility surfaces and
potential disclosure points. Examples in documentation and tests should use placeholders only.

The secret interfaces are not disposable. Implementations retain filesystem references or cached values without an explicit disposal/zeroization
contract. Cancellation is accepted by async methods but several in-memory or synchronous-provider paths complete synchronously.

## Failure and verification boundary

Provider lookup uses `null` for an absent identifier; manager operations may throw when sealed. Unsealing reports success as a Boolean; malformed
or inaccessible persisted data can still throw. No common exception taxonomy distinguishes missing files, invalid passwords, corrupt ciphertext,
unsupported platforms, or parsing failures.

`test/DProjects.Secrets.Test` covers manager sealing and CRUD behavior, encrypted JSON persistence and password rotation, cancellation, corrupt stored
data, and manager factory wiring. Factory substitution is also tested in `DProjects.Factories.Test`, including successful default-value replacement.
Platform-specific providers, every failure category, and cryptographic hostile-input behavior still lack broad direct coverage. See
[Support and status](../support.md).

Return to the [documentation index](../index.md) or read [Factories](../factories/index.md), [Crypto](../crypto/index.md), and
[Filesystem](../filesystem/index.md) for collaborating boundaries.
