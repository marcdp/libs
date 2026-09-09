# DProjects.XVault

`DProjects.XVault` provides .NET read interoperability for partially encrypted files produced by XVault. It is a format reader, not an
implementation of the XVault product.

## Purpose

```text
XVault-generated file
        ↓
DProjects.XVault
        ↓
metadata/version validation
        ↓
key resolution or derivation
        ↓
authenticated token decryption
        ↓
plaintext document or .NET configuration
```

Construct `XVault` with a file path and, normally, its password. `Decrypt()` returns the document with encrypted values replaced by plaintext and
XVault-owned metadata removed. `Register(ConfigurationManager)` loads decrypted values as .NET configuration only where the format has meaningful
configuration semantics.

## Supported scope

| Format | Extensions | `Decrypt()` | `Register()` |
| --- | --- | --- | --- |
| JSON / JSONC | `.json`, `.jsonc` | Supported | Supported |
| YAML | `.yaml`, `.yml` | Supported | Supported |
| XML | `.xml` | Supported by the .NET reader | Not supported |
| ENV | `.env` | Supported | Not supported |
| Markdown | `.md`, `.markdown` | Supported | Not supported |

Unsupported registration calls fail with `NotSupportedException`. The reader does not infer configuration semantics for arbitrary documents.

## Cryptographic compatibility

The verified XVault crypto format is `crypto_version = 1`: Argon2id derives a 256-bit key from the password and metadata salt, and AES-256-GCM
authenticates and decrypts each token. This protocol implementation exists only for XVault v1 compatibility; it is not a general cryptography API.
Unknown crypto versions fail explicitly. Wrong passwords, malformed metadata or token encoding, truncated tokens, and modified ciphertext or tags
are rejected.

## Verification

Sanitized fixtures for JSON, JSONC, YAML, ENV, and Markdown were produced by the canonical `marcdp/xvault` implementation at commit `39f760c`
(XVault `0.0.45`). Tests consume those committed files from .NET and compare the decrypted document or configuration values. Fixed synthetic
randomness makes fixture regeneration reproducible; no real secrets are present.

The XML test uses canonical v1 ciphertext and metadata inside the envelope supported by this reader. Canonical XVault recognizes `.xml` at the
inspected revision, but its XML handler has no parse/stringify implementation, so end-to-end canonical XML authoring is not yet verified.

## Explicit non-goals

- Creating XVault files or encrypting and modifying XVault documents.
- Vault management or XVault CLI behavior.
- Owning XVault format evolution.
- General-purpose cryptography.
- Supporting unrelated encrypted-file formats.

## Limitations

Compatibility is tied to the known XVault schema and crypto-v1 format. Future crypto versions require separate compatibility work. Windows keyring
lookup is platform state and is not exercised by the normal deterministic suite; password-based derivation is the portable verified path. ENV, XML,
and Markdown cannot be registered as .NET configuration. Canonical end-to-end XML generation and the `.markdown` alias are not currently evidenced
by the inspected Python implementation. Canonical XVault's `.ovpn` format is not supported by this .NET reader.

Return to the [documentation index](../index.md) or see [Verification](../verification.md) and [Support and status](../support.md).
