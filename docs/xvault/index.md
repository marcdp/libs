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

The reader supports `crypto_version = 1` and `crypto_version = 2`. Both versions use Argon2id version 19 with a 16-byte salt, time cost 5, memory cost
131072 KiB, parallelism 4, and a 32-byte output. CryptoV1 uses that derived key directly to decrypt secret values and validates the historical `check`
token when present.

CryptoV2 uses the derived key as a key-encryption key (KEK). Metadata stores `encrypted_key` as URL-safe base64 of a 12-byte nonce, encrypted
32-byte data-encryption key (DEK), and 16-byte AES-GCM tag. The reader authenticates and unwraps this 60-byte envelope with AES-256-GCM and the exact
UTF-8 AAD `xvault:v2:dek`. It then uses the authenticated DEK for secret tokens. Secret values in both versions use a 12-byte nonce, ciphertext, and
16-byte tag with no AAD.

Unknown versions, malformed metadata, invalid key sizes or encodings, wrong passwords, truncated values, and modified ciphertext, nonces, or tags
fail explicitly. A failed v2 unwrap never creates or substitutes a DEK.

## Key resolution

Supplying a password works on every supported platform. When the password is omitted on Windows, the reader can retrieve the canonical Python keyring
credential from Windows Credential Manager. CryptoV1 uses `xvault:<absolute-file-uri>` and caches its direct encryption key; CryptoV2 uses
`xvault:v2:<absolute-file-uri>` and caches the KEK, never the DEK. A cached value must decode to exactly 32 bytes, and a cached v2 KEK must still
authenticate the file's current wrapped DEK.

Cached-key lookup remains Windows-specific. Other platforms require a password, and the deterministic test suite verifies identity selection rather
than reading or modifying a user's real credential store.

## Verification

Sanitized CryptoV1 and CryptoV2 fixtures for JSON, JSONC, YAML, ENV, and Markdown were produced by the canonical `marcdp/xvault` implementation at
commit `b18412d` (XVault `0.0.45`). Tests consume those committed files from .NET and compare the decrypted document or configuration values. The
canonical fixed `tests/fixtures/v2.env` Argon2 vector is copied byte-for-byte. Separate before/after canonical password-rotation fixtures verify that
a new KEK and wrapped DEK can decrypt unchanged secret ciphertext. Fixed synthetic randomness makes fixture regeneration reproducible; no real
secrets are present.

The XML test uses canonical v1 ciphertext and metadata inside the envelope supported by this .NET reader. The current canonical Python format
registry does not support XML, so end-to-end canonical XML authoring is not claimed. Canonical Python also supports OpenVPN, which this .NET reader
deliberately does not add.

## Explicit non-goals

- Creating XVault files or encrypting and modifying XVault documents.
- Vault management or XVault CLI behavior.
- Owning XVault format evolution.
- General-purpose cryptography.
- Supporting unrelated encrypted-file formats.

## Limitations

Compatibility is tied to the known XVault schema and CryptoV1/CryptoV2 formats. Future crypto versions require separate compatibility work. Windows
keyring state is not exercised by the normal deterministic suite; password-based derivation is the portable verified path. ENV, XML, and Markdown
cannot be registered as .NET configuration. Canonical end-to-end XML generation and the `.markdown` alias are not evidenced by the inspected Python
implementation. Canonical XVault's `.ovpn` format is not supported by this .NET reader.

Return to the [documentation index](../index.md) or see [Verification](../verification.md) and [Support and status](../support.md).
