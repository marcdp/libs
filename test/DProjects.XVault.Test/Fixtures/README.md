# Fixture provenance

The fixtures were generated or copied from the canonical `marcdp/xvault` Python implementation inspected at commit `b18412d` (version `0.0.45`).
They contain no real credentials.

- `compatibility.*` contains deterministic CryptoV1 output for JSON, JSONC, YAML, ENV, and Markdown.
- `compatibility-v2.*` contains deterministic CryptoV2 output for the same formats, including whole-value and wrapped markers and one Unicode secret.
- `canonical-v2.env` is copied byte-for-byte from canonical `tests/fixtures/v2.env`. Its password is `fixture-v2-password`.
- `rekey-before-v2.env` and `rekey-after-v2.env` use canonical `CryptoV2.prepare_password_rotation`. Their secret ciphertext is identical while the
  salt and wrapped DEK change. Their passwords are `fixture-v2-old-password` and `fixture-v2-new-password`, respectively.

The synthetic compatibility password is `correct horse battery staple`. `generate.py` supplies deterministic bytes only to canonical cryptographic
objects during test-fixture creation; it does not alter production randomness. Existing CryptoV1 fixtures remain byte-for-byte reproducible.

The XML fixture uses canonical CryptoV1 ciphertext and metadata inside the XML envelope supported only by this .NET reader. The current canonical
Python format registry does not support XML, so the fixture does not claim Python XML writer provenance.
