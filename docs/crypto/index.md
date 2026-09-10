# Cryptography

## Purpose

The crypto family provides stream-oriented hashing, password-based key derivation, and symmetric transforms behind small public contracts. It also
registers URL factories for selecting algorithms and options at runtime.

```text
DProjects.Crypto
        ↓
DProjects.Crypto.Abstractions
```

The current subsystem is **Crypto v1**. This page defines its support, compatibility, and security boundaries. The presence of an algorithm is not a
security recommendation, and Crypto v1 does not claim to provide a complete cryptographic protocol.

## Support status

Crypto v1 is classified as **Legacy / compatibility**. It is an established, valid architecture whose existing contracts and persisted formats remain
supported and should be preserved and verified. This status does not mean broken, abandoned, unsafe by definition, scheduled for removal, or
incorrectly designed. It means that compatibility constraints take priority over architectural modernization.

Existing valid v1 APIs and data should continue to work. Security capabilities that require incompatible guarantees, abstractions, primitives, or
formats belong in a separately designed Crypto v2.

## Crypto v1

Crypto v1 is intentionally compatibility-oriented and is designed around the existing stream contracts. `ICryptoHash` hashes or verifies streams and
text, `ICryptoKeyDerivation` derives bytes from a password and caller-supplied salt, and the symmetric contracts create writable encryption streams or
readable decryption streams. Extension methods adapt those contracts to streams, byte arrays, and strings.

The v1 contract includes observable behavior beyond public signatures: factory URLs, option semantics, stream ownership, and data that consumers have
already persisted. V1 is therefore maintained by preserving intended behavior and format compatibility rather than continuously redesigning it to
follow newer cryptographic API trends.

## Security contract

AES v1 provides encryption using the implemented configurable AES transform. It defaults to CBC mode, PKCS7 padding, a 256-bit key, a random 16-byte
salt and IV, and PBKDF2-HMAC-SHA256 with an iteration count selected from its configured range. The v1 representation has no authentication tag or
MAC. Successful decryption is not proof that ciphertext is authentic, intact, or untampered.

Crypto v1 does not claim authenticated encryption, ciphertext integrity or authenticity, tamper-proof persisted storage, replay protection,
automatic key management or rotation, a complete modern cryptographic protocol, or formal security verification. Passwords, keys, salts, IVs,
ciphertext lifecycle, configuration safety, and protocol composition remain caller responsibilities. Some configurable choices, including ECB mode
or a caller-supplied IV, require particular care; their availability is not a recommendation.

These limits define the v1 boundary. They do not imply that the existing compatibility surface should be redesigned in place.

## Compatibility contract

Changes to v1 should preserve valid established external behavior and persisted-data compatibility unless a migration is explicitly designed. The
compatibility boundary includes:

- public interfaces and stream ownership semantics;
- factory protocols and configuration option names, defaults, and meanings;
- AES header syntax, metadata order, Base64 and binary representations, and folding behavior;
- credential selection through the AES `Version` value and password-provider callback;
- existing encrypted files and other persisted ciphertext;
- bcrypt encoded password hashes; and
- PBKDF2 parameters whose values determine the derived key.

Not every internal implementation detail is immutable. Correctness, validation, and implementation improvements remain appropriate when they preserve
the intended v1 contract and existing valid data.

## Current capabilities

Crypto v1 currently provides SHA-1, SHA-256, SHA-512, and MD5 hashing; bcrypt password hashing and verification; PBKDF2 key derivation; AES symmetric
encryption and decryption; and the Caesar compatibility transformation. Factories use the `sha1:`, `sha256:`, `sha512:`, `md5:`, `bcrypt:`,
`pbkdf2:`, `aes:`, and `caesar:` protocols. `ICryptoAsymmetric` is an empty marker and has no concrete implementation.

The SHA implementations expose stream and text hashing through the generic hash contract. Their async-shaped methods currently perform synchronous
hashing and return completed tasks, so cancellation does not interrupt that work. Symmetric transforms use the repository's stream wrappers; callers
using `GetStream` directly must dispose the returned transform, while the underlying caller stream remains open in the current AES wrappers.

## Algorithm-specific guidance

