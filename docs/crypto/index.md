# Cryptography

The crypto family provides stream-oriented hashing, password-based key derivation, and symmetric transforms behind small public contracts. It also
registers URL factories for selecting algorithms and options at runtime.

```text
DProjects.Crypto
        ↓
DProjects.Crypto.Abstractions
```

This page describes compatibility and implementation facts. The presence of an algorithm is not a security recommendation, and the package does not
claim to provide a complete cryptographic protocol.

## Contracts and available implementations

`ICryptoHash` hashes or verifies streams and text and exposes a factory URL. Implementations exist for bcrypt, MD5, SHA-1, SHA-256, and SHA-512.
`ICryptoKeyDerivation` derives bytes from a password and caller-supplied salt; the implementation uses PBKDF2 with configurable PRF, iteration count,
and key length. Symmetric encrypt/decrypt contracts return transform streams and have extension methods for streams, byte arrays, and strings.

AES and Caesar transforms are implemented. Caesar is a reversible compatibility transform, not cryptographic protection. MD5 and SHA-1 remain
available as compatibility hashes and should not be inferred to provide collision resistance appropriate to new security designs. Bcrypt handles
text as UTF-8 and embeds its own salt/work-factor format. `ICryptoAsymmetric` is currently an empty marker with no concrete implementation.

Factories use `md5:`, `sha1:`, `sha256:`, `sha512:`, `bcrypt:`, `pbkdf2:`, `aes:`, and `caesar:`. Options are deserialized from URL query values, so
protocol names, option names, defaults, and serialized headers are compatibility-sensitive surfaces.

## AES format and password handling

AES defaults to CBC, PKCS7 padding, a 256-bit key, random 16-byte salt and IV, and PBKDF2-HMAC-SHA256. Its default iteration count is chosen
between configured minimum and range values. Both Base64 and binary encodings are supported, and Base64 output may be line-folded.

With headers enabled, encryption prefixes a serialized `aes:` option header and separator. The encoded payload then records the IV, salt, and actual
iteration count before ciphertext; an encrypted salt marker is also consumed during decryption. The header can carry a `Version` string, which the
decryptor passes to a password-provider callback so callers can select credentials for persisted versions. This is format selection, not automatic
key rotation or secret management.

These bytes and header options form a repository-specific persisted format. Changing defaults may affect newly written values, while changing parsing,
field order, lengths, encoding, or header syntax can make existing ciphertext unreadable. The string helpers are naturally suited to Base64 text;
binary ciphertext should be handled as bytes or streams rather than arbitrary UTF-8 text.

## Ownership and execution model

Encryption returns a writable `CryptoStream`; decryption returns a readable one. The extension methods dispose the transform after copying so final
padding and buffered encoding are completed. Current AES wrappers leave the caller's input or output stream open, making the caller responsible for
the original stream. Callers using `GetStream` directly must dispose the returned transform.

SHA hash asynchronous methods compute synchronously and return completed tasks; their cancellation tokens do not interrupt hashing. Other transforms
delegate to the repository stream helpers. Async-shaped APIs therefore do not uniformly imply native cancellable cryptographic work.

## Security boundary

The AES implementation is configurable to modes such as ECB and permits a caller-supplied IV. Availability does not make every configuration safe.
The format does not add an authentication tag or MAC, so it does not establish ciphertext integrity or authenticated encryption. Hash
verification for the SHA/MD5 implementations uses ordinary byte comparison rather than a documented constant-time comparison contract.

Passwords, derived keys, salts, IVs, and ciphertext lifecycle remain the caller's responsibility. Factory URLs can contain security-relevant options
and must not be assumed safe to log merely because they are configuration strings. Use the [Secrets](../secrets/index.md) boundary for secret lookup;
it does not change the cryptographic guarantees of the selected algorithm.

## Verification and limitations

Focused tests pin known MD5, SHA-1, SHA-256, SHA-512, and PBKDF2 outputs and one deterministic AES representation/round trip plus a Caesar round trip.
They provide useful compatibility evidence but do not cover bcrypt, malformed headers, tampering, all AES modes and encodings, cancellation, factory
discovery, or cryptographic side-channel properties. No external security review or formal protocol analysis is represented in this repository.

The combination of legacy algorithms, configurable unsafe modes, an unauthenticated AES format, and partial tests makes conservative use essential.
See [Support and status](../support.md) and [Streams](../streams/index.md) for the underlying wrapper behavior.

Return to the [documentation index](../index.md).