- **MD5 and SHA-1** remain compatibility algorithms. Do not select them for new security-sensitive designs that require modern collision resistance.
- **SHA-256 and SHA-512** compute their corresponding SHA-family digests. The wrapper does not add authenticity, password-hardening, protocol design,
  constant-time verification, or other guarantees beyond the implemented hashing behavior.
- **bcrypt** is a password/text hashing and verification facility. Its stream methods decode the complete stream as UTF-8 text; it is not a
  general-purpose byte-stream digest equivalent to SHA-256 or SHA-512.
- **PBKDF2** derives a key from a password, salt, PRF, iteration count, and key length. Those parameters are compatibility data: changing them changes
  derived keys and can make persisted data unreadable. Historical defaults describe v1 behavior, not a current policy recommendation.
- **AES** uses the v1 representation described below. It provides encryption but is not authenticated encryption.
- **Caesar** is a reversible compatibility transformation, not meaningful modern cryptographic protection.

## AES v1 persisted representation

With headers enabled, AES encryption prefixes a serialized `aes:` options header and separator. The Base64 or binary payload then records the IV,
salt, and actual iteration count in that order before the ciphertext; decryption also consumes an encrypted salt marker. Base64 output can be folded.
The string helpers are naturally suited to Base64 text, while binary ciphertext should be handled as bytes or streams.

The header's `Version` option is passed to the decryption password-provider callback so a consumer can select the credential used for an existing
persisted value. It is a credential-selection value within the v1 representation. It is not subsystem-version negotiation, automatic key rotation,
or authenticated cryptographic-format negotiation.

For clarity, **Crypto v1** names the current subsystem and API strategy. A possible future **Crypto v2** names a separate security evolution. Neither
term introduces a new serialized format here, and the existing AES `Version` value should not be confused with either subsystem label.

## What v1 maintenance means

Appropriate v1 work includes correctness, regression, and compatibility fixes; deterministic compatibility tests; malformed-input robustness; better
verification of existing behavior; documentation improvements; removal of misleading examples; and non-breaking implementation improvements.

V1 should not casually replace the AES format, silently change defaults that affect derived keys or persisted data, introduce a new authenticated
format under existing semantics, redesign the stream abstractions solely for future crypto requirements, remove old compatibility algorithms, or
change persisted representations without a migration plan.

## Crypto v2 direction

Crypto v2 is the future security-oriented evolution of the subsystem. Security improvements that require different guarantees, formats, primitives,
or abstractions belong in v2 rather than being retrofitted incompatibly into v1.

Potential design concerns include authenticated encryption, explicit integrity and authenticity guarantees, stronger hostile-input handling,
deliberate format versioning, modern password and KDF policies, clearer key and credential lifecycles, explicit migration from historical formats,
and reconsidered streaming semantics when authentication before use is required. These are roadmap boundaries, not a chosen algorithm, API, serialized
v2 format, or implemented feature.

## Consumers and verification

Repository consumers persist AES v1 data. `SecretManagerJson` stores AES-encrypted JSON documents, `SecretProviderDProjectsTools` reads existing
`.json.aes` files, and password-protected `FilesystemXml` instances encrypt their persisted XML representation. Their ability to read existing data
depends on v1 format and credential compatibility.

Focused tests pin known MD5, SHA-1, SHA-256, SHA-512, and PBKDF2 outputs; bcrypt password/text verification; exact Base64 and binary AES v1
representations; folded consumer configuration; password-provider version selection; the Base64 salt-marker calculation; factory discovery and documented
option names; stream ownership; and Caesar compatibility behavior. Negative tests cover representative malformed AES headers and metadata, wrong
passwords, corruption outcomes, invalid PBKDF2 values, malformed bcrypt hashes, and invalid Caesar keys.

AES v1 rejects negative and absurd serialized allocation lengths before allocating buffers and bounds serialized iteration counts before invoking
PBKDF2. These checks are deliberately conservative compatibility guards, not a general validation framework or an authenticity mechanism. The tests
do not cover every AES mode and option combination, side-channel properties, or formal protocol analysis.

See [Support and status](../support.md), [Secrets](../secrets/index.md), and [Streams](../streams/index.md) for related lifecycle and consumer
boundaries.

Return to the [documentation index](../index.md).
